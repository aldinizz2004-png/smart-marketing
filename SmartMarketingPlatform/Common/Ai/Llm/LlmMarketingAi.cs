using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using SmartMarketingPlatform.Enums;

namespace SmartMarketingPlatform.Common.Ai.Llm;

// The real AI. It turns a MarketingContext into a prompt, asks the model chain for JSON, and then
// checks the answer like it would check a junior marketer: only allowed platforms, only real product
// and offer ids, times inside the window, no banned words. An answer that fails these checks is
// treated as a provider failure, so the chain asks the next model instead of saving bad data.
public sealed class LlmMarketingAi(ChatModelChain chain) : IMarketingAi
{
    private static readonly JsonSerializerOptions PromptJson = new(JsonSerializerDefaults.Web)
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,   // keep Arabic readable (and cheaper in tokens)
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public string ModelName => "llm: " + chain.Description;
    public string PromptVersion => "llm-v1";

    // ---- campaign plan -------------------------------------------------------------

    public async Task<AiCallResult<JsonObject>> PlanCampaignAsync(MarketingContext context, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var campaign = context.Campaign ?? throw new ArgumentException("A campaign plan needs a campaign.", nameof(context));
        var tz = Zone(context);
        var start = campaign.StartsAt ?? now;
        var end = campaign.EndsAt ?? start.AddDays(14);
        var days = Math.Max(1, (int)Math.Ceiling((end - start).TotalDays));

        var user = $$"""
            TASK: Plan the marketing campaign described in BUSINESS.campaign.
            Today (business local time): {{Local(now, tz)}}. Campaign runs {{Local(start, tz)}} -> {{Local(end, tz)}} ({{days}} days).

            BUSINESS:
            {{Facts(context, tz)}}

            Return one JSON object:
            {
              "summary": string,            // 2-3 sentences for the owner, in the customer language
              "durationDays": {{days}},
              "channels": [ { "platform": one of {{List(context.Channels)}}, "postsPerWeek": int (max {{context.MaxPostsPerDay * 7}}), "bestTimes": ["HH:mm" local{{WindowHint(context)}}] } ],
              "weeklyThemes": [string],     // one per week
              "contentMix": { "Reel": int, "Post": int, "Story": int },   // percentages, sum 100
              "kpis": [string],             // e.g. "reach", "orders", "messages"
              "budgetSplit": [ { "platform": string, "amount": number, "currency": string } ],   // [] when there is no budget; amounts sum to the budget
              "postIdeas": [ { "week": int, "platform": string, "contentType": "Post|Reel|Story", "hook": {{EnumList<HookType>()}}, "idea": string } ]
            }
            """;

        var result = await chain.RunAsync(new ChatRequest(SystemPrompt(context), user, Temperature: 0.6), text =>
        {
            var plan = JsonNode.Parse(text) as JsonObject ?? throw Invalid("The plan is not a JSON object.");
            if (string.IsNullOrWhiteSpace(plan["summary"]?.ToString())) throw Invalid("The plan has no summary.");
            if (plan["channels"] is not JsonArray channels || channels.Count == 0) throw Invalid("The plan has no channels.");

            // Only platforms the owner allowed, with a sane number of posts.
            var kept = new JsonArray();
            foreach (var channel in channels.OfType<JsonObject>())
            {
                var platform = channel["platform"]?.ToString();
                if (platform is null || !context.Channels.Contains(platform, StringComparer.OrdinalIgnoreCase)) continue;
                var perWeek = channel["postsPerWeek"] is JsonValue v && v.TryGetValue<int>(out var n) ? n : 3;
                channel["platform"] = platform.ToLowerInvariant();
                channel["postsPerWeek"] = Math.Clamp(perWeek, 1, context.MaxPostsPerDay * 7);
                kept.Add(channel.DeepClone());
            }
            if (kept.Count == 0) throw Invalid("The plan uses no allowed platform.");
            plan["channels"] = kept;

            // The model's own text is checked first (keys that may legitimately list the banned words are
            // dropped), then the facts the model must not change are written by us, not by the model.
            plan.Remove("avoidTerms");
            plan.Remove("voice");
            ThrowIfProhibited(plan.ToJsonString(PromptJson), context);

            plan["objective"] = campaign.Objective.ToString();
            plan["durationDays"] = days;
            plan["voice"] = new JsonObject { ["dialect"] = context.Dialect, ["tone"] = context.Tone, ["emojiLevel"] = context.EmojiLevel };
            plan["avoidTerms"] = new JsonArray(context.ProhibitedTerms.Select(t => (JsonNode?)t).ToArray());
            return plan;
        }, cancellationToken);

        return ToCallResult(result);
    }

    // ---- recommendations -----------------------------------------------------------

