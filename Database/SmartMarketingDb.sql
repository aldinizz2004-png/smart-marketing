IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE TABLE [AppUsers] (
    [Id] uniqueidentifier NOT NULL,
    [AuthSubject] nvarchar(200) NOT NULL,
    [Email] nvarchar(256) NOT NULL,
    [FullName] nvarchar(200) NULL,
    [PreferredLanguage] nvarchar(5) NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    CONSTRAINT [PK_AppUsers] PRIMARY KEY ([Id])
);

CREATE TABLE [Platforms] (
    [Code] nvarchar(30) NOT NULL,
    [DisplayNameKey] nvarchar(100) NOT NULL,
    [SupportsPublishing] bit NOT NULL,
    [SupportsMessaging] bit NOT NULL,
    [SupportsCompetitorTracking] bit NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Platforms] PRIMARY KEY ([Code])
);

CREATE TABLE [Statuses] (
    [Id] int NOT NULL,
    [Category] nvarchar(50) NOT NULL,
    [Code] nvarchar(50) NOT NULL,
    [TranslationKey] nvarchar(150) NOT NULL,
    [SortOrder] int NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Statuses] PRIMARY KEY ([Id])
);

CREATE TABLE [PlatformDomains] (
    [Domain] nvarchar(100) NOT NULL,
    [PlatformCode] nvarchar(30) NOT NULL,
    CONSTRAINT [PK_PlatformDomains] PRIMARY KEY ([Domain]),
    CONSTRAINT [FK_PlatformDomains_Platforms_PlatformCode] FOREIGN KEY ([PlatformCode]) REFERENCES [Platforms] ([Code]) ON DELETE NO ACTION
);

