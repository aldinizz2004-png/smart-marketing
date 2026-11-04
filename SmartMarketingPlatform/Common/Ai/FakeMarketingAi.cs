using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.Common.Ai;

// A deterministic stand-in for the real AI. It does not call any service, but it builds its
// answers from the real business data (products, offers, tone, banned words, allowed platforms,
// publish window, time zone), so the whole flow can be built and tested end to end for free.
public sealed class FakeMarketingAi(IOptions<AiOptions> options) : IMarketingAi
{
    // Put this text in a campaign goal to test the failure path (AiExecution Failed -> 502).
    public const string FailureMarker = "[simulate-ai-failure]";

    private static readonly TimeOnly EveningSlot = new(19, 0);

    public string ModelName => options.Value.Model;
    public string PromptVersion => "fake-v1";

    public Task<AiCallResult<JsonObject>> PlanCampaignAsync(MarketingContext context, DateTimeOffset now, CancellationToken cancellationToken)
    {
        ThrowIfAskedToFail(context);
        var campaign = context.Campaign ?? throw new ArgumentException("A campaign plan needs a campaign.", nameof(context));

        var start = campaign.StartsAt ?? now;
        var end = campaign.EndsAt ?? start.AddDays(14);
        var days = Math.Max(1, (int)Math.Ceiling((end - start).TotalDays));
        var weeks = Math.Clamp((int)Math.Ceiling(days / 7.0), 1, 8);
        var postsPerWeek = Math.Min(context.MaxPostsPerDay * 7,
            campaign.Objective is CampaignObjective.Awareness or CampaignObjective.Followers ? 5 : 4);
        var bestTime = BestTime(context).ToString("HH:mm", CultureInfo.InvariantCulture);

        var channels = new JsonArray();
        foreach (var platform in context.Channels)
            channels.Add(new JsonObject { ["platform"] = platform, ["postsPerWeek"] = postsPerWeek, ["bestTimes"] = new JsonArray(bestTime) });

        var focus = context.Products.Select(p => p.Name).Concat(context.Offers.Select(o => o.Title)).DefaultIfEmpty(context.BusinessName).ToList();
        var themes = new JsonArray();
        for (var w = 0; w < weeks; w++)
            themes.Add($"Week {w + 1}: {focus[w % focus.Count]}");

        var budgetSplit = new JsonArray();
        if (campaign.Budget is { } budget && context.Channels.Count > 0)
            foreach (var platform in context.Channels)
                budgetSplit.Add(new JsonObject
                {
                    ["platform"] = platform,
                    ["amount"] = Math.Round(budget / context.Channels.Count, 2),
                    ["currency"] = campaign.BudgetCurrency
                });

        var plan = new JsonObject
        {
            ["summary"] = $"{days}-day {campaign.Objective} campaign for {context.BusinessName}: {campaign.GoalStatement}",
            ["objective"] = campaign.Objective.ToString(),
            ["durationDays"] = days,
            ["channels"] = channels,
            ["weeklyThemes"] = themes,
            ["contentMix"] = new JsonObject { ["Reel"] = 40, ["Post"] = 40, ["Story"] = 20 },
            ["kpis"] = new JsonArray(KpiFor(campaign.Objective)),
            ["budgetSplit"] = budgetSplit,
            ["voice"] = new JsonObject { ["dialect"] = context.Dialect, ["tone"] = context.Tone, ["emojiLevel"] = context.EmojiLevel },
            ["avoidTerms"] = new JsonArray(context.ProhibitedTerms.Select(t => (JsonNode?)t).ToArray())
        };

        return Task.FromResult(Result(context, plan));
    }

