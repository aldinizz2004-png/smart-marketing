using Microsoft.EntityFrameworkCore;
using SmartMarketingPlatform.Entities;

namespace SmartMarketingPlatform.Data;

public class MarketingDbContext : DbContext
{
    public MarketingDbContext(DbContextOptions<MarketingDbContext> options)
        : base(options)
    {
    }

    // Lookups
    public DbSet<Status> Statuses => Set<Status>();
    public DbSet<Platform> Platforms => Set<Platform>();
    public DbSet<PlatformDomain> PlatformDomains => Set<PlatformDomain>();

    // Account and subscriptions
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<TenantMembership> TenantMemberships => Set<TenantMembership>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();

    // Business knowledge
    public DbSet<Business> Businesses => Set<Business>();
    public DbSet<BrandProfile> BrandProfiles => Set<BrandProfile>();
    public DbSet<BrandTerm> BrandTerms => Set<BrandTerm>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Offer> Offers => Set<Offer>();
    public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();
    public DbSet<KnowledgeSource> KnowledgeSources => Set<KnowledgeSource>();
    public DbSet<AudienceSegment> AudienceSegments => Set<AudienceSegment>();

    // Social accounts and owner limits
    public DbSet<SocialAccount> SocialAccounts => Set<SocialAccount>();
    public DbSet<AutomationPolicy> AutomationPolicies => Set<AutomationPolicy>();
    public DbSet<AutomationPolicyPlatform> AutomationPolicyPlatforms => Set<AutomationPolicyPlatform>();
    public DbSet<AutoApplyRule> AutoApplyRules => Set<AutoApplyRule>();

    // Competitors
    public DbSet<TrackedSource> TrackedSources => Set<TrackedSource>();
    public DbSet<SourcePost> SourcePosts => Set<SourcePost>();
    public DbSet<CompetitorInsight> CompetitorInsights => Set<CompetitorInsight>();

    // AI and campaigns
    public DbSet<AiExecution> AiExecutions => Set<AiExecution>();
    public DbSet<Campaign> Campaigns => Set<Campaign>();
    public DbSet<CampaignProduct> CampaignProducts => Set<CampaignProduct>();
    public DbSet<CampaignAudience> CampaignAudiences => Set<CampaignAudience>();
    public DbSet<CampaignOffer> CampaignOffers => Set<CampaignOffer>();
    public DbSet<CampaignPlan> CampaignPlans => Set<CampaignPlan>();
    public DbSet<StrategyRun> StrategyRuns => Set<StrategyRun>();
    public DbSet<StrategyRecommendation> StrategyRecommendations => Set<StrategyRecommendation>();

    // Content and publishing
    public DbSet<ContentItem> ContentItems => Set<ContentItem>();
    public DbSet<ContentRevision> ContentRevisions => Set<ContentRevision>();
    public DbSet<ContentRevisionAsset> ContentRevisionAssets => Set<ContentRevisionAsset>();
    public DbSet<ContentReview> ContentReviews => Set<ContentReview>();
    public DbSet<PublicationJob> PublicationJobs => Set<PublicationJob>();
    public DbSet<Publication> Publications => Set<Publication>();

    // Results and cost
    public DbSet<MetricObservation> MetricObservations => Set<MetricObservation>();
    public DbSet<RecommendationEvidence> RecommendationEvidence => Set<RecommendationEvidence>();
    public DbSet<UsageEvent> UsageEvents => Set<UsageEvent>();

    // UpdatedAt is set here for every modified row, so no service can forget it.
    // (On insert it is filled by the SYSDATETIMEOFFSET() default.)
    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Modified
                && entry.Metadata.FindProperty("UpdatedAt")?.ClrType == typeof(DateTimeOffset))
            {
                entry.Property("UpdatedAt").CurrentValue = now;
            }
        }

        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Picks up every IEntityTypeConfiguration<T> in Data/Configurations.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MarketingDbContext).Assembly);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            // No cascade delete: history is never removed by accident, and SQL Server
            // refuses tables that can be reached by more than one cascade path.
            foreach (var foreignKey in entityType.GetForeignKeys())
            {
                foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
            }

            // CreatedAt / UpdatedAt / JoinedAt are filled by SQL Server when the app leaves them empty.
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTimeOffset)
                    && property.Name is "CreatedAt" or "UpdatedAt" or "JoinedAt")
                {
                    property.SetDefaultValueSql("SYSDATETIMEOFFSET()");
                }
            }
        }
    }
}