CREATE TABLE [Tenants] (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [StatusId] int NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    CONSTRAINT [PK_Tenants] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Tenants_Statuses_StatusId] FOREIGN KEY ([StatusId]) REFERENCES [Statuses] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Businesses] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [Category] nvarchar(100) NOT NULL,
    [CountryCode] nchar(2) NULL,
    [City] nvarchar(100) NULL,
    [Address] nvarchar(300) NULL,
    [TimeZoneId] nvarchar(64) NOT NULL,
    [DefaultCurrency] nchar(3) NOT NULL,
    [ContactPhone] nvarchar(30) NULL,
    [Description] nvarchar(2000) NULL,
    [StatusId] int NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [UpdatedAt] datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    CONSTRAINT [PK_Businesses] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Businesses_Statuses_StatusId] FOREIGN KEY ([StatusId]) REFERENCES [Statuses] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Businesses_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Subscriptions] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [Duration] tinyint NOT NULL,
    [Price] decimal(12,2) NOT NULL,
    [Currency] nchar(3) NOT NULL,
    [StartsAt] datetimeoffset NOT NULL,
    [EndsAt] datetimeoffset NOT NULL,
    [StatusId] int NOT NULL,
    [RenewedFromSubscriptionId] uniqueidentifier NULL,
    [CanceledAt] datetimeoffset NULL,
    [PaymentReference] nvarchar(200) NULL,
    [CreatedAt] datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    CONSTRAINT [PK_Subscriptions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Subscriptions_Statuses_StatusId] FOREIGN KEY ([StatusId]) REFERENCES [Statuses] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Subscriptions_Subscriptions_RenewedFromSubscriptionId] FOREIGN KEY ([RenewedFromSubscriptionId]) REFERENCES [Subscriptions] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Subscriptions_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [TenantMemberships] (
    [TenantId] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [Role] nvarchar(30) NOT NULL,
    [StatusId] int NOT NULL,
    [JoinedAt] datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    CONSTRAINT [PK_TenantMemberships] PRIMARY KEY ([TenantId], [UserId]),
    CONSTRAINT [FK_TenantMemberships_AppUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AppUsers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_TenantMemberships_Statuses_StatusId] FOREIGN KEY ([StatusId]) REFERENCES [Statuses] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_TenantMemberships_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [AiExecutions] (
    [Id] uniqueidentifier NOT NULL,
    [BusinessId] uniqueidentifier NOT NULL,
    [TaskType] nvarchar(30) NOT NULL,
    [ModelName] nvarchar(100) NOT NULL,
    [PromptVersion] nvarchar(50) NOT NULL,
    [StatusId] int NOT NULL,
    [InputSnapshotJson] nvarchar(max) NULL,
    [OutputSnapshotJson] nvarchar(max) NULL,
    [InputTokens] int NULL,
    [OutputTokens] int NULL,
    [EstimatedCostUsd] decimal(14,6) NULL,
    [LatencyMs] int NULL,
    [ErrorCode] nvarchar(100) NULL,
    [ErrorMessage] nvarchar(2000) NULL,
    [StartedAt] datetimeoffset NULL,
    [CompletedAt] datetimeoffset NULL,
    [CreatedAt] datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    CONSTRAINT [PK_AiExecutions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AiExecutions_Businesses_BusinessId] FOREIGN KEY ([BusinessId]) REFERENCES [Businesses] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_AiExecutions_Statuses_StatusId] FOREIGN KEY ([StatusId]) REFERENCES [Statuses] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [AudienceSegments] (
    [Id] uniqueidentifier NOT NULL,
    [BusinessId] uniqueidentifier NOT NULL,
    [Name] nvarchar(150) NOT NULL,
    [Description] nvarchar(1000) NULL,
    [TargetingRulesJson] nvarchar(max) NULL,
    [CreatedAt] datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    CONSTRAINT [PK_AudienceSegments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AudienceSegments_Businesses_BusinessId] FOREIGN KEY ([BusinessId]) REFERENCES [Businesses] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [AutomationPolicies] (
    [BusinessId] uniqueidentifier NOT NULL,
    [AutomationEnabled] bit NOT NULL,
    [MaxPostsPerDay] smallint NOT NULL,
    [PublishWindowStart] time NULL,
    [PublishWindowEnd] time NULL,
    [MaxDiscountPercent] decimal(5,2) NULL,
    [MonthlyAdBudget] decimal(14,2) NULL,
    [BudgetCurrency] nchar(3) NULL,
    [RequireSeparateReviewer] bit NOT NULL,
    [UpdatedBy] uniqueidentifier NOT NULL,
    [UpdatedAt] datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    CONSTRAINT [PK_AutomationPolicies] PRIMARY KEY ([BusinessId]),
    CONSTRAINT [FK_AutomationPolicies_AppUsers_UpdatedBy] FOREIGN KEY ([UpdatedBy]) REFERENCES [AppUsers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_AutomationPolicies_Businesses_BusinessId] FOREIGN KEY ([BusinessId]) REFERENCES [Businesses] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [BrandProfiles] (
    [BusinessId] uniqueidentifier NOT NULL,
    [Dialect] nvarchar(50) NOT NULL,
    [Tone] nvarchar(50) NOT NULL,
    [EmojiLevel] tinyint NOT NULL,
    [Guidelines] nvarchar(4000) NULL,
    [ColorPaletteJson] nvarchar(max) NULL,
    [UpdatedAt] datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    CONSTRAINT [PK_BrandProfiles] PRIMARY KEY ([BusinessId]),
    CONSTRAINT [FK_BrandProfiles_Businesses_BusinessId] FOREIGN KEY ([BusinessId]) REFERENCES [Businesses] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Campaigns] (
    [Id] uniqueidentifier NOT NULL,
    [BusinessId] uniqueidentifier NOT NULL,
    [CreatedBy] uniqueidentifier NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [Objective] nvarchar(30) NOT NULL,
    [GoalStatement] nvarchar(1000) NOT NULL,
    [TargetMetric] nvarchar(50) NULL,
    [TargetValue] decimal(18,4) NULL,
    [Budget] decimal(14,2) NULL,
    [BudgetCurrency] nchar(3) NULL,
    [StartsAt] datetimeoffset NULL,
    [EndsAt] datetimeoffset NULL,
    [StatusId] int NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [UpdatedAt] datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    CONSTRAINT [PK_Campaigns] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Campaigns_AppUsers_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [AppUsers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Campaigns_Businesses_BusinessId] FOREIGN KEY ([BusinessId]) REFERENCES [Businesses] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Campaigns_Statuses_StatusId] FOREIGN KEY ([StatusId]) REFERENCES [Statuses] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Offers] (
    [Id] uniqueidentifier NOT NULL,
    [BusinessId] uniqueidentifier NOT NULL,
    [Title] nvarchar(200) NOT NULL,
    [Description] nvarchar(2000) NULL,
    [OfferPrice] decimal(14,2) NULL,
    [Currency] nchar(3) NULL,
    [DiscountPercent] decimal(5,2) NULL,
    [StartsAt] datetimeoffset NOT NULL,
    [EndsAt] datetimeoffset NOT NULL,
    [StatusId] int NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    CONSTRAINT [PK_Offers] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Offers_Businesses_BusinessId] FOREIGN KEY ([BusinessId]) REFERENCES [Businesses] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Offers_Statuses_StatusId] FOREIGN KEY ([StatusId]) REFERENCES [Statuses] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Products] (
    [Id] uniqueidentifier NOT NULL,
    [BusinessId] uniqueidentifier NOT NULL,
    [Sku] nvarchar(64) NULL,
    [Name] nvarchar(200) NOT NULL,
    [Description] nvarchar(2000) NULL,
    [Price] decimal(14,2) NOT NULL,
    [Currency] nchar(3) NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [UpdatedAt] datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    CONSTRAINT [PK_Products] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Products_Businesses_BusinessId] FOREIGN KEY ([BusinessId]) REFERENCES [Businesses] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [SocialAccounts] (
    [Id] uniqueidentifier NOT NULL,
    [BusinessId] uniqueidentifier NOT NULL,
    [PlatformCode] nvarchar(30) NOT NULL,
    [ExternalAccountId] nvarchar(200) NOT NULL,
    [Handle] nvarchar(100) NULL,
    [CredentialRef] nvarchar(300) NULL,
    [StatusId] int NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    CONSTRAINT [PK_SocialAccounts] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SocialAccounts_Businesses_BusinessId] FOREIGN KEY ([BusinessId]) REFERENCES [Businesses] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_SocialAccounts_Platforms_PlatformCode] FOREIGN KEY ([PlatformCode]) REFERENCES [Platforms] ([Code]) ON DELETE NO ACTION,
    CONSTRAINT [FK_SocialAccounts_Statuses_StatusId] FOREIGN KEY ([StatusId]) REFERENCES [Statuses] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [TrackedSources] (
    [Id] uniqueidentifier NOT NULL,
    [BusinessId] uniqueidentifier NOT NULL,
    [Url] nvarchar(2048) NOT NULL,
    [NormalizedUrl] nvarchar(800) NOT NULL,
    [Host] nvarchar(255) NOT NULL,
    [PlatformCode] nvarchar(30) NOT NULL,
    [Kind] nvarchar(30) NOT NULL,
    [DisplayName] nvarchar(200) NULL,
    [Handle] nvarchar(100) NULL,
    [Notes] nvarchar(1000) NULL,
    [StatusId] int NOT NULL,
    [LastScannedAt] datetimeoffset NULL,
    [AddedBy] uniqueidentifier NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    CONSTRAINT [PK_TrackedSources] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TrackedSources_AppUsers_AddedBy] FOREIGN KEY ([AddedBy]) REFERENCES [AppUsers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_TrackedSources_Businesses_BusinessId] FOREIGN KEY ([BusinessId]) REFERENCES [Businesses] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_TrackedSources_Platforms_PlatformCode] FOREIGN KEY ([PlatformCode]) REFERENCES [Platforms] ([Code]) ON DELETE NO ACTION,
    CONSTRAINT [FK_TrackedSources_Statuses_StatusId] FOREIGN KEY ([StatusId]) REFERENCES [Statuses] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [StrategyRuns] (
    [Id] uniqueidentifier NOT NULL,
    [BusinessId] uniqueidentifier NOT NULL,
    [WindowStart] datetimeoffset NOT NULL,
    [WindowEnd] datetimeoffset NOT NULL,
    [AlgorithmVersion] nvarchar(50) NOT NULL,
    [StatusId] int NOT NULL,
    [InputSummaryJson] nvarchar(max) NULL,
    [AiExecutionId] uniqueidentifier NULL,
    [StartedAt] datetimeoffset NULL,
    [CompletedAt] datetimeoffset NULL,
    CONSTRAINT [PK_StrategyRuns] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_StrategyRuns_AiExecutions_AiExecutionId] FOREIGN KEY ([AiExecutionId]) REFERENCES [AiExecutions] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_StrategyRuns_Businesses_BusinessId] FOREIGN KEY ([BusinessId]) REFERENCES [Businesses] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_StrategyRuns_Statuses_StatusId] FOREIGN KEY ([StatusId]) REFERENCES [Statuses] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [UsageEvents] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [BusinessId] uniqueidentifier NULL,
    [AiExecutionId] uniqueidentifier NULL,
    [EventType] nvarchar(30) NOT NULL,
    [Units] decimal(14,4) NOT NULL,
    [EstimatedCostUsd] decimal(14,6) NOT NULL,
    [IdempotencyKey] nvarchar(100) NOT NULL,
    [OccurredAt] datetimeoffset NOT NULL,
    CONSTRAINT [PK_UsageEvents] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_UsageEvents_AiExecutions_AiExecutionId] FOREIGN KEY ([AiExecutionId]) REFERENCES [AiExecutions] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_UsageEvents_Businesses_BusinessId] FOREIGN KEY ([BusinessId]) REFERENCES [Businesses] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_UsageEvents_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [AutoApplyRules] (
    [BusinessId] uniqueidentifier NOT NULL,
    [ActionType] nvarchar(30) NOT NULL,
    CONSTRAINT [PK_AutoApplyRules] PRIMARY KEY ([BusinessId], [ActionType]),
    CONSTRAINT [FK_AutoApplyRules_AutomationPolicies_BusinessId] FOREIGN KEY ([BusinessId]) REFERENCES [AutomationPolicies] ([BusinessId]) ON DELETE NO ACTION
);