    public Task<AiCallResult<IReadOnlyList<RecommendationDraft>>> RecommendAsync(
        MarketingContext context, DateTimeOffset windowStart, DateTimeOffset windowEnd, CancellationToken cancellationToken)
    {
        ThrowIfAskedToFail(context);

        var tz = TimeZoneInfo.FindSystemTimeZoneById(context.TimeZoneId);
        var time = BestTime(context);
        var platform = context.Channels.FirstOrDefault() ?? "instagram";
        var product = context.Products.FirstOrDefault();
        var offer = context.Offers.FirstOrDefault();
        var avoid = new JsonArray(context.ProhibitedTerms.Select(t => (JsonNode?)t).ToArray());
        var drafts = new List<RecommendationDraft>();

        drafts.Add(new RecommendationDraft(
            RecommendationAction.NewContent,
            new JsonObject
            {
                ["platform"] = platform,
                ["contentType"] = nameof(ContentType.Reel),
                ["format"] = nameof(ContentFormat.Reel),
                ["hook"] = nameof(HookType.Question),
                ["productId"] = product?.Id,
                ["captionIdea"] = product is null
                    ? $"A behind-the-scenes look at {context.BusinessName}"
                    : $"Have you tried our {product.Name}? Only {product.Price} {product.Currency}.",
                ["avoidTerms"] = avoid
            },
            Slot(windowStart, windowEnd, tz, time, 1),
            $"Short videos get the most reach for {context.Category} businesses; {time:HH:mm} is the evening peak in {context.City ?? "your city"}.",
            0.78m));

        if (offer is not null)
            drafts.Add(new RecommendationDraft(
                RecommendationAction.NewContent,
                new JsonObject
                {
                    ["platform"] = platform,
                    ["contentType"] = nameof(ContentType.Post),
                    ["format"] = nameof(ContentFormat.SingleImage),
                    ["hook"] = nameof(HookType.Offer),
                    ["offerId"] = offer.Id,
                    ["captionIdea"] = $"{offer.Title} - until {offer.EndsAt:dd MMM}.",
                    ["avoidTerms"] = avoid.DeepClone()
                },
                Slot(windowStart, windowEnd, tz, time, 2),
                "An active offer converts best when it is announced early with a clear end date.",
                0.72m));

        drafts.Add(new RecommendationDraft(
            RecommendationAction.Reschedule,
            new JsonObject { ["platform"] = platform, ["moveTo"] = time.ToString("HH:mm", CultureInfo.InvariantCulture) },
            null,
            "Posts published in the evening window get more engagement than morning posts.",
            0.61m));

        drafts.Add(new RecommendationDraft(
            RecommendationAction.ChangeFormat,
            new JsonObject { ["from"] = nameof(ContentFormat.SingleImage), ["to"] = nameof(ContentFormat.Carousel), ["topic"] = "menu" },
            null,
            "Carousels keep people on the post longer than single images for menus and price lists.",
            0.55m));

        return Task.FromResult(Result<IReadOnlyList<RecommendationDraft>>(context, drafts));
    }

    public Task<AiCallResult<ContentDraft>> WriteContentAsync(MarketingContext context, ContentBrief brief, CancellationToken cancellationToken)
    {
        ThrowIfAskedToFail(context);

        var format = brief.Format ?? DefaultFormat(brief.ContentType);
        var hook = brief.Hook ?? (brief.Offer is not null ? HookType.Offer : HookType.Question);
        var cta = CtaFor(context.Campaign?.Objective);
        var arabic = context.Dialect is null || context.Dialect.Contains("Arabic", StringComparison.OrdinalIgnoreCase);
        var emoji = context.EmojiLevel > 0;
        var shorter = brief.Instructions?.Contains("short", StringComparison.OrdinalIgnoreCase) == true;

        var subject = brief.Product?.Name ?? context.BusinessName;
        var lines = new List<string>();
        if (arabic)
        {
            lines.Add(hook == HookType.Offer && brief.Offer is not null
                ? $"{brief.Offer.Title}{(emoji ? " 🎉" : "")}"
                : $"جربت {subject} عنا؟{(emoji ? " ☕" : "")}");
            if (brief.Product is not null && !shorter) lines.Add($"{brief.Product.Name} بـ {brief.Product.Price} {brief.Product.Currency} بس.");
            if (brief.Offer is not null && hook != HookType.Offer) lines.Add($"والعرض: {brief.Offer.Title}");
            if (!shorter) lines.Add($"{context.BusinessName}{(context.City is null ? "" : " - " + context.City)}");
            lines.Add(CtaTextAr(cta) + (emoji ? " 👇" : ""));
        }
        else
        {
            lines.Add(hook == HookType.Offer && brief.Offer is not null ? brief.Offer.Title : $"Have you tried our {subject}?");
            if (brief.Product is not null && !shorter) lines.Add($"{brief.Product.Name} for only {brief.Product.Price} {brief.Product.Currency}.");
            if (!shorter) lines.Add(context.BusinessName);
            lines.Add(cta.ToString());
        }

        var draft = new ContentDraft(
            string.Join("\n", lines),
            format,
            hook,
            cta,
            brief.Offer is not null,
            new JsonObject
            {
                ["visualIdea"] = brief.Product is null ? $"Bright photo inside {context.BusinessName}" : $"Close-up of {brief.Product.Name} in natural light",
                ["hashtags"] = new JsonArray($"#{context.City ?? "local"}", $"#{context.Category}"),
                ["language"] = arabic ? "ar" : "en",
                ["basedOnRevision"] = brief.PreviousBody is not null
            });

        return Task.FromResult(Result(context, draft));
    }