    public async Task<AiCallResult<IReadOnlyList<RecommendationDraft>>> RecommendAsync(
        MarketingContext context, DateTimeOffset windowStart, DateTimeOffset windowEnd, CancellationToken cancellationToken)
    {
        var tz = Zone(context);
        var user = $$"""
            TASK: Recommend the next marketing actions for the period {{Local(windowStart, tz)}} -> {{Local(windowEnd, tz)}} (business local time).
            Give 2 to 5 concrete recommendations, the most valuable first.

            BUSINESS:
            {{Facts(context, tz)}}

            Return one JSON object:
            {
              "recommendations": [ {
                "actionType": {{EnumList<RecommendationAction>()}},
                "platform": one of {{List(context.Channels)}},
                "contentType": {{EnumList<ContentType>()}} or null,      // NewContent only
                "format": {{EnumList<ContentFormat>()}} or null,
                "hook": {{EnumList<HookType>()}} or null,
                "productId": one of the product ids above or null,
                "offerId": one of the offer ids above or null,
                "captionIdea": string or null,      // NewContent: the idea of the post, in the customer language
                "publishAtLocal": "yyyy-MM-ddTHH:mm" inside the period{{WindowHint(context)}}, or null,
                "details": object or null,          // other actions, e.g. { "moveTo": "19:00" } or { "from": "SingleImage", "to": "Carousel" }
                "rationale": string,                // why, for the owner, in the customer language
                "confidence": number between 0 and 1
              } ]
            }
            """;

        var result = await chain.RunAsync(new ChatRequest(SystemPrompt(context), user, Temperature: 0.4), text =>
        {
            var root = JsonNode.Parse(text) as JsonObject ?? throw Invalid("The answer is not a JSON object.");
            var items = root["recommendations"] as JsonArray ?? throw Invalid("No recommendations array.");
            var productIds = context.Products.Select(p => p.Id).ToHashSet();
            var offerIds = context.Offers.Select(o => o.Id).ToHashSet();
            var avoid = new JsonArray(context.ProhibitedTerms.Select(t => (JsonNode?)t).ToArray());
            var drafts = new List<RecommendationDraft>();

            foreach (var item in items.OfType<JsonObject>().Take(5))
            {
                if (!TryEnum<RecommendationAction>(item["actionType"], out var action)) continue;
                var rationale = item["rationale"]?.ToString();
                if (string.IsNullOrWhiteSpace(rationale)) continue;

                var platform = item["platform"]?.ToString();
                platform = platform is not null && context.Channels.Contains(platform, StringComparer.OrdinalIgnoreCase)
                    ? platform.ToLowerInvariant()
                    : context.Channels.FirstOrDefault() ?? "instagram";

                // The same payload keys the content service reads when a recommendation is applied.
                var payload = new JsonObject { ["platform"] = platform };
                if (action == RecommendationAction.NewContent)
                {
                    payload["contentType"] = TryEnum<ContentType>(item["contentType"], out var ct) ? ct.ToString() : nameof(ContentType.Post);
                    payload["format"] = TryEnum<ContentFormat>(item["format"], out var f) ? f.ToString() : null;
                    payload["hook"] = TryEnum<HookType>(item["hook"], out var h) ? h.ToString() : null;
                    payload["productId"] = KnownId(item["productId"], productIds);   // an invented id is dropped
                    payload["offerId"] = KnownId(item["offerId"], offerIds);
                    payload["captionIdea"] = item["captionIdea"]?.ToString();
                    payload["avoidTerms"] = avoid.DeepClone();
                }
                else if (item["details"] is JsonObject details)
                {
                    foreach (var (key, value) in details)
                        if (key != "platform") payload[key] = value?.DeepClone();
                }

                var confidence = item["confidence"] is JsonValue cv && cv.TryGetValue<decimal>(out var c) ? Math.Clamp(c, 0m, 1m) : 0.5m;
                drafts.Add(new RecommendationDraft(
                    action, payload, PublishAt(item["publishAtLocal"], tz, windowStart, windowEnd), rationale.Trim(), Math.Round(confidence, 2)));
            }

            if (drafts.Count == 0) throw Invalid("No usable recommendation in the answer.");
            ThrowIfProhibited(string.Join("\n", drafts.Select(d => d.Rationale + " " + d.Payload["captionIdea"])), context);
            return (IReadOnlyList<RecommendationDraft>)drafts;
        }, cancellationToken);

        return ToCallResult(result);
    }

    // ---- writing a post ------------------------------------------------------------