CREATE TABLE [AutomationPolicyPlatforms] (
    [BusinessId] uniqueidentifier NOT NULL,
    [PlatformCode] nvarchar(30) NOT NULL,
    CONSTRAINT [PK_AutomationPolicyPlatforms] PRIMARY KEY ([BusinessId], [PlatformCode]),
    CONSTRAINT [FK_AutomationPolicyPlatforms_AutomationPolicies_BusinessId] FOREIGN KEY ([BusinessId]) REFERENCES [AutomationPolicies] ([BusinessId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_AutomationPolicyPlatforms_Platforms_PlatformCode] FOREIGN KEY ([PlatformCode]) REFERENCES [Platforms] ([Code]) ON DELETE NO ACTION
);

CREATE TABLE [BrandTerms] (
    [Id] uniqueidentifier NOT NULL,
    [BusinessId] uniqueidentifier NOT NULL,
    [Term] nvarchar(100) NOT NULL,
    [Rule] nvarchar(30) NOT NULL,
    [Note] nvarchar(500) NULL,
    CONSTRAINT [PK_BrandTerms] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_BrandTerms_BrandProfiles_BusinessId] FOREIGN KEY ([BusinessId]) REFERENCES [BrandProfiles] ([BusinessId]) ON DELETE NO ACTION
);