    private static ContentFormat DefaultFormat(ContentType type) => type switch
    {
        ContentType.Reel => ContentFormat.Reel,
        ContentType.Story => ContentFormat.Story,
        ContentType.Short => ContentFormat.Video,
        ContentType.Post => ContentFormat.SingleImage,
        _ => ContentFormat.Text
    };

    private static CallToAction CtaFor(CampaignObjective? objective) => objective switch
    {
        CampaignObjective.Orders or CampaignObjective.ProductPromotion => CallToAction.OrderNow,
        CampaignObjective.Messages => CallToAction.SendMessage,
        CampaignObjective.Visits => CallToAction.VisitStore,
        CampaignObjective.Followers => CallToAction.Follow,
        _ => CallToAction.LearnMore
    };

    private static string CtaTextAr(CallToAction cta) => cta switch
    {
        CallToAction.OrderNow => "اطلب هلأ",
        CallToAction.SendMessage => "ابعتلنا رسالة",
        CallToAction.VisitStore => "زورنا اليوم",
        CallToAction.Follow => "تابعنا",
        _ => "اعرف أكثر"
    };

    // 19:00 local time, moved inside the owner's publish window when one is set.
    private static TimeOnly BestTime(MarketingContext context)
    {
        if (context.PublishWindowStart is null || context.PublishWindowEnd is null) return EveningSlot;
        var start = TimeOnly.ParseExact(context.PublishWindowStart, "HH:mm", CultureInfo.InvariantCulture);
        var end = TimeOnly.ParseExact(context.PublishWindowEnd, "HH:mm", CultureInfo.InvariantCulture);
        return EveningSlot >= start && EveningSlot < end ? EveningSlot : start.Add((end - start) / 2);
    }

    // The given local time, N days after the window start, in the business time zone, as UTC.
    private static DateTimeOffset? Slot(DateTimeOffset windowStart, DateTimeOffset windowEnd, TimeZoneInfo tz, TimeOnly time, int dayOffset)
    {
        var localDay = TimeZoneInfo.ConvertTime(windowStart, tz).Date.AddDays(dayOffset);
        var local = localDay + time.ToTimeSpan();
        var slot = new DateTimeOffset(local, tz.GetUtcOffset(local)).ToUniversalTime();
        return slot >= windowStart && slot < windowEnd ? slot : null;
    }

    private static string KpiFor(CampaignObjective objective) => objective switch
    {
        CampaignObjective.Orders => "orders",
        CampaignObjective.Messages => "messages",
        CampaignObjective.Visits => "store_visits",
        CampaignObjective.Awareness => "reach",
        CampaignObjective.ProductPromotion => "product_clicks",
        CampaignObjective.Followers => "new_followers",
        _ => "engagement"
    };

    private static void ThrowIfAskedToFail(MarketingContext context)
    {
        if (context.Campaign?.GoalStatement.Contains(FailureMarker, StringComparison.OrdinalIgnoreCase) == true)
            throw new AiProviderException("simulated_failure", "Simulated AI failure (test marker in the campaign goal).");
    }

    // Rough token estimate (about 4 characters per token), like a real provider would report.
    private static AiCallResult<T> Result<T>(MarketingContext context, T output) => new(
        output,
        Math.Max(1, JsonSerializer.Serialize(context).Length / 4),
        Math.Max(1, JsonSerializer.Serialize(output).Length / 4));
}
