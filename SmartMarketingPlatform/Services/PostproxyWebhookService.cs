using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using SmartMarketingPlatform.Common.Integrations.Postproxy;
using SmartMarketingPlatform.Data;
using SmartMarketingPlatform.Data.Seed;
using SmartMarketingPlatform.Entities;
using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.Services;

// Every handler is idempotent: Postproxy may deliver the same event more than once.
public sealed class PostproxyWebhookService(
    MarketingDbContext db,
    PostproxyClient client,
    TimeProvider clock,
    ILogger<PostproxyWebhookService> logger) : IPostproxyWebhookService
{
    public async Task<string> HandleAsync(JsonObject webhookEvent, CancellationToken cancellationToken)
    {
        var type = webhookEvent["type"]?.ToString();
        var data = webhookEvent["data"]?["object"] as JsonObject;
        if (type is null || data is null)
            return "ignored";

        return type switch
        {
            "platform_post.published" => await PublishedAsync(data, cancellationToken),
            "platform_post.failed" => await FailedAsync(data, cancellationToken),
            "platform_post.insights" => await InsightsAsync(data, webhookEvent["created_at"]?.ToString(), cancellationToken),
            "profile.disconnected" => await DisconnectedAsync(data, cancellationToken),
            _ => "ignored"
        };
    }

    private async Task<string> PublishedAsync(JsonObject data, CancellationToken cancellationToken)
    {
        var publication = await FindPublicationAsync(data, cancellationToken);
        if (publication is null) return "unknown_post";
        if (publication.ExternalUrl is not null) return "already_known";

        try
        {
            var post = await client.GetPostAsync(publication.ExternalPostId!, cancellationToken);
            publication.ExternalUrl = PostproxySocialPublisher.FindPlatform(post, data["profile_id"]?.ToString() ?? "")?["permalink"]?.ToString();
        }
        catch (PostproxyException ex)
        {
            logger.LogWarning("Could not read the permalink of post {PostId}: {Error}", publication.ExternalPostId, ex.Message);
        }

        await db.SaveChangesAsync(cancellationToken);
        return "published";
    }

    // The post was accepted but the platform rejected it later: the content goes back to Approved.
    private async Task<string> FailedAsync(JsonObject data, CancellationToken cancellationToken)
    {
        var publication = await FindPublicationAsync(data, cancellationToken);
        if (publication is null) return "unknown_post";

        var job = publication.PublicationJob;
        job.StatusId = StatusIds.PublicationJob.Failed;
        job.LastError = Truncate($"The platform rejected the post: {data["error"]?.ToString() ?? "unknown error"}", 2000);
        job.ContentItem.StatusId = StatusIds.ContentItem.Approved;

        db.MetricObservations.RemoveRange(db.MetricObservations.Where(m => m.PublicationId == publication.Id));
        db.Publications.Remove(publication);
        await db.SaveChangesAsync(cancellationToken);
        return "failed";
    }

    private async Task<string> InsightsAsync(JsonObject data, string? createdAt, CancellationToken cancellationToken)
    {
        var publication = await FindPublicationAsync(data, cancellationToken);
        if (publication is null) return "unknown_post";

        var observedAt = DateTimeOffset.TryParse(createdAt, out var at) ? at : clock.GetUtcNow();
        observedAt = new DateTimeOffset(observedAt.UtcTicks - observedAt.UtcTicks % TimeSpan.TicksPerSecond, TimeSpan.Zero);
        if (await db.MetricObservations.AnyAsync(m => m.PublicationId == publication.Id && m.ObservedAt == observedAt, cancellationToken))
            return "duplicate";

        var insights = data["insights"] as JsonObject;
        foreach (var (code, value) in PostproxyMetricsProvider.ToMetrics(insights))
            db.MetricObservations.Add(new MetricObservation
            {
                PublicationId = publication.Id,
                MetricCode = code,
                MetricValue = value,
                ObservedAt = observedAt,
                Source = MetricSource.PlatformApi,
                RawPayloadJson = insights?.ToJsonString()
            });

        await db.SaveChangesAsync(cancellationToken);
        return "insights";
    }

    private async Task<string> DisconnectedAsync(JsonObject data, CancellationToken cancellationToken)
    {
        var profileId = data["id"]?.ToString();
        if (profileId is null) return "ignored";

        var updated = await db.SocialAccounts
            .Where(s => s.ExternalAccountId == profileId && s.CredentialRef == null && s.StatusId == StatusIds.SocialAccount.Connected)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.StatusId, StatusIds.SocialAccount.Disconnected), cancellationToken);
        return updated > 0 ? "disconnected" : "unknown_profile";
    }

    private Task<Publication?> FindPublicationAsync(JsonObject data, CancellationToken cancellationToken)
    {
        var postId = data["post_id"]?.ToString();
        var profileId = data["profile_id"]?.ToString();
        if (postId is null || profileId is null)
            return Task.FromResult<Publication?>(null);

        return db.Publications
            .Include(p => p.PublicationJob).ThenInclude(j => j.ContentItem)
            .FirstOrDefaultAsync(p => p.ExternalPostId == postId && p.PublicationJob.SocialAccount.ExternalAccountId == profileId, cancellationToken);
    }

    private static string Truncate(string value, int max) => value.Length <= max ? value : value[..max];
}