CREATE TABLE [CampaignAudiences] (
    [CampaignId] uniqueidentifier NOT NULL,
    [AudienceSegmentId] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_CampaignAudiences] PRIMARY KEY ([CampaignId], [AudienceSegmentId]),
    CONSTRAINT [FK_CampaignAudiences_AudienceSegments_AudienceSegmentId] FOREIGN KEY ([AudienceSegmentId]) REFERENCES [AudienceSegments] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_CampaignAudiences_Campaigns_CampaignId] FOREIGN KEY ([CampaignId]) REFERENCES [Campaigns] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [CampaignPlans] (
    [Id] uniqueidentifier NOT NULL,
    [CampaignId] uniqueidentifier NOT NULL,
    [VersionNo] int NOT NULL,
    [PlanJson] nvarchar(max) NOT NULL,
    [GeneratedByExecutionId] uniqueidentifier NULL,
    [CreatedAt] datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    CONSTRAINT [PK_CampaignPlans] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CampaignPlans_AiExecutions_GeneratedByExecutionId] FOREIGN KEY ([GeneratedByExecutionId]) REFERENCES [AiExecutions] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_CampaignPlans_Campaigns_CampaignId] FOREIGN KEY ([CampaignId]) REFERENCES [Campaigns] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [CampaignOffers] (
    [CampaignId] uniqueidentifier NOT NULL,
    [OfferId] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_CampaignOffers] PRIMARY KEY ([CampaignId], [OfferId]),
    CONSTRAINT [FK_CampaignOffers_Campaigns_CampaignId] FOREIGN KEY ([CampaignId]) REFERENCES [Campaigns] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_CampaignOffers_Offers_OfferId] FOREIGN KEY ([OfferId]) REFERENCES [Offers] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [CampaignProducts] (
    [CampaignId] uniqueidentifier NOT NULL,
    [ProductId] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_CampaignProducts] PRIMARY KEY ([CampaignId], [ProductId]),
    CONSTRAINT [FK_CampaignProducts_Campaigns_CampaignId] FOREIGN KEY ([CampaignId]) REFERENCES [Campaigns] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_CampaignProducts_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [MediaAssets] (
    [Id] uniqueidentifier NOT NULL,
    [BusinessId] uniqueidentifier NOT NULL,
    [ProductId] uniqueidentifier NULL,
    [Kind] nvarchar(30) NOT NULL,
    [ObjectKey] nvarchar(500) NOT NULL,
    [MimeType] nvarchar(100) NOT NULL,
    [ByteSize] bigint NOT NULL,
    [Sha256] nchar(64) NULL,
    [CreatedAt] datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    CONSTRAINT [PK_MediaAssets] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_MediaAssets_Businesses_BusinessId] FOREIGN KEY ([BusinessId]) REFERENCES [Businesses] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_MediaAssets_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [CompetitorInsights] (
    [Id] uniqueidentifier NOT NULL,
    [BusinessId] uniqueidentifier NOT NULL,
    [TrackedSourceId] uniqueidentifier NULL,
    [InsightType] nvarchar(30) NOT NULL,
    [Summary] nvarchar(1000) NOT NULL,
    [DetailsJson] nvarchar(max) NULL,
    [Confidence] decimal(4,3) NULL,
    [CreatedAt] datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    CONSTRAINT [PK_CompetitorInsights] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CompetitorInsights_Businesses_BusinessId] FOREIGN KEY ([BusinessId]) REFERENCES [Businesses] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_CompetitorInsights_TrackedSources_TrackedSourceId] FOREIGN KEY ([TrackedSourceId]) REFERENCES [TrackedSources] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [SourcePosts] (
    [Id] uniqueidentifier NOT NULL,
    [TrackedSourceId] uniqueidentifier NOT NULL,
    [ExternalPostId] nvarchar(200) NULL,
    [PostUrl] nvarchar(2048) NULL,
    [PostedAt] datetimeoffset NULL,
    [Format] nvarchar(30) NOT NULL,
    [HookType] nvarchar(30) NULL,
    [CaptionText] nvarchar(max) NULL,
    [HashtagsJson] nvarchar(max) NULL,
    [LikesCount] int NULL,
    [CommentsCount] int NULL,
    [SharesCount] int NULL,
    [ViewsCount] bigint NULL,
    [EngagementRate] decimal(9,6) NULL,
    [CapturedAt] datetimeoffset NOT NULL,
    CONSTRAINT [PK_SourcePosts] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SourcePosts_TrackedSources_TrackedSourceId] FOREIGN KEY ([TrackedSourceId]) REFERENCES [TrackedSources] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [StrategyRecommendations] (
    [Id] uniqueidentifier NOT NULL,
    [StrategyRunId] uniqueidentifier NOT NULL,
    [CampaignId] uniqueidentifier NULL,
    [ActionType] nvarchar(30) NOT NULL,
    [ProposedPayloadJson] nvarchar(max) NOT NULL,
    [ProposedPublishAt] datetimeoffset NULL,
    [Rationale] nvarchar(2000) NOT NULL,
    [Confidence] decimal(4,3) NULL,
    [StatusId] int NOT NULL,
    [AppliedAutomatically] bit NOT NULL,
    [DecidedBy] uniqueidentifier NULL,
    [DecidedAt] datetimeoffset NULL,
    [AppliedAt] datetimeoffset NULL,
    [CreatedAt] datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    CONSTRAINT [PK_StrategyRecommendations] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_StrategyRecommendations_AppUsers_DecidedBy] FOREIGN KEY ([DecidedBy]) REFERENCES [AppUsers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_StrategyRecommendations_Campaigns_CampaignId] FOREIGN KEY ([CampaignId]) REFERENCES [Campaigns] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_StrategyRecommendations_Statuses_StatusId] FOREIGN KEY ([StatusId]) REFERENCES [Statuses] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_StrategyRecommendations_StrategyRuns_StrategyRunId] FOREIGN KEY ([StrategyRunId]) REFERENCES [StrategyRuns] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [KnowledgeSources] (
    [Id] uniqueidentifier NOT NULL,
    [BusinessId] uniqueidentifier NOT NULL,
    [AssetId] uniqueidentifier NULL,
    [SourceUrl] nvarchar(2048) NULL,
    [SourceType] nvarchar(30) NOT NULL,
    [ExtractedText] nvarchar(max) NULL,
    [StatusId] int NOT NULL,
    [ProcessedAt] datetimeoffset NULL,
    [CreatedAt] datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    CONSTRAINT [PK_KnowledgeSources] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_KnowledgeSources_Businesses_BusinessId] FOREIGN KEY ([BusinessId]) REFERENCES [Businesses] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_KnowledgeSources_MediaAssets_AssetId] FOREIGN KEY ([AssetId]) REFERENCES [MediaAssets] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_KnowledgeSources_Statuses_StatusId] FOREIGN KEY ([StatusId]) REFERENCES [Statuses] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [ContentItems] (
    [Id] uniqueidentifier NOT NULL,
    [CampaignId] uniqueidentifier NOT NULL,
    [PlatformCode] nvarchar(30) NOT NULL,
    [ContentType] nvarchar(30) NOT NULL,
    [StatusId] int NOT NULL,
    [PlannedPublishAt] datetimeoffset NULL,
    [VariantGroupId] uniqueidentifier NULL,
    [VariantLabel] nvarchar(10) NULL,
    [SourceRecommendationId] uniqueidentifier NULL,
    [InspiredBySourcePostId] uniqueidentifier NULL,
    [CreatedAt] datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    CONSTRAINT [PK_ContentItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ContentItems_Campaigns_CampaignId] FOREIGN KEY ([CampaignId]) REFERENCES [Campaigns] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ContentItems_Platforms_PlatformCode] FOREIGN KEY ([PlatformCode]) REFERENCES [Platforms] ([Code]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ContentItems_SourcePosts_InspiredBySourcePostId] FOREIGN KEY ([InspiredBySourcePostId]) REFERENCES [SourcePosts] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ContentItems_Statuses_StatusId] FOREIGN KEY ([StatusId]) REFERENCES [Statuses] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ContentItems_StrategyRecommendations_SourceRecommendationId] FOREIGN KEY ([SourceRecommendationId]) REFERENCES [StrategyRecommendations] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [ContentRevisions] (
    [Id] uniqueidentifier NOT NULL,
    [ContentItemId] uniqueidentifier NOT NULL,
    [RevisionNo] int NOT NULL,
    [Body] nvarchar(max) NOT NULL,
    [Format] nvarchar(30) NOT NULL,
    [HookType] nvarchar(30) NULL,
    [CallToAction] nvarchar(30) NULL,
    [IncludesOffer] bit NOT NULL,
    [CreativeDetailsJson] nvarchar(max) NULL,
    [GenerationContextJson] nvarchar(max) NULL,
    [GeneratedByExecutionId] uniqueidentifier NULL,
    [SourceRecommendationId] uniqueidentifier NULL,
    [CreatedBy] uniqueidentifier NULL,
    [CreatedAt] datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    CONSTRAINT [PK_ContentRevisions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ContentRevisions_AiExecutions_GeneratedByExecutionId] FOREIGN KEY ([GeneratedByExecutionId]) REFERENCES [AiExecutions] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ContentRevisions_AppUsers_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [AppUsers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ContentRevisions_ContentItems_ContentItemId] FOREIGN KEY ([ContentItemId]) REFERENCES [ContentItems] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ContentRevisions_StrategyRecommendations_SourceRecommendationId] FOREIGN KEY ([SourceRecommendationId]) REFERENCES [StrategyRecommendations] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [ContentReviews] (
    [Id] uniqueidentifier NOT NULL,
    [RevisionId] uniqueidentifier NOT NULL,
    [ReviewerId] uniqueidentifier NOT NULL,
    [StatusId] int NOT NULL,
    [Feedback] nvarchar(2000) NULL,
    [DecidedAt] datetimeoffset NOT NULL,
    CONSTRAINT [PK_ContentReviews] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ContentReviews_AppUsers_ReviewerId] FOREIGN KEY ([ReviewerId]) REFERENCES [AppUsers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ContentReviews_ContentRevisions_RevisionId] FOREIGN KEY ([RevisionId]) REFERENCES [ContentRevisions] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ContentReviews_Statuses_StatusId] FOREIGN KEY ([StatusId]) REFERENCES [Statuses] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [ContentRevisionAssets] (
    [RevisionId] uniqueidentifier NOT NULL,
    [AssetId] uniqueidentifier NOT NULL,
    [Position] tinyint NOT NULL,
    CONSTRAINT [PK_ContentRevisionAssets] PRIMARY KEY ([RevisionId], [AssetId]),
    CONSTRAINT [FK_ContentRevisionAssets_ContentRevisions_RevisionId] FOREIGN KEY ([RevisionId]) REFERENCES [ContentRevisions] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ContentRevisionAssets_MediaAssets_AssetId] FOREIGN KEY ([AssetId]) REFERENCES [MediaAssets] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [PublicationJobs] (
    [Id] uniqueidentifier NOT NULL,
    [ContentItemId] uniqueidentifier NOT NULL,
    [RevisionId] uniqueidentifier NOT NULL,
    [ApprovedReviewId] uniqueidentifier NOT NULL,
    [SocialAccountId] uniqueidentifier NOT NULL,
    [ScheduledAt] datetimeoffset NOT NULL,
    [StatusId] int NOT NULL,
    [IdempotencyKey] nvarchar(100) NOT NULL,
    [AttemptCount] int NOT NULL,
    [LastError] nvarchar(2000) NULL,
    [SourceRecommendationId] uniqueidentifier NULL,
    [CreatedAt] datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    CONSTRAINT [PK_PublicationJobs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PublicationJobs_ContentItems_ContentItemId] FOREIGN KEY ([ContentItemId]) REFERENCES [ContentItems] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_PublicationJobs_ContentReviews_ApprovedReviewId] FOREIGN KEY ([ApprovedReviewId]) REFERENCES [ContentReviews] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_PublicationJobs_ContentRevisions_RevisionId] FOREIGN KEY ([RevisionId]) REFERENCES [ContentRevisions] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_PublicationJobs_SocialAccounts_SocialAccountId] FOREIGN KEY ([SocialAccountId]) REFERENCES [SocialAccounts] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_PublicationJobs_Statuses_StatusId] FOREIGN KEY ([StatusId]) REFERENCES [Statuses] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_PublicationJobs_StrategyRecommendations_SourceRecommendationId] FOREIGN KEY ([SourceRecommendationId]) REFERENCES [StrategyRecommendations] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Publications] (
    [Id] uniqueidentifier NOT NULL,
    [PublicationJobId] uniqueidentifier NOT NULL,
    [ExternalPostId] nvarchar(200) NULL,
    [ExternalUrl] nvarchar(2048) NULL,
    [PublishedAt] datetimeoffset NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    CONSTRAINT [PK_Publications] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Publications_PublicationJobs_PublicationJobId] FOREIGN KEY ([PublicationJobId]) REFERENCES [PublicationJobs] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [MetricObservations] (
    [Id] uniqueidentifier NOT NULL,
    [PublicationId] uniqueidentifier NOT NULL,
    [MetricCode] nvarchar(30) NOT NULL,
    [MetricValue] decimal(20,4) NOT NULL,
    [ObservedAt] datetimeoffset NOT NULL,
    [Source] nvarchar(30) NOT NULL,
    [RawPayloadJson] nvarchar(max) NULL,
    CONSTRAINT [PK_MetricObservations] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_MetricObservations_Publications_PublicationId] FOREIGN KEY ([PublicationId]) REFERENCES [Publications] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [RecommendationEvidence] (
    [Id] uniqueidentifier NOT NULL,
    [RecommendationId] uniqueidentifier NOT NULL,
    [MetricObservationId] uniqueidentifier NULL,
    [SourcePostId] uniqueidentifier NULL,
    [CompetitorInsightId] uniqueidentifier NULL,
    [Interpretation] nvarchar(1000) NULL,
    CONSTRAINT [PK_RecommendationEvidence] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RecommendationEvidence_CompetitorInsights_CompetitorInsightId] FOREIGN KEY ([CompetitorInsightId]) REFERENCES [CompetitorInsights] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_RecommendationEvidence_MetricObservations_MetricObservationId] FOREIGN KEY ([MetricObservationId]) REFERENCES [MetricObservations] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_RecommendationEvidence_SourcePosts_SourcePostId] FOREIGN KEY ([SourcePostId]) REFERENCES [SourcePosts] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_RecommendationEvidence_StrategyRecommendations_RecommendationId] FOREIGN KEY ([RecommendationId]) REFERENCES [StrategyRecommendations] ([Id]) ON DELETE NO ACTION
);

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Code', N'DisplayNameKey', N'IsActive', N'SupportsCompetitorTracking', N'SupportsMessaging', N'SupportsPublishing') AND [object_id] = OBJECT_ID(N'[Platforms]'))
    SET IDENTITY_INSERT [Platforms] ON;