    public async Task<AiCallResult<ContentDraft>> WriteContentAsync(MarketingContext context, ContentBrief brief, CancellationToken cancellationToken)
    {
        var tz = Zone(context);
        var task = brief.PreviousBody is null
            ? "TASK: Write one ready-to-publish social media text."
            : $"TASK: Rewrite the text below following the instructions. Keep what is good in it.\nCURRENT TEXT:\n{brief.PreviousBody}";

        var user = $$"""
            {{task}}

            BRIEF:
            {{JsonSerializer.Serialize(new
            {
                platform = brief.PlatformCode,
                contentType = brief.ContentType.ToString(),
                format = brief.Format?.ToString() ?? "choose the best",
                hook = brief.Hook?.ToString() ?? "choose the best",
                product = brief.Product,
                offer = brief.Offer is null ? null : new { brief.Offer.Id, brief.Offer.Title, brief.Offer.DiscountPercent, brief.Offer.OfferPrice, endsAt = Local(brief.Offer.EndsAt, tz) },
                instructions = brief.Instructions
            }, PromptJson)}}

            BUSINESS:
            {{Facts(context, tz)}}

            Writing rules:
            - Fit the platform: Instagram/Facebook up to ~6 short lines; TikTok/Reel captions 1-3 lines; WhatsApp messages short and personal.
            - Start with a strong first line that matches the hook.
            - If a product or offer is given, use its exact name and price. Never invent a price, a discount or a date.
            - End with one clear call to action. Put hashtags only in "hashtags", not in "body".

            Return one JSON object:
            {
              "body": string,
              "format": {{EnumList<ContentFormat>()}},
              "hookType": {{EnumList<HookType>()}},
              "callToAction": {{EnumList<CallToAction>()}},
              "includesOffer": boolean,
              "visualIdea": string,       // the photo or video to shoot, one sentence
              "hashtags": [string],       // 3 to 6
              "language": "ar" or "en"
            }
            """;

        var result = await chain.RunAsync(new ChatRequest(SystemPrompt(context), user, Temperature: 0.8), text =>
        {
            var root = JsonNode.Parse(text) as JsonObject ?? throw Invalid("The answer is not a JSON object.");
            var body = root["body"]?.ToString()?.Trim();
            if (string.IsNullOrWhiteSpace(body)) throw Invalid("The text is empty.");
            if (body.Length > 2200) throw Invalid("The text is longer than the platforms allow.");
            ThrowIfProhibited(body, context);

            // What the owner asked for wins over what the model chose.
            var format = brief.Format ?? (TryEnum<ContentFormat>(root["format"], out var f) ? f : DefaultFormat(brief.ContentType));
            var hook = brief.Hook ?? (TryEnum<HookType>(root["hookType"], out var h) ? (HookType?)h : null);
            CallToAction? cta = TryEnum<CallToAction>(root["callToAction"], out var c) ? c : null;

            var hashtags = new JsonArray();
            foreach (var tag in (root["hashtags"] as JsonArray ?? new JsonArray()).Select(t => t?.ToString()?.Trim()).Where(t => !string.IsNullOrEmpty(t)).Take(6))
                hashtags.Add(tag!.StartsWith('#') ? tag : "#" + tag.Replace(" ", "_"));

            return new ContentDraft(
                body,
                format,
                hook,
                cta,
                brief.Offer is not null,
                new JsonObject
                {
                    ["visualIdea"] = root["visualIdea"]?.ToString(),
                    ["hashtags"] = hashtags,
                    ["language"] = root["language"]?.ToString() ?? "ar",
                    ["basedOnRevision"] = brief.PreviousBody is not null
                });
        }, cancellationToken);

        return ToCallResult(result);
    }

    // ---- prompt pieces -----------------------------------------------------------------

    private static string SystemPrompt(MarketingContext context)
    {
        var language = string.IsNullOrWhiteSpace(context.Dialect) ? "Arabic" : context.Dialect;
        return $"""
            You are the social media marketing manager of a small local business ({context.Category}).
            You plan campaigns, write posts and recommend what to do next, based on the BUSINESS facts you are given.
            Rules:
            - Use only the facts given (products, prices, offers, dates, platforms). Never invent prices, discounts, products, phone numbers, addresses or health claims.
            - Never use any word or phrase from prohibitedTerms, in any form. Use preferredTerms where they fit naturally.
            - Customer-facing text is written in {language}{(string.IsNullOrWhiteSpace(context.Tone) ? "" : $", with a {context.Tone} tone")}.
            - Emoji level {context.EmojiLevel} (0 = no emojis; higher = more, but never more than one per line).
            - Answer with ONE JSON object in exactly the requested shape. No markdown, no comments, no text outside the JSON.
            """;
    }

