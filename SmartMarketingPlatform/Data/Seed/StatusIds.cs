namespace SmartMarketingPlatform.Data.Seed;

// Fixed status ids. Use StatusIds.Campaign.Active in code instead of magic numbers.
public static class StatusIds
{
    public static class Tenant { public const int Active = 101, Suspended = 102, Closed = 103; }
    public static class Membership { public const int Invited = 201, Active = 202, Disabled = 203; }
    public static class Subscription { public const int Pending = 301, Active = 302, Expired = 303, Canceled = 304; }
    public static class Business { public const int Draft = 401, Active = 402, Paused = 403, Closed = 404; }
    public static class Offer { public const int Draft = 501, Active = 502, Paused = 503, Ended = 504; }
    public static class KnowledgeSource { public const int Pending = 601, Processed = 602, Failed = 603, Disabled = 604; }
    public static class SocialAccount { public const int Connected = 701, Expired = 702, Disconnected = 703; }
    public static class TrackedSource { public const int Pending = 801, Active = 802, Paused = 803, Failed = 804; }
    public static class AiExecution { public const int Queued = 901, Running = 902, Succeeded = 903, Failed = 904; }
    public static class Campaign { public const int Draft = 1001, Planning = 1002, Ready = 1003, Active = 1004, Paused = 1005, Completed = 1006, Canceled = 1007; }
    public static class StrategyRun { public const int Queued = 1101, Running = 1102, Completed = 1103, Failed = 1104; }
    public static class Recommendation { public const int Pending = 1201, Accepted = 1202, Rejected = 1203, Applied = 1204; }
    public static class ContentItem { public const int Draft = 1301, InReview = 1302, Approved = 1303, Scheduled = 1304, Published = 1305, Canceled = 1306; }
    public static class ContentReview { public const int Approved = 1401, Rejected = 1402, ChangesRequested = 1403; }
    public static class PublicationJob { public const int Queued = 1501, Running = 1502, Succeeded = 1503, Retry = 1504, Failed = 1505, Canceled = 1506; }
}