INSERT INTO [Platforms] ([Code], [DisplayNameKey], [IsActive], [SupportsCompetitorTracking], [SupportsMessaging], [SupportsPublishing])
VALUES (N'facebook', N'platform.facebook', CAST(1 AS bit), CAST(1 AS bit), CAST(1 AS bit), CAST(1 AS bit)),
(N'instagram', N'platform.instagram', CAST(1 AS bit), CAST(1 AS bit), CAST(1 AS bit), CAST(1 AS bit)),
(N'linkedin', N'platform.linkedin', CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit)),
(N'snapchat', N'platform.snapchat', CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit)),
(N'tiktok', N'platform.tiktok', CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit)),
(N'website', N'platform.website', CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(0 AS bit)),
(N'whatsapp', N'platform.whatsapp', CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit)),
(N'x', N'platform.x', CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit)),
(N'youtube', N'platform.youtube', CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit));
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Code', N'DisplayNameKey', N'IsActive', N'SupportsCompetitorTracking', N'SupportsMessaging', N'SupportsPublishing') AND [object_id] = OBJECT_ID(N'[Platforms]'))
    SET IDENTITY_INSERT [Platforms] OFF;

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'Code', N'IsActive', N'SortOrder', N'TranslationKey') AND [object_id] = OBJECT_ID(N'[Statuses]'))
    SET IDENTITY_INSERT [Statuses] ON;