    // Everything the model may rely on, as compact JSON. Ids are included so answers can refer to them.
    private static string Facts(MarketingContext c, TimeZoneInfo tz) => JsonSerializer.Serialize(new
    {
        name = c.BusinessName,
        category = c.Category,
        city = c.City,
        currency = c.Currency,
        voice = new { dialect = c.Dialect, tone = c.Tone, emojiLevel = c.EmojiLevel, preferredTerms = c.PreferredTerms, prohibitedTerms = c.ProhibitedTerms },
        products = c.Products.Select(p => new { id = p.Id, name = p.Name, price = p.Price, currency = p.Currency }),
        offers = c.Offers.Select(o => new { id = o.Id, title = o.Title, discountPercent = o.DiscountPercent, offerPrice = o.OfferPrice, endsAt = Local(o.EndsAt, tz) }),
        audiences = c.Audiences.Select(a => new { name = a.Name, description = a.Description }),
        platforms = c.Channels,
        limits = new { maxPostsPerDay = c.MaxPostsPerDay, publishWindow = c.PublishWindowStart is null ? null : $"{c.PublishWindowStart}-{c.PublishWindowEnd}" },
        campaign = c.Campaign is null ? null : new
        {
            name = c.Campaign.Name,
            objective = c.Campaign.Objective.ToString(),
            goal = c.Campaign.GoalStatement,
            budget = c.Campaign.Budget,
            budgetCurrency = c.Campaign.BudgetCurrency
        }
    }, PromptJson);

    private static string WindowHint(MarketingContext c) =>
        c.PublishWindowStart is null ? "" : $", between {c.PublishWindowStart} and {c.PublishWindowEnd}";

    private static string List(IEnumerable<string> values) => "[" + string.Join(", ", values.Select(v => $"\"{v}\"")) + "]";
    private static string EnumList<T>() where T : struct, Enum => string.Join("|", Enum.GetNames<T>());

    // ---- answer checks -----------------------------------------------------------------

    private static void ThrowIfProhibited(string text, MarketingContext context)
    {
        var found = context.ProhibitedTerms.Where(t => !string.IsNullOrWhiteSpace(t) && text.Contains(t, StringComparison.OrdinalIgnoreCase)).ToList();
        if (found.Count > 0)
            throw new AiProviderException("prohibited_terms", $"The answer used prohibited words: {string.Join(", ", found)}.");
    }

    private static AiProviderException Invalid(string message) => new("invalid_output", message);

    private static bool TryEnum<T>(JsonNode? node, out T value) where T : struct, Enum
    {
        value = default;
        var text = node is JsonValue v && v.TryGetValue<string>(out var s) ? s : null;
        return text is not null && !int.TryParse(text, out _) && Enum.TryParse(text.Trim(), ignoreCase: true, out value) && Enum.IsDefined(value);
    }

    private static Guid? KnownId(JsonNode? node, HashSet<Guid> known) =>
        node is JsonValue v && v.TryGetValue<string>(out var s) && Guid.TryParse(s, out var id) && known.Contains(id) ? id : null;

    // "2026-11-05T19:00" in the business time zone -> UTC, only when it falls inside the period.
    private static DateTimeOffset? PublishAt(JsonNode? node, TimeZoneInfo tz, DateTimeOffset windowStart, DateTimeOffset windowEnd)
    {
        if (node is not JsonValue v || !v.TryGetValue<string>(out var s)) return null;
        if (!DateTime.TryParseExact(s.Trim(), ["yyyy-MM-ddTHH:mm", "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-dd HH:mm"],
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var local)) return null;
        if (tz.IsInvalidTime(local)) return null;
        var utc = new DateTimeOffset(local, tz.GetUtcOffset(local)).ToUniversalTime();
        return utc >= windowStart && utc < windowEnd ? utc : null;
    }

    private static ContentFormat DefaultFormat(ContentType type) => type switch
    {
        ContentType.Reel => ContentFormat.Reel,
        ContentType.Story => ContentFormat.Story,
        ContentType.Short => ContentFormat.Video,
        ContentType.Post => ContentFormat.SingleImage,
        _ => ContentFormat.Text
    };

    private static TimeZoneInfo Zone(MarketingContext c) => TimeZoneInfo.FindSystemTimeZoneById(c.TimeZoneId);

    private static string Local(DateTimeOffset value, TimeZoneInfo tz) =>
        TimeZoneInfo.ConvertTime(value, tz).ToString("yyyy-MM-dd HH:mm (dddd)", CultureInfo.InvariantCulture);

    private static AiCallResult<T> ToCallResult<T>(ChainResult<T> r) =>
        new(r.Output, r.Completion.InputTokens, r.Completion.OutputTokens)
        {
            ModelName = r.ModelName,
            CostUsd = r.CostUsd,
            FailedAttempts = r.FailedAttempts
        };
}
