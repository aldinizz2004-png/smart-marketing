using System.Text.Json.Nodes;
using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.Common.Ai;

// The only door to the AI, chosen in configuration (Ai:Provider): FakeMarketingAi for tests and demos,
// LlmMarketingAi for real models (Gemini, Groq ... with fallback). Services never know which one runs.
public interface IMarketingAi
{
    string ModelName { get; }
    string PromptVersion { get; }

    Task<AiCallResult<JsonObject>> PlanCampaignAsync(MarketingContext context, DateTimeOffset now, CancellationToken cancellationToken);

    Task<AiCallResult<IReadOnlyList<RecommendationDraft>>> RecommendAsync(
        MarketingContext context, DateTimeOffset windowStart, DateTimeOffset windowEnd, CancellationToken cancellationToken);

    Task<AiCallResult<ContentDraft>> WriteContentAsync(MarketingContext context, ContentBrief brief, CancellationToken cancellationToken);

    Task<AiCallResult<IReadOnlyList<CommentAnalysis>>> AnalyzeCommentsAsync(
        MarketingContext context, IReadOnlyList<CommentInput> comments, CancellationToken cancellationToken);

    Task<AiCallResult<OwnerReportDraft>> WriteOwnerReportAsync(
        MarketingContext context, OwnerReportFacts facts, CancellationToken cancellationToken);
}

public sealed record CommentInput(Guid Id, string PlatformCode, string? AuthorName, string Body, string? PostBody);

// SuggestedReply is null when the comment needs no answer (spam, a reply between customers ...).
public sealed record CommentAnalysis(Guid CommentId, CommentCategory Category, bool NeedsReply, string? SuggestedReply);

public sealed record AiCallResult<T>(T Output, int InputTokens, int OutputTokens)
{
    // Set by real providers: the model that actually answered (after any fallback) and its real cost.
    // When null, the runner uses IMarketingAi.ModelName and the prices in Ai options.
    public string? ModelName { get; init; }
    public decimal? CostUsd { get; init; }

    // Models that failed before the one that answered, e.g. "Gemini/gemini-3.6-flash: rate_limited".
    public IReadOnlyList<string> FailedAttempts { get; init; } = [];
}

public sealed record RecommendationDraft(
    RecommendationAction ActionType,
    JsonObject Payload,
    DateTimeOffset? ProposedPublishAt,
    string Rationale,
    decimal Confidence);

// What to write: where, what kind, about which product/offer, with which hook, and any extra instruction
// (e.g. "shorter"). PreviousBody is set when the AI rewrites an existing revision.
public sealed record ContentBrief(
    string PlatformCode,
    ContentType ContentType,
    ContentFormat? Format,
    HookType? Hook,
    ProductInfo? Product,
    OfferInfo? Offer,
    string? Instructions,
    string? PreviousBody);

public sealed record ContentDraft(
    string Body,
    ContentFormat Format,
    HookType? HookType,
    CallToAction? CallToAction,
    bool IncludesOffer,
    JsonObject CreativeDetails);