INSERT INTO [Statuses] ([Id], [Category], [Code], [IsActive], [SortOrder], [TranslationKey])
VALUES (101, N'Tenant', N'Active', CAST(1 AS bit), 1, N'status.tenant.active'),
(102, N'Tenant', N'Suspended', CAST(1 AS bit), 2, N'status.tenant.suspended'),
(103, N'Tenant', N'Closed', CAST(1 AS bit), 3, N'status.tenant.closed'),
(201, N'Membership', N'Invited', CAST(1 AS bit), 1, N'status.membership.invited'),
(202, N'Membership', N'Active', CAST(1 AS bit), 2, N'status.membership.active'),
(203, N'Membership', N'Disabled', CAST(1 AS bit), 3, N'status.membership.disabled'),
(301, N'Subscription', N'Pending', CAST(1 AS bit), 1, N'status.subscription.pending'),
(302, N'Subscription', N'Active', CAST(1 AS bit), 2, N'status.subscription.active'),
(303, N'Subscription', N'Expired', CAST(1 AS bit), 3, N'status.subscription.expired'),
(304, N'Subscription', N'Canceled', CAST(1 AS bit), 4, N'status.subscription.canceled'),
(401, N'Business', N'Draft', CAST(1 AS bit), 1, N'status.business.draft'),
(402, N'Business', N'Active', CAST(1 AS bit), 2, N'status.business.active'),
(403, N'Business', N'Paused', CAST(1 AS bit), 3, N'status.business.paused'),
(404, N'Business', N'Closed', CAST(1 AS bit), 4, N'status.business.closed'),
(501, N'Offer', N'Draft', CAST(1 AS bit), 1, N'status.offer.draft'),
(502, N'Offer', N'Active', CAST(1 AS bit), 2, N'status.offer.active'),
(503, N'Offer', N'Paused', CAST(1 AS bit), 3, N'status.offer.paused'),
(504, N'Offer', N'Ended', CAST(1 AS bit), 4, N'status.offer.ended'),
(601, N'KnowledgeSource', N'Pending', CAST(1 AS bit), 1, N'status.knowledge_source.pending'),
(602, N'KnowledgeSource', N'Processed', CAST(1 AS bit), 2, N'status.knowledge_source.processed'),
(603, N'KnowledgeSource', N'Failed', CAST(1 AS bit), 3, N'status.knowledge_source.failed'),
(604, N'KnowledgeSource', N'Disabled', CAST(1 AS bit), 4, N'status.knowledge_source.disabled'),
(701, N'SocialAccount', N'Connected', CAST(1 AS bit), 1, N'status.social_account.connected'),
(702, N'SocialAccount', N'Expired', CAST(1 AS bit), 2, N'status.social_account.expired'),
(703, N'SocialAccount', N'Disconnected', CAST(1 AS bit), 3, N'status.social_account.disconnected'),
(801, N'TrackedSource', N'Pending', CAST(1 AS bit), 1, N'status.tracked_source.pending'),
(802, N'TrackedSource', N'Active', CAST(1 AS bit), 2, N'status.tracked_source.active'),
(803, N'TrackedSource', N'Paused', CAST(1 AS bit), 3, N'status.tracked_source.paused'),
(804, N'TrackedSource', N'Failed', CAST(1 AS bit), 4, N'status.tracked_source.failed'),
(901, N'AiExecution', N'Queued', CAST(1 AS bit), 1, N'status.ai_execution.queued'),
(902, N'AiExecution', N'Running', CAST(1 AS bit), 2, N'status.ai_execution.running'),
(903, N'AiExecution', N'Succeeded', CAST(1 AS bit), 3, N'status.ai_execution.succeeded'),
(904, N'AiExecution', N'Failed', CAST(1 AS bit), 4, N'status.ai_execution.failed'),
(1001, N'Campaign', N'Draft', CAST(1 AS bit), 1, N'status.campaign.draft'),
(1002, N'Campaign', N'Planning', CAST(1 AS bit), 2, N'status.campaign.planning'),
(1003, N'Campaign', N'Ready', CAST(1 AS bit), 3, N'status.campaign.ready'),
(1004, N'Campaign', N'Active', CAST(1 AS bit), 4, N'status.campaign.active'),
(1005, N'Campaign', N'Paused', CAST(1 AS bit), 5, N'status.campaign.paused'),
(1006, N'Campaign', N'Completed', CAST(1 AS bit), 6, N'status.campaign.completed'),
(1007, N'Campaign', N'Canceled', CAST(1 AS bit), 7, N'status.campaign.canceled'),
(1101, N'StrategyRun', N'Queued', CAST(1 AS bit), 1, N'status.strategy_run.queued'),
(1102, N'StrategyRun', N'Running', CAST(1 AS bit), 2, N'status.strategy_run.running');
INSERT INTO [Statuses] ([Id], [Category], [Code], [IsActive], [SortOrder], [TranslationKey])
VALUES (1103, N'StrategyRun', N'Completed', CAST(1 AS bit), 3, N'status.strategy_run.completed'),
(1104, N'StrategyRun', N'Failed', CAST(1 AS bit), 4, N'status.strategy_run.failed'),
(1201, N'Recommendation', N'Pending', CAST(1 AS bit), 1, N'status.recommendation.pending'),
(1202, N'Recommendation', N'Accepted', CAST(1 AS bit), 2, N'status.recommendation.accepted'),
(1203, N'Recommendation', N'Rejected', CAST(1 AS bit), 3, N'status.recommendation.rejected'),
(1204, N'Recommendation', N'Applied', CAST(1 AS bit), 4, N'status.recommendation.applied'),
(1301, N'ContentItem', N'Draft', CAST(1 AS bit), 1, N'status.content_item.draft'),
(1302, N'ContentItem', N'InReview', CAST(1 AS bit), 2, N'status.content_item.in_review'),
(1303, N'ContentItem', N'Approved', CAST(1 AS bit), 3, N'status.content_item.approved'),
(1304, N'ContentItem', N'Scheduled', CAST(1 AS bit), 4, N'status.content_item.scheduled'),
(1305, N'ContentItem', N'Published', CAST(1 AS bit), 5, N'status.content_item.published'),
(1306, N'ContentItem', N'Canceled', CAST(1 AS bit), 6, N'status.content_item.canceled'),
(1401, N'ContentReview', N'Approved', CAST(1 AS bit), 1, N'status.content_review.approved'),
(1402, N'ContentReview', N'Rejected', CAST(1 AS bit), 2, N'status.content_review.rejected'),
(1403, N'ContentReview', N'ChangesRequested', CAST(1 AS bit), 3, N'status.content_review.changes_requested'),
(1501, N'PublicationJob', N'Queued', CAST(1 AS bit), 1, N'status.publication_job.queued'),
(1502, N'PublicationJob', N'Running', CAST(1 AS bit), 2, N'status.publication_job.running'),
(1503, N'PublicationJob', N'Succeeded', CAST(1 AS bit), 3, N'status.publication_job.succeeded'),
(1504, N'PublicationJob', N'Retry', CAST(1 AS bit), 4, N'status.publication_job.retry'),
(1505, N'PublicationJob', N'Failed', CAST(1 AS bit), 5, N'status.publication_job.failed'),
(1506, N'PublicationJob', N'Canceled', CAST(1 AS bit), 6, N'status.publication_job.canceled');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'Code', N'IsActive', N'SortOrder', N'TranslationKey') AND [object_id] = OBJECT_ID(N'[Statuses]'))
    SET IDENTITY_INSERT [Statuses] OFF;

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Domain', N'PlatformCode') AND [object_id] = OBJECT_ID(N'[PlatformDomains]'))
    SET IDENTITY_INSERT [PlatformDomains] ON;
