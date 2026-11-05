using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using SmartMarketingPlatform.Common.Competitors;
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
                "confidence": number between 0 and 1,
                "insightIds": [ids from marketInsights this recommendation follows] or []
              } ]
            }
            """;

        var result = await chain.RunAsync(new ChatRequest(SystemPrompt(context), user, Temperature: 0.4), text =>
        {
            var root = JsonNode.Parse(text) as JsonObject ?? throw Invalid("The answer is not a JSON object.");
            var items = root["recommendations"] as JsonArray ?? throw Invalid("No recommendations array.");
            var productIds = context.Products.Select(p => p.Id).ToHashSet();
            var offerIds = context.Offers.Select(o => o.Id).ToHashSet();
            var insightIds = context.Insights.Select(i => i.Id).ToHashSet();
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
                    action, payload, PublishAt(item["publishAtLocal"], tz, windowStart, windowEnd), rationale.Trim(), Math.Round(confidence, 2))
                {
                    InsightIds = KnownIds(item["insightIds"], insightIds)
                });
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
                instructions = brief.Instructions,
                attachedPhotos = brief.Photos.Count == 0 ? null : brief.Photos
            }, PromptJson)}}

            BUSINESS:
            {{Facts(context, tz)}}

            Writing rules:
            - Fit the platform: Instagram/Facebook up to ~6 short lines; TikTok/Reel captions 1-3 lines; WhatsApp messages short and personal.
            - Start with a strong first line that matches the hook.
            - If a product or offer is given, use its exact name and price. Never invent a price, a discount or a date.
            - attachedPhotos describe the pictures published with this text: the text must match them and never mention what they do not show.
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

    // ---- comments ----------------------------------------------------------------------

    public async Task<AiCallResult<IReadOnlyList<CommentAnalysis>>> AnalyzeCommentsAsync(
        MarketingContext context, IReadOnlyList<CommentInput> comments, CancellationToken cancellationToken)
    {
        var tz = Zone(context);
        var user = $$"""
            TASK: Read the customer comments below, classify each one and, when it deserves an answer,
            write the public reply the business would post under it.

            Reply rules:
            - Short (one or two sentences), warm, in the customer language, signed by nobody.
            - Complaints: apologize and move the conversation to private messages; never argue in public.
            - Questions about prices: answer only with prices from BUSINESS; otherwise invite a private message.
            - Spam, insults and comments between customers: needsReply false, suggestedReply null.

            COMMENTS:
            {{JsonSerializer.Serialize(comments.Select(c => new { id = c.Id, platform = c.PlatformCode, author = c.AuthorName, comment = c.Body, ourPost = c.PostBody }), PromptJson)}}

            BUSINESS:
            {{Facts(context, tz)}}

            Return one JSON object:
            {
              "comments": [ {
                "id": one of the comment ids above,
                "category": {{EnumList<CommentCategory>()}},
                "needsReply": boolean,
                "suggestedReply": string or null
              } ]
            }
            """;

        var result = await chain.RunAsync(new ChatRequest(SystemPrompt(context), user, Temperature: 0.5), text =>
        {
            var root = JsonNode.Parse(text) as JsonObject ?? throw Invalid("The answer is not a JSON object.");
            var items = root["comments"] as JsonArray ?? throw Invalid("No comments array.");
            var known = comments.Select(c => c.Id).ToHashSet();
            var analyses = new Dictionary<Guid, CommentAnalysis>();

            foreach (var item in items.OfType<JsonObject>())
            {
                if (!Guid.TryParse(item["id"]?.ToString(), out var id) || !known.Contains(id) || analyses.ContainsKey(id)) continue;
                if (!TryEnum<CommentCategory>(item["category"], out var category)) continue;

                var reply = item["suggestedReply"]?.ToString()?.Trim();
                var needsReply = item["needsReply"] is JsonValue v && v.TryGetValue<bool>(out var b) && b && !string.IsNullOrEmpty(reply);
                if (reply is { Length: > 1000 }) throw Invalid("A suggested reply is too long.");
                analyses[id] = new CommentAnalysis(id, category, needsReply, needsReply ? reply : null);
            }

            if (analyses.Count == 0) throw Invalid("No usable comment analysis in the answer.");
            ThrowIfProhibited(string.Join("\n", analyses.Values.Select(a => a.SuggestedReply)), context);
            return (IReadOnlyList<CommentAnalysis>)analyses.Values.ToList();
        }, cancellationToken);

        return ToCallResult(result);
    }

    // ---- owner report ------------------------------------------------------------------

    public async Task<AiCallResult<OwnerReportDraft>> WriteOwnerReportAsync(MarketingContext context, OwnerReportFacts facts, CancellationToken cancellationToken)
    {
        var tz = Zone(context);
        var user = $$"""
            TASK: Write the periodic marketing report for the OWNER of the business (not for customers).
            Explain what happened, what worked, what needs attention, and what to do next.

            Rules:
            - Use only the numbers in FACTS. Never invent numbers, posts or results. Engagement rates are fractions (0.05 = 5%).
            - Plain words for a busy shop owner; no marketing jargon.
            - nextActions are concrete things the owner can do this week, most important first.
            - Write in {{(string.IsNullOrWhiteSpace(context.Dialect) ? "Arabic" : context.Dialect)}}.

            FACTS:
            {{JsonSerializer.Serialize(facts, PromptJson)}}

            BUSINESS:
            {{Facts(context, tz)}}

            Return one JSON object:
            {
              "headline": string,        // one short line
              "summary": string,         // 2-4 sentences
              "wins": [string],          // 0-4 items
              "concerns": [string],      // 0-4 items
              "nextActions": [string]    // 1-5 items
            }
            """;

        var result = await chain.RunAsync(new ChatRequest(SystemPrompt(context), user, Temperature: 0.4), text =>
        {
            var root = JsonNode.Parse(text) as JsonObject ?? throw Invalid("The answer is not a JSON object.");
            var headline = root["headline"]?.ToString()?.Trim();
            var summary = root["summary"]?.ToString()?.Trim();
            if (string.IsNullOrWhiteSpace(headline) || headline.Length > 200) throw Invalid("Missing or too long headline.");
            if (string.IsNullOrWhiteSpace(summary) || summary.Length > 1500) throw Invalid("Missing or too long summary.");

            var draft = new OwnerReportDraft(headline, summary, Items(root["wins"], 4), Items(root["concerns"], 4), Items(root["nextActions"], 5));
            if (draft.NextActions.Count == 0) throw Invalid("A report needs at least one next action.");

            ThrowIfProhibited(string.Join("\n", [draft.Headline, draft.Summary, .. draft.Wins, .. draft.Concerns, .. draft.NextActions]), context);
            return draft;
        }, cancellationToken);

        return ToCallResult(result);
    }

    private static IReadOnlyList<string> Items(JsonNode? node, int max) =>
        (node as JsonArray ?? new JsonArray())
            .Select(i => i?.ToString()?.Trim())
            .Where(i => !string.IsNullOrEmpty(i) && i.Length <= 300)
            .Take(max)
            .ToList()!;

    // ---- competitors -------------------------------------------------------------------

    public async Task<AiCallResult<IReadOnlyList<CompetitorInsightDraft>>> AnalyzeCompetitorsAsync(
        MarketingContext context, CompetitorFacts facts, CancellationToken cancellationToken)
    {
        var tz = Zone(context);
        var user = $$"""
            TASK: The owner tracks other accounts (competitors and inspirations) to LEARN from them, not to copy them.
            FACTS are statistics computed from their recent public posts. Find the 3 to 6 patterns that matter most
            and say how OUR business should apply each one with OUR products, offers and voice.

            Rules:
            - Patterns only: timing, formats, hooks, frequency, hashtags, offers, tone. Use only numbers present in FACTS.
            - Engagement rates and lifts are for the owner: write 0.0395 as 4% and a lift of 1.22 as "22% more than usual".
            - Never repeat their captions or slogans, and never mention their products, prices or offers as ours.
            - applyToUs names one of OUR products or offers from BUSINESS when it fits, and respects our limits (max posts per day, publish window).
            - A pattern seen in few posts gets a low confidence.
            - Write summary and applyToUs in {{(string.IsNullOrWhiteSpace(context.Dialect) ? "Arabic" : context.Dialect)}}, for the owner.

            FACTS:
            {{JsonSerializer.Serialize(facts, PromptJson)}}

            BUSINESS:
            {{Facts(context with { Insights = [] }, tz)}}

            Return one JSON object:
            {
              "insights": [ {
                "type": {{EnumList<InsightType>()}},
                "summary": string,              // the pattern, with its number (e.g. "reels get 2.1x the usual engagement")
                "applyToUs": string,            // the concrete thing we do
                "confidence": number between 0 and 1,
                "evidencePostIds": [ids from FACTS.topPosts] or []
              } ]
            }
            """;

        var result = await chain.RunAsync(new ChatRequest(SystemPrompt(context), user, Temperature: 0.3), text =>
        {
            var root = JsonNode.Parse(text) as JsonObject ?? throw Invalid("The answer is not a JSON object.");
            var items = root["insights"] as JsonArray ?? throw Invalid("No insights array.");
            var postIds = facts.TopPosts.Select(p => p.Id).ToHashSet();
            var insights = new List<CompetitorInsightDraft>();

            foreach (var item in items.OfType<JsonObject>().Take(6))
            {
                if (!TryEnum<InsightType>(item["type"], out var type)) continue;
                var summary = item["summary"]?.ToString()?.Trim();
                var apply = item["applyToUs"]?.ToString()?.Trim();
                if (string.IsNullOrWhiteSpace(summary) || string.IsNullOrWhiteSpace(apply)) continue;
                if (summary.Length > 600 || apply.Length > 500) throw Invalid("An insight is too long.");

                var confidence = item["confidence"] is JsonValue cv && cv.TryGetValue<decimal>(out var c) ? Math.Clamp(c, 0m, 1m) : 0.5m;
                insights.Add(new CompetitorInsightDraft(type, summary, apply, Math.Round(confidence, 2), KnownIds(item["evidencePostIds"], postIds)));
            }

            if (insights.Count == 0) throw Invalid("No usable insight in the answer.");
            ThrowIfProhibited(string.Join("\n", insights.Select(i => i.Summary + "\n" + i.ApplyToUs)), context);
            ThrowIfCopied(insights.Select(i => i.ApplyToUs), facts);
            return (IReadOnlyList<CompetitorInsightDraft>)insights;
        }, cancellationToken);

        return ToCallResult(result);
    }

    // ---- WhatsApp ----------------------------------------------------------------------

    public async Task<AiCallResult<CustomerReplyDraft>> ReplyToCustomerAsync(MarketingContext context, ChatThread thread, CancellationToken cancellationToken)
    {
        var tz = Zone(context);
        var user = $$"""
            TASK: You answer the business's WhatsApp messages. Read the CONVERSATION (oldest first) and write the next reply to the customer.

            Rules:
            - Answer only from BUSINESS (products, prices, offers, city). If the answer is not there, do not guess: needsOwner = true.
            - Orders (buy, book, delivery), complaints, refunds, special requests, payment or personal details: needsOwner = true,
              and reply is a short polite message the owner can send while taking over.
            - Greetings and questions BUSINESS answers fully: needsOwner = false.
            - One to three short sentences, warm, in the customer's language and dialect. If asked, say you are the shop's automatic assistant.
            - confidence: how sure you are that the reply is correct and complete (0 to 1).

            CONVERSATION:
            {{JsonSerializer.Serialize(new { customer = thread.CustomerName, messages = thread.Lines.Select(l => new { from = l.FromCustomer ? "customer" : "business", text = l.Text, time = l.LocalTime }) }, PromptJson)}}

            BUSINESS:
            {{Facts(context, tz)}}

            Return one JSON object:
            {
              "intent": {{EnumList<ConversationIntent>()}},
              "reply": string or null,
              "needsOwner": boolean,
              "handoffReason": string or null,     // for the owner, when needsOwner is true
              "confidence": number between 0 and 1
            }
            """;

        var result = await chain.RunAsync(new ChatRequest(SystemPrompt(context), user, Temperature: 0.4), text =>
        {
            var root = JsonNode.Parse(text) as JsonObject ?? throw Invalid("The answer is not a JSON object.");
            if (!TryEnum<ConversationIntent>(root["intent"], out var intent)) throw Invalid("Unknown intent.");
            var reply = root["reply"]?.ToString()?.Trim();
            var needsOwner = root["needsOwner"] is JsonValue v && v.TryGetValue<bool>(out var b) ? b : true;
            var reason = root["handoffReason"]?.ToString()?.Trim();
            if (reply is { Length: > 1000 }) throw Invalid("The reply is too long.");
            if (!needsOwner && string.IsNullOrWhiteSpace(reply)) throw Invalid("No reply and no handoff.");
            if (!string.IsNullOrEmpty(reply)) ThrowIfProhibited(reply, context);

            // A number the facts do not contain may be an invented price: a person checks it first.
            if (!needsOwner && UnknownNumber(reply!, context) is { } number)
            {
                needsOwner = true;
                reason = $"The reply mentions {number}, which is not in the business facts.";
            }

            var confidence = root["confidence"] is JsonValue cv && cv.TryGetValue<decimal>(out var c) ? Math.Clamp(c, 0m, 1m) : 0.5m;
            return new CustomerReplyDraft(intent, string.IsNullOrEmpty(reply) ? null : reply, needsOwner,
                needsOwner ? Truncate(reason ?? "The AI is not sure.", 300) : null, Math.Round(confidence, 2));
        }, cancellationToken);

        return ToCallResult(result);
    }

    // ---- photos ------------------------------------------------------------------------

    public async Task<AiCallResult<ImageDescriptionDraft>> DescribeImageAsync(
        MarketingContext context, ImageInput image, ProductInfo? product, CancellationToken cancellationToken)
    {
        var language = string.IsNullOrWhiteSpace(context.Dialect) ? "Arabic" : context.Dialect;
        var user = $$"""
            TASK: Look at the attached photo of the business {{context.BusinessName}} ({{context.Category}}).
            {{(product is null ? "It is a general photo of the business." : $"It should show the product \"{product.Name}\".")}}

            1. Describe it for a copywriter who will write posts about it WITHOUT seeing it: what is shown, colors,
               presentation, setting, mood. Facts only, 1 to 3 sentences, in {{language}}.
            2. Judge it as an ad photo: list only the problems you really see.
            3. If there are problems, give the owner ONE practical tip to take a better photo, in {{language}}.

            Return one JSON object:
            {
              "description": string,
              "showsProduct": boolean,          // the product is clearly visible
              "issues": [{{EnumList<ImageQualityIssue>()}}],
              "advice": string or null
            }
            """;

        var request = new ChatRequest(SystemPrompt(context), user, Temperature: 0.2) { Images = [new ChatImage(image.MimeType, image.Data)] };
        var result = await chain.RunAsync(request, text =>
        {
            var root = JsonNode.Parse(text) as JsonObject ?? throw Invalid("The answer is not a JSON object.");
            var description = root["description"]?.ToString()?.Trim();
            if (string.IsNullOrWhiteSpace(description) || description.Length > 1000) throw Invalid("Missing or too long description.");
            var advice = root["advice"]?.ToString()?.Trim();
            if (advice is { Length: > 500 }) throw Invalid("The advice is too long.");

            // Resolution is measured, so the model's opinion about it is replaced by the measurement.
            var issues = (root["issues"] as JsonArray ?? new JsonArray())
                .Select(i => TryEnum<ImageQualityIssue>(i, out var issue) ? issue : (ImageQualityIssue?)null)
                .OfType<ImageQualityIssue>()
                .Where(i => i != ImageQualityIssue.LowResolution)
                .ToList();
            if (image.IsLowResolution) issues.Insert(0, ImageQualityIssue.LowResolution);

            var showsProduct = root["showsProduct"] is JsonValue v && v.TryGetValue<bool>(out var b) && b;
            return new ImageDescriptionDraft(description, showsProduct, issues.Distinct().ToList(), string.IsNullOrEmpty(advice) ? null : advice);
        }, cancellationToken);

        return ToCallResult(result);
    }

    internal static string? UnknownNumber(string reply, MarketingContext context)
    {
        var known = context.Products.Select(p => p.Price)
            .Concat(context.Offers.SelectMany(o => new[] { o.OfferPrice, o.DiscountPercent }).OfType<decimal>())
            .Select(n => n.ToString("0.##", CultureInfo.InvariantCulture))
            .ToHashSet();

        var digits = new string(reply.Select(ch => ch is >= '٠' and <= '٩' ? (char)('0' + ch - '٠') : ch).ToArray());
        return System.Text.RegularExpressions.Regex.Matches(digits, @"\d+(?:\.\d+)?")
            .Select(m => m.Value)
            .FirstOrDefault(n => decimal.TryParse(n, NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
                                 && value >= 10 && !known.Contains(value.ToString("0.##", CultureInfo.InvariantCulture)));
    }

    private static string Truncate(string value, int max) => value.Length <= max ? value : value[..max];

    // Advice that repeats a competitor caption is not learning, it is copying: ask another model.
    private static void ThrowIfCopied(IEnumerable<string> texts, CompetitorFacts facts)
    {
        foreach (var text in texts)
            foreach (var post in facts.TopPosts.Where(p => p.Excerpt is not null))
                if (PostText.Containment(text, post.Excerpt!) >= 0.5)
                    throw new AiProviderException("copied_text", "The answer repeats a competitor caption.");
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
            - marketInsights are patterns learned from other accounts (timing, formats, hooks, frequency). Follow the patterns with OUR products, prices and voice; never copy another account's words, products, prices or offers.
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
        products = c.Products.Select(p => new { id = p.Id, name = p.Name, price = p.Price, currency = p.Currency, look = p.Look }),
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
        },
        marketInsights = c.Insights.Count == 0 ? null : c.Insights.Select(i => new { id = i.Id, type = i.Type.ToString(), pattern = i.Summary, applyToUs = i.ApplyToUs })
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

    private static IReadOnlyList<Guid> KnownIds(JsonNode? node, HashSet<Guid> known) =>
        (node as JsonArray ?? new JsonArray())
            .Select(n => n is JsonValue v && v.TryGetValue<string>(out var s) && Guid.TryParse(s, out var id) && known.Contains(id) ? id : Guid.Empty)
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

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