INSERT INTO [PlatformDomains] ([Domain], [PlatformCode])
VALUES (N'facebook.com', N'facebook'),
(N'fb.com', N'facebook'),
(N'fb.me', N'facebook'),
(N'instagr.am', N'instagram'),
(N'instagram.com', N'instagram'),
(N'linkedin.com', N'linkedin'),
(N'snapchat.com', N'snapchat'),
(N'tiktok.com', N'tiktok'),
(N'twitter.com', N'x'),
(N'wa.me', N'whatsapp'),
(N'whatsapp.com', N'whatsapp'),
(N'x.com', N'x'),
(N'youtu.be', N'youtube'),
(N'youtube.com', N'youtube');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Domain', N'PlatformCode') AND [object_id] = OBJECT_ID(N'[PlatformDomains]'))
    SET IDENTITY_INSERT [PlatformDomains] OFF;

CREATE INDEX [IX_AiExecutions_BusinessId_TaskType_CreatedAt] ON [AiExecutions] ([BusinessId], [TaskType], [CreatedAt]);

CREATE INDEX [IX_AiExecutions_StatusId] ON [AiExecutions] ([StatusId]);

CREATE UNIQUE INDEX [IX_AppUsers_AuthSubject] ON [AppUsers] ([AuthSubject]);

CREATE UNIQUE INDEX [IX_AppUsers_Email] ON [AppUsers] ([Email]);

CREATE UNIQUE INDEX [IX_AudienceSegments_BusinessId_Name] ON [AudienceSegments] ([BusinessId], [Name]);

CREATE INDEX [IX_AutomationPolicies_UpdatedBy] ON [AutomationPolicies] ([UpdatedBy]);

CREATE INDEX [IX_AutomationPolicyPlatforms_PlatformCode] ON [AutomationPolicyPlatforms] ([PlatformCode]);

CREATE UNIQUE INDEX [IX_BrandTerms_BusinessId_Rule_Term] ON [BrandTerms] ([BusinessId], [Rule], [Term]);

CREATE INDEX [IX_Businesses_StatusId] ON [Businesses] ([StatusId]);

CREATE INDEX [IX_Businesses_TenantId] ON [Businesses] ([TenantId]);

CREATE INDEX [IX_CampaignAudiences_AudienceSegmentId] ON [CampaignAudiences] ([AudienceSegmentId]);

CREATE INDEX [IX_CampaignOffers_OfferId] ON [CampaignOffers] ([OfferId]);

CREATE UNIQUE INDEX [IX_CampaignPlans_CampaignId_VersionNo] ON [CampaignPlans] ([CampaignId], [VersionNo]);

CREATE INDEX [IX_CampaignPlans_GeneratedByExecutionId] ON [CampaignPlans] ([GeneratedByExecutionId]);

CREATE INDEX [IX_CampaignProducts_ProductId] ON [CampaignProducts] ([ProductId]);

CREATE INDEX [IX_Campaigns_BusinessId] ON [Campaigns] ([BusinessId]);

CREATE INDEX [IX_Campaigns_CreatedBy] ON [Campaigns] ([CreatedBy]);

CREATE INDEX [IX_Campaigns_StatusId] ON [Campaigns] ([StatusId]);

CREATE INDEX [IX_CompetitorInsights_BusinessId] ON [CompetitorInsights] ([BusinessId]);

CREATE INDEX [IX_CompetitorInsights_TrackedSourceId] ON [CompetitorInsights] ([TrackedSourceId]);

CREATE INDEX [IX_ContentItems_CampaignId] ON [ContentItems] ([CampaignId]);

CREATE INDEX [IX_ContentItems_InspiredBySourcePostId] ON [ContentItems] ([InspiredBySourcePostId]);

CREATE INDEX [IX_ContentItems_PlatformCode] ON [ContentItems] ([PlatformCode]);

CREATE INDEX [IX_ContentItems_SourceRecommendationId] ON [ContentItems] ([SourceRecommendationId]);

CREATE INDEX [IX_ContentItems_StatusId] ON [ContentItems] ([StatusId]);

CREATE INDEX [IX_ContentReviews_ReviewerId] ON [ContentReviews] ([ReviewerId]);

CREATE INDEX [IX_ContentReviews_RevisionId] ON [ContentReviews] ([RevisionId]);

CREATE INDEX [IX_ContentReviews_StatusId] ON [ContentReviews] ([StatusId]);

CREATE INDEX [IX_ContentRevisionAssets_AssetId] ON [ContentRevisionAssets] ([AssetId]);

CREATE UNIQUE INDEX [IX_ContentRevisions_ContentItemId_RevisionNo] ON [ContentRevisions] ([ContentItemId], [RevisionNo]);

CREATE INDEX [IX_ContentRevisions_CreatedBy] ON [ContentRevisions] ([CreatedBy]);

CREATE INDEX [IX_ContentRevisions_GeneratedByExecutionId] ON [ContentRevisions] ([GeneratedByExecutionId]);

CREATE INDEX [IX_ContentRevisions_SourceRecommendationId] ON [ContentRevisions] ([SourceRecommendationId]);

CREATE INDEX [IX_KnowledgeSources_AssetId] ON [KnowledgeSources] ([AssetId]);

CREATE INDEX [IX_KnowledgeSources_BusinessId] ON [KnowledgeSources] ([BusinessId]);

CREATE INDEX [IX_KnowledgeSources_StatusId] ON [KnowledgeSources] ([StatusId]);

CREATE INDEX [IX_MediaAssets_BusinessId] ON [MediaAssets] ([BusinessId]);

CREATE UNIQUE INDEX [IX_MediaAssets_ObjectKey] ON [MediaAssets] ([ObjectKey]);

CREATE INDEX [IX_MediaAssets_ProductId] ON [MediaAssets] ([ProductId]);

CREATE UNIQUE INDEX [IX_MetricObservations_PublicationId_MetricCode_ObservedAt] ON [MetricObservations] ([PublicationId], [MetricCode], [ObservedAt]);

CREATE INDEX [IX_Offers_BusinessId] ON [Offers] ([BusinessId]);

CREATE INDEX [IX_Offers_StatusId] ON [Offers] ([StatusId]);

CREATE INDEX [IX_PlatformDomains_PlatformCode] ON [PlatformDomains] ([PlatformCode]);

CREATE UNIQUE INDEX [IX_Platforms_DisplayNameKey] ON [Platforms] ([DisplayNameKey]);

CREATE UNIQUE INDEX [IX_Products_BusinessId_Sku] ON [Products] ([BusinessId], [Sku]) WHERE [Sku] IS NOT NULL;

CREATE INDEX [IX_PublicationJobs_ApprovedReviewId] ON [PublicationJobs] ([ApprovedReviewId]);

CREATE INDEX [IX_PublicationJobs_ContentItemId] ON [PublicationJobs] ([ContentItemId]);

CREATE INDEX [IX_PublicationJobs_RevisionId] ON [PublicationJobs] ([RevisionId]);

CREATE UNIQUE INDEX [IX_PublicationJobs_SocialAccountId_IdempotencyKey] ON [PublicationJobs] ([SocialAccountId], [IdempotencyKey]);

CREATE INDEX [IX_PublicationJobs_SourceRecommendationId] ON [PublicationJobs] ([SourceRecommendationId]);

CREATE INDEX [IX_PublicationJobs_StatusId_ScheduledAt] ON [PublicationJobs] ([StatusId], [ScheduledAt]);

CREATE UNIQUE INDEX [IX_Publications_PublicationJobId] ON [Publications] ([PublicationJobId]);

CREATE INDEX [IX_RecommendationEvidence_CompetitorInsightId] ON [RecommendationEvidence] ([CompetitorInsightId]);

CREATE INDEX [IX_RecommendationEvidence_MetricObservationId] ON [RecommendationEvidence] ([MetricObservationId]);

CREATE INDEX [IX_RecommendationEvidence_RecommendationId] ON [RecommendationEvidence] ([RecommendationId]);

CREATE INDEX [IX_RecommendationEvidence_SourcePostId] ON [RecommendationEvidence] ([SourcePostId]);

CREATE INDEX [IX_SocialAccounts_BusinessId] ON [SocialAccounts] ([BusinessId]);

CREATE UNIQUE INDEX [IX_SocialAccounts_PlatformCode_ExternalAccountId] ON [SocialAccounts] ([PlatformCode], [ExternalAccountId]);

CREATE INDEX [IX_SocialAccounts_StatusId] ON [SocialAccounts] ([StatusId]);

CREATE UNIQUE INDEX [IX_SourcePosts_TrackedSourceId_ExternalPostId] ON [SourcePosts] ([TrackedSourceId], [ExternalPostId]) WHERE [ExternalPostId] IS NOT NULL;

CREATE UNIQUE INDEX [IX_Statuses_Category_Code] ON [Statuses] ([Category], [Code]);

CREATE UNIQUE INDEX [IX_Statuses_TranslationKey] ON [Statuses] ([TranslationKey]);

CREATE INDEX [IX_StrategyRecommendations_CampaignId] ON [StrategyRecommendations] ([CampaignId]);

CREATE INDEX [IX_StrategyRecommendations_DecidedBy] ON [StrategyRecommendations] ([DecidedBy]);

CREATE INDEX [IX_StrategyRecommendations_StatusId] ON [StrategyRecommendations] ([StatusId]);

CREATE INDEX [IX_StrategyRecommendations_StrategyRunId] ON [StrategyRecommendations] ([StrategyRunId]);

CREATE INDEX [IX_StrategyRuns_AiExecutionId] ON [StrategyRuns] ([AiExecutionId]);

CREATE INDEX [IX_StrategyRuns_BusinessId] ON [StrategyRuns] ([BusinessId]);

CREATE INDEX [IX_StrategyRuns_StatusId] ON [StrategyRuns] ([StatusId]);

CREATE UNIQUE INDEX [IX_Subscriptions_PaymentReference] ON [Subscriptions] ([PaymentReference]) WHERE [PaymentReference] IS NOT NULL;

CREATE UNIQUE INDEX [IX_Subscriptions_RenewedFromSubscriptionId] ON [Subscriptions] ([RenewedFromSubscriptionId]) WHERE [RenewedFromSubscriptionId] IS NOT NULL;

CREATE INDEX [IX_Subscriptions_StatusId] ON [Subscriptions] ([StatusId]);

CREATE INDEX [IX_Subscriptions_TenantId_StatusId_EndsAt] ON [Subscriptions] ([TenantId], [StatusId], [EndsAt]);

CREATE INDEX [IX_TenantMemberships_StatusId] ON [TenantMemberships] ([StatusId]);

CREATE INDEX [IX_TenantMemberships_UserId] ON [TenantMemberships] ([UserId]);

CREATE INDEX [IX_Tenants_StatusId] ON [Tenants] ([StatusId]);

CREATE INDEX [IX_TrackedSources_AddedBy] ON [TrackedSources] ([AddedBy]);

CREATE UNIQUE INDEX [IX_TrackedSources_BusinessId_NormalizedUrl] ON [TrackedSources] ([BusinessId], [NormalizedUrl]);

CREATE INDEX [IX_TrackedSources_PlatformCode] ON [TrackedSources] ([PlatformCode]);

CREATE INDEX [IX_TrackedSources_StatusId] ON [TrackedSources] ([StatusId]);

CREATE INDEX [IX_UsageEvents_AiExecutionId] ON [UsageEvents] ([AiExecutionId]);

CREATE INDEX [IX_UsageEvents_BusinessId] ON [UsageEvents] ([BusinessId]);

CREATE UNIQUE INDEX [IX_UsageEvents_TenantId_IdempotencyKey] ON [UsageEvents] ([TenantId], [IdempotencyKey]);

CREATE INDEX [IX_UsageEvents_TenantId_OccurredAt] ON [UsageEvents] ([TenantId], [OccurredAt]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261022114420_InitialCreate', N'10.0.12');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [AppUsers] ADD [PasswordHash] nvarchar(500) NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261027090456_AddUserPasswordHash', N'10.0.12');

COMMIT;
GO

