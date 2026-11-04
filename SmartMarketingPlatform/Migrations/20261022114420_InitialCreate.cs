using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SmartMarketingPlatform.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuthSubject = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PreferredLanguage = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Platforms",
                columns: table => new
                {
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DisplayNameKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SupportsPublishing = table.Column<bool>(type: "bit", nullable: false),
                    SupportsMessaging = table.Column<bool>(type: "bit", nullable: false),
                    SupportsCompetitorTracking = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Platforms", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "Statuses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TranslationKey = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Statuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlatformDomains",
                columns: table => new
                {
                    Domain = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PlatformCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatformDomains", x => x.Domain);
                    table.ForeignKey(
                        name: "FK_PlatformDomains_Platforms_PlatformCode",
                        column: x => x.PlatformCode,
                        principalTable: "Platforms",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StatusId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tenants_Statuses_StatusId",
                        column: x => x.StatusId,
                        principalTable: "Statuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Businesses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CountryCode = table.Column<string>(type: "nchar(2)", fixedLength: true, maxLength: 2, nullable: true),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    TimeZoneId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DefaultCurrency = table.Column<string>(type: "nchar(3)", fixedLength: true, maxLength: 3, nullable: false),
                    ContactPhone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    StatusId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Businesses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Businesses_Statuses_StatusId",
                        column: x => x.StatusId,
                        principalTable: "Statuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Businesses_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Subscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Duration = table.Column<byte>(type: "tinyint", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nchar(3)", fixedLength: true, maxLength: 3, nullable: false),
                    StartsAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EndsAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    StatusId = table.Column<int>(type: "int", nullable: false),
                    RenewedFromSubscriptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CanceledAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    PaymentReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Subscriptions_Statuses_StatusId",
                        column: x => x.StatusId,
                        principalTable: "Statuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Subscriptions_Subscriptions_RenewedFromSubscriptionId",
                        column: x => x.RenewedFromSubscriptionId,
                        principalTable: "Subscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Subscriptions_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TenantMemberships",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    StatusId = table.Column<int>(type: "int", nullable: false),
                    JoinedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantMemberships", x => new { x.TenantId, x.UserId });
                    table.ForeignKey(
                        name: "FK_TenantMemberships_AppUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TenantMemberships_Statuses_StatusId",
                        column: x => x.StatusId,
                        principalTable: "Statuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TenantMemberships_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AiExecutions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ModelName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PromptVersion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StatusId = table.Column<int>(type: "int", nullable: false),
                    InputSnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OutputSnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InputTokens = table.Column<int>(type: "int", nullable: true),
                    OutputTokens = table.Column<int>(type: "int", nullable: true),
                    EstimatedCostUsd = table.Column<decimal>(type: "decimal(14,6)", precision: 14, scale: 6, nullable: true),
                    LatencyMs = table.Column<int>(type: "int", nullable: true),
                    ErrorCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiExecutions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiExecutions_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AiExecutions_Statuses_StatusId",
                        column: x => x.StatusId,
                        principalTable: "Statuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AudienceSegments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TargetingRulesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AudienceSegments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AudienceSegments_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AutomationPolicies",
                columns: table => new
                {
                    BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AutomationEnabled = table.Column<bool>(type: "bit", nullable: false),
                    MaxPostsPerDay = table.Column<short>(type: "smallint", nullable: false),
                    PublishWindowStart = table.Column<TimeOnly>(type: "time", nullable: true),
                    PublishWindowEnd = table.Column<TimeOnly>(type: "time", nullable: true),
                    MaxDiscountPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    MonthlyAdBudget = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: true),
                    BudgetCurrency = table.Column<string>(type: "nchar(3)", fixedLength: true, maxLength: 3, nullable: true),
                    RequireSeparateReviewer = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutomationPolicies", x => x.BusinessId);
                    table.ForeignKey(
                        name: "FK_AutomationPolicies_AppUsers_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AutomationPolicies_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BrandProfiles",
                columns: table => new
                {
                    BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Dialect = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Tone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EmojiLevel = table.Column<byte>(type: "tinyint", nullable: false),
                    Guidelines = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ColorPaletteJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BrandProfiles", x => x.BusinessId);
                    table.ForeignKey(
                        name: "FK_BrandProfiles_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Campaigns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Objective = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    GoalStatement = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    TargetMetric = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TargetValue = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    Budget = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: true),
                    BudgetCurrency = table.Column<string>(type: "nchar(3)", fixedLength: true, maxLength: 3, nullable: true),
                    StartsAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    EndsAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    StatusId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Campaigns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Campaigns_AppUsers_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Campaigns_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Campaigns_Statuses_StatusId",
                        column: x => x.StatusId,
                        principalTable: "Statuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Offers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    OfferPrice = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: true),
                    Currency = table.Column<string>(type: "nchar(3)", fixedLength: true, maxLength: 3, nullable: true),
                    DiscountPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    StartsAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EndsAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    StatusId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Offers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Offers_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Offers_Statuses_StatusId",
                        column: x => x.StatusId,
                        principalTable: "Statuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Sku = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Price = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nchar(3)", fixedLength: true, maxLength: 3, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Products_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SocialAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ExternalAccountId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Handle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CredentialRef = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    StatusId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SocialAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SocialAccounts_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SocialAccounts_Platforms_PlatformCode",
                        column: x => x.PlatformCode,
                        principalTable: "Platforms",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SocialAccounts_Statuses_StatusId",
                        column: x => x.StatusId,
                        principalTable: "Statuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TrackedSources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    NormalizedUrl = table.Column<string>(type: "nvarchar(800)", maxLength: 800, nullable: false),
                    Host = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PlatformCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Handle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    StatusId = table.Column<int>(type: "int", nullable: false),
                    LastScannedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    AddedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrackedSources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrackedSources_AppUsers_AddedBy",
                        column: x => x.AddedBy,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TrackedSources_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TrackedSources_Platforms_PlatformCode",
                        column: x => x.PlatformCode,
                        principalTable: "Platforms",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TrackedSources_Statuses_StatusId",
                        column: x => x.StatusId,
                        principalTable: "Statuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StrategyRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WindowStart = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    WindowEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    AlgorithmVersion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StatusId = table.Column<int>(type: "int", nullable: false),
                    InputSummaryJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AiExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StrategyRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StrategyRuns_AiExecutions_AiExecutionId",
                        column: x => x.AiExecutionId,
                        principalTable: "AiExecutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StrategyRuns_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StrategyRuns_Statuses_StatusId",
                        column: x => x.StatusId,
                        principalTable: "Statuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UsageEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AiExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EventType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Units = table.Column<decimal>(type: "decimal(14,4)", precision: 14, scale: 4, nullable: false),
                    EstimatedCostUsd = table.Column<decimal>(type: "decimal(14,6)", precision: 14, scale: 6, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsageEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UsageEvents_AiExecutions_AiExecutionId",
                        column: x => x.AiExecutionId,
                        principalTable: "AiExecutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UsageEvents_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UsageEvents_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AutoApplyRules",
                columns: table => new
                {
                    BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActionType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutoApplyRules", x => new { x.BusinessId, x.ActionType });
                    table.ForeignKey(
                        name: "FK_AutoApplyRules_AutomationPolicies_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "AutomationPolicies",
                        principalColumn: "BusinessId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AutomationPolicyPlatforms",
                columns: table => new
                {
                    BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutomationPolicyPlatforms", x => new { x.BusinessId, x.PlatformCode });
                    table.ForeignKey(
                        name: "FK_AutomationPolicyPlatforms_AutomationPolicies_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "AutomationPolicies",
                        principalColumn: "BusinessId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AutomationPolicyPlatforms_Platforms_PlatformCode",
                        column: x => x.PlatformCode,
                        principalTable: "Platforms",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BrandTerms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Term = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Rule = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BrandTerms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BrandTerms_BrandProfiles_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "BrandProfiles",
                        principalColumn: "BusinessId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CampaignAudiences",
                columns: table => new
                {
                    CampaignId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AudienceSegmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampaignAudiences", x => new { x.CampaignId, x.AudienceSegmentId });
                    table.ForeignKey(
                        name: "FK_CampaignAudiences_AudienceSegments_AudienceSegmentId",
                        column: x => x.AudienceSegmentId,
                        principalTable: "AudienceSegments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CampaignAudiences_Campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "Campaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CampaignPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionNo = table.Column<int>(type: "int", nullable: false),
                    PlanJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GeneratedByExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampaignPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CampaignPlans_AiExecutions_GeneratedByExecutionId",
                        column: x => x.GeneratedByExecutionId,
                        principalTable: "AiExecutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CampaignPlans_Campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "Campaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CampaignOffers",
                columns: table => new
                {
                    CampaignId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OfferId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampaignOffers", x => new { x.CampaignId, x.OfferId });
                    table.ForeignKey(
                        name: "FK_CampaignOffers_Campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "Campaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CampaignOffers_Offers_OfferId",
                        column: x => x.OfferId,
                        principalTable: "Offers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CampaignProducts",
                columns: table => new
                {
                    CampaignId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampaignProducts", x => new { x.CampaignId, x.ProductId });
                    table.ForeignKey(
                        name: "FK_CampaignProducts_Campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "Campaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CampaignProducts_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MediaAssets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Kind = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ObjectKey = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    MimeType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ByteSize = table.Column<long>(type: "bigint", nullable: false),
                    Sha256 = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaAssets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MediaAssets_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MediaAssets_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CompetitorInsights",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TrackedSourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    InsightType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    DetailsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Confidence = table.Column<decimal>(type: "decimal(4,3)", precision: 4, scale: 3, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompetitorInsights", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompetitorInsights_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CompetitorInsights_TrackedSources_TrackedSourceId",
                        column: x => x.TrackedSourceId,
                        principalTable: "TrackedSources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SourcePosts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TrackedSourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExternalPostId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PostUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    PostedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Format = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    HookType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    CaptionText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HashtagsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LikesCount = table.Column<int>(type: "int", nullable: true),
                    CommentsCount = table.Column<int>(type: "int", nullable: true),
                    SharesCount = table.Column<int>(type: "int", nullable: true),
                    ViewsCount = table.Column<long>(type: "bigint", nullable: true),
                    EngagementRate = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    CapturedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SourcePosts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SourcePosts_TrackedSources_TrackedSourceId",
                        column: x => x.TrackedSourceId,
                        principalTable: "TrackedSources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StrategyRecommendations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StrategyRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ActionType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ProposedPayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProposedPublishAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Rationale = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Confidence = table.Column<decimal>(type: "decimal(4,3)", precision: 4, scale: 3, nullable: true),
                    StatusId = table.Column<int>(type: "int", nullable: false),
                    AppliedAutomatically = table.Column<bool>(type: "bit", nullable: false),
                    DecidedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DecidedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    AppliedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StrategyRecommendations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StrategyRecommendations_AppUsers_DecidedBy",
                        column: x => x.DecidedBy,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StrategyRecommendations_Campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "Campaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StrategyRecommendations_Statuses_StatusId",
                        column: x => x.StatusId,
                        principalTable: "Statuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StrategyRecommendations_StrategyRuns_StrategyRunId",
                        column: x => x.StrategyRunId,
                        principalTable: "StrategyRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeSources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SourceUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    SourceType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ExtractedText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StatusId = table.Column<int>(type: "int", nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeSources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KnowledgeSources_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KnowledgeSources_MediaAssets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "MediaAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KnowledgeSources_Statuses_StatusId",
                        column: x => x.StatusId,
                        principalTable: "Statuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ContentItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    StatusId = table.Column<int>(type: "int", nullable: false),
                    PlannedPublishAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    VariantGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VariantLabel = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    SourceRecommendationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    InspiredBySourcePostId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContentItems_Campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "Campaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContentItems_Platforms_PlatformCode",
                        column: x => x.PlatformCode,
                        principalTable: "Platforms",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContentItems_SourcePosts_InspiredBySourcePostId",
                        column: x => x.InspiredBySourcePostId,
                        principalTable: "SourcePosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContentItems_Statuses_StatusId",
                        column: x => x.StatusId,
                        principalTable: "Statuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContentItems_StrategyRecommendations_SourceRecommendationId",
                        column: x => x.SourceRecommendationId,
                        principalTable: "StrategyRecommendations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ContentRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContentItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RevisionNo = table.Column<int>(type: "int", nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Format = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    HookType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    CallToAction = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    IncludesOffer = table.Column<bool>(type: "bit", nullable: false),
                    CreativeDetailsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GenerationContextJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GeneratedByExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SourceRecommendationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContentRevisions_AiExecutions_GeneratedByExecutionId",
                        column: x => x.GeneratedByExecutionId,
                        principalTable: "AiExecutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContentRevisions_AppUsers_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContentRevisions_ContentItems_ContentItemId",
                        column: x => x.ContentItemId,
                        principalTable: "ContentItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContentRevisions_StrategyRecommendations_SourceRecommendationId",
                        column: x => x.SourceRecommendationId,
                        principalTable: "StrategyRecommendations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ContentReviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RevisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReviewerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StatusId = table.Column<int>(type: "int", nullable: false),
                    Feedback = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DecidedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContentReviews_AppUsers_ReviewerId",
                        column: x => x.ReviewerId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContentReviews_ContentRevisions_RevisionId",
                        column: x => x.RevisionId,
                        principalTable: "ContentRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContentReviews_Statuses_StatusId",
                        column: x => x.StatusId,
                        principalTable: "Statuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ContentRevisionAssets",
                columns: table => new
                {
                    RevisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<byte>(type: "tinyint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentRevisionAssets", x => new { x.RevisionId, x.AssetId });
                    table.ForeignKey(
                        name: "FK_ContentRevisionAssets_ContentRevisions_RevisionId",
                        column: x => x.RevisionId,
                        principalTable: "ContentRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContentRevisionAssets_MediaAssets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "MediaAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PublicationJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContentItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RevisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApprovedReviewId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SocialAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScheduledAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    StatusId = table.Column<int>(type: "int", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    LastError = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    SourceRecommendationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublicationJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PublicationJobs_ContentItems_ContentItemId",
                        column: x => x.ContentItemId,
                        principalTable: "ContentItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PublicationJobs_ContentReviews_ApprovedReviewId",
                        column: x => x.ApprovedReviewId,
                        principalTable: "ContentReviews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PublicationJobs_ContentRevisions_RevisionId",
                        column: x => x.RevisionId,
                        principalTable: "ContentRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PublicationJobs_SocialAccounts_SocialAccountId",
                        column: x => x.SocialAccountId,
                        principalTable: "SocialAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PublicationJobs_Statuses_StatusId",
                        column: x => x.StatusId,
                        principalTable: "Statuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PublicationJobs_StrategyRecommendations_SourceRecommendationId",
                        column: x => x.SourceRecommendationId,
                        principalTable: "StrategyRecommendations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Publications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PublicationJobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExternalPostId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ExternalUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    PublishedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Publications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Publications_PublicationJobs_PublicationJobId",
                        column: x => x.PublicationJobId,
                        principalTable: "PublicationJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MetricObservations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PublicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MetricCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    MetricValue = table.Column<decimal>(type: "decimal(20,4)", precision: 20, scale: 4, nullable: false),
                    ObservedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    RawPayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetricObservations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MetricObservations_Publications_PublicationId",
                        column: x => x.PublicationId,
                        principalTable: "Publications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RecommendationEvidence",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecommendationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MetricObservationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SourcePostId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CompetitorInsightId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Interpretation = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecommendationEvidence", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecommendationEvidence_CompetitorInsights_CompetitorInsightId",
                        column: x => x.CompetitorInsightId,
                        principalTable: "CompetitorInsights",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RecommendationEvidence_MetricObservations_MetricObservationId",
                        column: x => x.MetricObservationId,
                        principalTable: "MetricObservations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RecommendationEvidence_SourcePosts_SourcePostId",
                        column: x => x.SourcePostId,
                        principalTable: "SourcePosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RecommendationEvidence_StrategyRecommendations_RecommendationId",
                        column: x => x.RecommendationId,
                        principalTable: "StrategyRecommendations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Platforms",
                columns: new[] { "Code", "DisplayNameKey", "IsActive", "SupportsCompetitorTracking", "SupportsMessaging", "SupportsPublishing" },
                values: new object[,]
                {
                    { "facebook", "platform.facebook", true, true, true, true },
                    { "instagram", "platform.instagram", true, true, true, true },
                    { "linkedin", "platform.linkedin", true, true, false, true },
                    { "snapchat", "platform.snapchat", true, true, false, true },
                    { "tiktok", "platform.tiktok", true, true, false, true },
                    { "website", "platform.website", true, true, false, false },
                    { "whatsapp", "platform.whatsapp", true, false, true, true },
                    { "x", "platform.x", true, true, false, true },
                    { "youtube", "platform.youtube", true, true, false, true }
                });

            migrationBuilder.InsertData(
                table: "Statuses",
                columns: new[] { "Id", "Category", "Code", "IsActive", "SortOrder", "TranslationKey" },
                values: new object[,]
                {
                    { 101, "Tenant", "Active", true, 1, "status.tenant.active" },
                    { 102, "Tenant", "Suspended", true, 2, "status.tenant.suspended" },
                    { 103, "Tenant", "Closed", true, 3, "status.tenant.closed" },
                    { 201, "Membership", "Invited", true, 1, "status.membership.invited" },
                    { 202, "Membership", "Active", true, 2, "status.membership.active" },
                    { 203, "Membership", "Disabled", true, 3, "status.membership.disabled" },
                    { 301, "Subscription", "Pending", true, 1, "status.subscription.pending" },
                    { 302, "Subscription", "Active", true, 2, "status.subscription.active" },
                    { 303, "Subscription", "Expired", true, 3, "status.subscription.expired" },
                    { 304, "Subscription", "Canceled", true, 4, "status.subscription.canceled" },
                    { 401, "Business", "Draft", true, 1, "status.business.draft" },
                    { 402, "Business", "Active", true, 2, "status.business.active" },
                    { 403, "Business", "Paused", true, 3, "status.business.paused" },
                    { 404, "Business", "Closed", true, 4, "status.business.closed" },
                    { 501, "Offer", "Draft", true, 1, "status.offer.draft" },
                    { 502, "Offer", "Active", true, 2, "status.offer.active" },
                    { 503, "Offer", "Paused", true, 3, "status.offer.paused" },
                    { 504, "Offer", "Ended", true, 4, "status.offer.ended" },
                    { 601, "KnowledgeSource", "Pending", true, 1, "status.knowledge_source.pending" },
                    { 602, "KnowledgeSource", "Processed", true, 2, "status.knowledge_source.processed" },
                    { 603, "KnowledgeSource", "Failed", true, 3, "status.knowledge_source.failed" },
                    { 604, "KnowledgeSource", "Disabled", true, 4, "status.knowledge_source.disabled" },
                    { 701, "SocialAccount", "Connected", true, 1, "status.social_account.connected" },
                    { 702, "SocialAccount", "Expired", true, 2, "status.social_account.expired" },
                    { 703, "SocialAccount", "Disconnected", true, 3, "status.social_account.disconnected" },
                    { 801, "TrackedSource", "Pending", true, 1, "status.tracked_source.pending" },
                    { 802, "TrackedSource", "Active", true, 2, "status.tracked_source.active" },
                    { 803, "TrackedSource", "Paused", true, 3, "status.tracked_source.paused" },
                    { 804, "TrackedSource", "Failed", true, 4, "status.tracked_source.failed" },
                    { 901, "AiExecution", "Queued", true, 1, "status.ai_execution.queued" },
                    { 902, "AiExecution", "Running", true, 2, "status.ai_execution.running" },
                    { 903, "AiExecution", "Succeeded", true, 3, "status.ai_execution.succeeded" },
                    { 904, "AiExecution", "Failed", true, 4, "status.ai_execution.failed" },
                    { 1001, "Campaign", "Draft", true, 1, "status.campaign.draft" },
                    { 1002, "Campaign", "Planning", true, 2, "status.campaign.planning" },
                    { 1003, "Campaign", "Ready", true, 3, "status.campaign.ready" },
                    { 1004, "Campaign", "Active", true, 4, "status.campaign.active" },
                    { 1005, "Campaign", "Paused", true, 5, "status.campaign.paused" },
                    { 1006, "Campaign", "Completed", true, 6, "status.campaign.completed" },
                    { 1007, "Campaign", "Canceled", true, 7, "status.campaign.canceled" },
                    { 1101, "StrategyRun", "Queued", true, 1, "status.strategy_run.queued" },
                    { 1102, "StrategyRun", "Running", true, 2, "status.strategy_run.running" },
                    { 1103, "StrategyRun", "Completed", true, 3, "status.strategy_run.completed" },
                    { 1104, "StrategyRun", "Failed", true, 4, "status.strategy_run.failed" },
                    { 1201, "Recommendation", "Pending", true, 1, "status.recommendation.pending" },
                    { 1202, "Recommendation", "Accepted", true, 2, "status.recommendation.accepted" },
                    { 1203, "Recommendation", "Rejected", true, 3, "status.recommendation.rejected" },
                    { 1204, "Recommendation", "Applied", true, 4, "status.recommendation.applied" },
                    { 1301, "ContentItem", "Draft", true, 1, "status.content_item.draft" },
                    { 1302, "ContentItem", "InReview", true, 2, "status.content_item.in_review" },
                    { 1303, "ContentItem", "Approved", true, 3, "status.content_item.approved" },
                    { 1304, "ContentItem", "Scheduled", true, 4, "status.content_item.scheduled" },
                    { 1305, "ContentItem", "Published", true, 5, "status.content_item.published" },
                    { 1306, "ContentItem", "Canceled", true, 6, "status.content_item.canceled" },
                    { 1401, "ContentReview", "Approved", true, 1, "status.content_review.approved" },
                    { 1402, "ContentReview", "Rejected", true, 2, "status.content_review.rejected" },
                    { 1403, "ContentReview", "ChangesRequested", true, 3, "status.content_review.changes_requested" },
                    { 1501, "PublicationJob", "Queued", true, 1, "status.publication_job.queued" },
                    { 1502, "PublicationJob", "Running", true, 2, "status.publication_job.running" },
                    { 1503, "PublicationJob", "Succeeded", true, 3, "status.publication_job.succeeded" },
                    { 1504, "PublicationJob", "Retry", true, 4, "status.publication_job.retry" },
                    { 1505, "PublicationJob", "Failed", true, 5, "status.publication_job.failed" },
                    { 1506, "PublicationJob", "Canceled", true, 6, "status.publication_job.canceled" }
                });

            migrationBuilder.InsertData(
                table: "PlatformDomains",
                columns: new[] { "Domain", "PlatformCode" },
                values: new object[,]
                {
                    { "facebook.com", "facebook" },
                    { "fb.com", "facebook" },
                    { "fb.me", "facebook" },
                    { "instagr.am", "instagram" },
                    { "instagram.com", "instagram" },
                    { "linkedin.com", "linkedin" },
                    { "snapchat.com", "snapchat" },
                    { "tiktok.com", "tiktok" },
                    { "twitter.com", "x" },
                    { "wa.me", "whatsapp" },
                    { "whatsapp.com", "whatsapp" },
                    { "x.com", "x" },
                    { "youtu.be", "youtube" },
                    { "youtube.com", "youtube" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiExecutions_BusinessId_TaskType_CreatedAt",
                table: "AiExecutions",
                columns: new[] { "BusinessId", "TaskType", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AiExecutions_StatusId",
                table: "AiExecutions",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_AppUsers_AuthSubject",
                table: "AppUsers",
                column: "AuthSubject",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppUsers_Email",
                table: "AppUsers",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AudienceSegments_BusinessId_Name",
                table: "AudienceSegments",
                columns: new[] { "BusinessId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AutomationPolicies_UpdatedBy",
                table: "AutomationPolicies",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_AutomationPolicyPlatforms_PlatformCode",
                table: "AutomationPolicyPlatforms",
                column: "PlatformCode");

            migrationBuilder.CreateIndex(
                name: "IX_BrandTerms_BusinessId_Rule_Term",
                table: "BrandTerms",
                columns: new[] { "BusinessId", "Rule", "Term" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Businesses_StatusId",
                table: "Businesses",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Businesses_TenantId",
                table: "Businesses",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_CampaignAudiences_AudienceSegmentId",
                table: "CampaignAudiences",
                column: "AudienceSegmentId");

            migrationBuilder.CreateIndex(
                name: "IX_CampaignOffers_OfferId",
                table: "CampaignOffers",
                column: "OfferId");

            migrationBuilder.CreateIndex(
                name: "IX_CampaignPlans_CampaignId_VersionNo",
                table: "CampaignPlans",
                columns: new[] { "CampaignId", "VersionNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CampaignPlans_GeneratedByExecutionId",
                table: "CampaignPlans",
                column: "GeneratedByExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_CampaignProducts_ProductId",
                table: "CampaignProducts",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Campaigns_BusinessId",
                table: "Campaigns",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_Campaigns_CreatedBy",
                table: "Campaigns",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Campaigns_StatusId",
                table: "Campaigns",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_CompetitorInsights_BusinessId",
                table: "CompetitorInsights",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_CompetitorInsights_TrackedSourceId",
                table: "CompetitorInsights",
                column: "TrackedSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentItems_CampaignId",
                table: "ContentItems",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentItems_InspiredBySourcePostId",
                table: "ContentItems",
                column: "InspiredBySourcePostId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentItems_PlatformCode",
                table: "ContentItems",
                column: "PlatformCode");

            migrationBuilder.CreateIndex(
                name: "IX_ContentItems_SourceRecommendationId",
                table: "ContentItems",
                column: "SourceRecommendationId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentItems_StatusId",
                table: "ContentItems",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentReviews_ReviewerId",
                table: "ContentReviews",
                column: "ReviewerId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentReviews_RevisionId",
                table: "ContentReviews",
                column: "RevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentReviews_StatusId",
                table: "ContentReviews",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentRevisionAssets_AssetId",
                table: "ContentRevisionAssets",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentRevisions_ContentItemId_RevisionNo",
                table: "ContentRevisions",
                columns: new[] { "ContentItemId", "RevisionNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContentRevisions_CreatedBy",
                table: "ContentRevisions",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ContentRevisions_GeneratedByExecutionId",
                table: "ContentRevisions",
                column: "GeneratedByExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentRevisions_SourceRecommendationId",
                table: "ContentRevisions",
                column: "SourceRecommendationId");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeSources_AssetId",
                table: "KnowledgeSources",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeSources_BusinessId",
                table: "KnowledgeSources",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeSources_StatusId",
                table: "KnowledgeSources",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaAssets_BusinessId",
                table: "MediaAssets",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaAssets_ObjectKey",
                table: "MediaAssets",
                column: "ObjectKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MediaAssets_ProductId",
                table: "MediaAssets",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_MetricObservations_PublicationId_MetricCode_ObservedAt",
                table: "MetricObservations",
                columns: new[] { "PublicationId", "MetricCode", "ObservedAt" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Offers_BusinessId",
                table: "Offers",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_Offers_StatusId",
                table: "Offers",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformDomains_PlatformCode",
                table: "PlatformDomains",
                column: "PlatformCode");

            migrationBuilder.CreateIndex(
                name: "IX_Platforms_DisplayNameKey",
                table: "Platforms",
                column: "DisplayNameKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_BusinessId_Sku",
                table: "Products",
                columns: new[] { "BusinessId", "Sku" },
                unique: true,
                filter: "[Sku] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PublicationJobs_ApprovedReviewId",
                table: "PublicationJobs",
                column: "ApprovedReviewId");

            migrationBuilder.CreateIndex(
                name: "IX_PublicationJobs_ContentItemId",
                table: "PublicationJobs",
                column: "ContentItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PublicationJobs_RevisionId",
                table: "PublicationJobs",
                column: "RevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_PublicationJobs_SocialAccountId_IdempotencyKey",
                table: "PublicationJobs",
                columns: new[] { "SocialAccountId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PublicationJobs_SourceRecommendationId",
                table: "PublicationJobs",
                column: "SourceRecommendationId");

            migrationBuilder.CreateIndex(
                name: "IX_PublicationJobs_StatusId_ScheduledAt",
                table: "PublicationJobs",
                columns: new[] { "StatusId", "ScheduledAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Publications_PublicationJobId",
                table: "Publications",
                column: "PublicationJobId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationEvidence_CompetitorInsightId",
                table: "RecommendationEvidence",
                column: "CompetitorInsightId");

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationEvidence_MetricObservationId",
                table: "RecommendationEvidence",
                column: "MetricObservationId");

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationEvidence_RecommendationId",
                table: "RecommendationEvidence",
                column: "RecommendationId");

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationEvidence_SourcePostId",
                table: "RecommendationEvidence",
                column: "SourcePostId");

            migrationBuilder.CreateIndex(
                name: "IX_SocialAccounts_BusinessId",
                table: "SocialAccounts",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_SocialAccounts_PlatformCode_ExternalAccountId",
                table: "SocialAccounts",
                columns: new[] { "PlatformCode", "ExternalAccountId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SocialAccounts_StatusId",
                table: "SocialAccounts",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_SourcePosts_TrackedSourceId_ExternalPostId",
                table: "SourcePosts",
                columns: new[] { "TrackedSourceId", "ExternalPostId" },
                unique: true,
                filter: "[ExternalPostId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Statuses_Category_Code",
                table: "Statuses",
                columns: new[] { "Category", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Statuses_TranslationKey",
                table: "Statuses",
                column: "TranslationKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StrategyRecommendations_CampaignId",
                table: "StrategyRecommendations",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_StrategyRecommendations_DecidedBy",
                table: "StrategyRecommendations",
                column: "DecidedBy");

            migrationBuilder.CreateIndex(
                name: "IX_StrategyRecommendations_StatusId",
                table: "StrategyRecommendations",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_StrategyRecommendations_StrategyRunId",
                table: "StrategyRecommendations",
                column: "StrategyRunId");

            migrationBuilder.CreateIndex(
                name: "IX_StrategyRuns_AiExecutionId",
                table: "StrategyRuns",
                column: "AiExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_StrategyRuns_BusinessId",
                table: "StrategyRuns",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_StrategyRuns_StatusId",
                table: "StrategyRuns",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_PaymentReference",
                table: "Subscriptions",
                column: "PaymentReference",
                unique: true,
                filter: "[PaymentReference] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_RenewedFromSubscriptionId",
                table: "Subscriptions",
                column: "RenewedFromSubscriptionId",
                unique: true,
                filter: "[RenewedFromSubscriptionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_StatusId",
                table: "Subscriptions",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_TenantId_StatusId_EndsAt",
                table: "Subscriptions",
                columns: new[] { "TenantId", "StatusId", "EndsAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TenantMemberships_StatusId",
                table: "TenantMemberships",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantMemberships_UserId",
                table: "TenantMemberships",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_StatusId",
                table: "Tenants",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_TrackedSources_AddedBy",
                table: "TrackedSources",
                column: "AddedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TrackedSources_BusinessId_NormalizedUrl",
                table: "TrackedSources",
                columns: new[] { "BusinessId", "NormalizedUrl" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrackedSources_PlatformCode",
                table: "TrackedSources",
                column: "PlatformCode");

            migrationBuilder.CreateIndex(
                name: "IX_TrackedSources_StatusId",
                table: "TrackedSources",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_UsageEvents_AiExecutionId",
                table: "UsageEvents",
                column: "AiExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_UsageEvents_BusinessId",
                table: "UsageEvents",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_UsageEvents_TenantId_IdempotencyKey",
                table: "UsageEvents",
                columns: new[] { "TenantId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UsageEvents_TenantId_OccurredAt",
                table: "UsageEvents",
                columns: new[] { "TenantId", "OccurredAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AutoApplyRules");

            migrationBuilder.DropTable(
                name: "AutomationPolicyPlatforms");

            migrationBuilder.DropTable(
                name: "BrandTerms");

            migrationBuilder.DropTable(
                name: "CampaignAudiences");

            migrationBuilder.DropTable(
                name: "CampaignOffers");

            migrationBuilder.DropTable(
                name: "CampaignPlans");

            migrationBuilder.DropTable(
                name: "CampaignProducts");

            migrationBuilder.DropTable(
                name: "ContentRevisionAssets");

            migrationBuilder.DropTable(
                name: "KnowledgeSources");

            migrationBuilder.DropTable(
                name: "PlatformDomains");

            migrationBuilder.DropTable(
                name: "RecommendationEvidence");

            migrationBuilder.DropTable(
                name: "Subscriptions");

            migrationBuilder.DropTable(
                name: "TenantMemberships");

            migrationBuilder.DropTable(
                name: "UsageEvents");

            migrationBuilder.DropTable(
                name: "AutomationPolicies");

            migrationBuilder.DropTable(
                name: "BrandProfiles");

            migrationBuilder.DropTable(
                name: "AudienceSegments");

            migrationBuilder.DropTable(
                name: "Offers");

            migrationBuilder.DropTable(
                name: "MediaAssets");

            migrationBuilder.DropTable(
                name: "CompetitorInsights");

            migrationBuilder.DropTable(
                name: "MetricObservations");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "Publications");

            migrationBuilder.DropTable(
                name: "PublicationJobs");

            migrationBuilder.DropTable(
                name: "ContentReviews");

            migrationBuilder.DropTable(
                name: "SocialAccounts");

            migrationBuilder.DropTable(
                name: "ContentRevisions");

            migrationBuilder.DropTable(
                name: "ContentItems");

            migrationBuilder.DropTable(
                name: "SourcePosts");

            migrationBuilder.DropTable(
                name: "StrategyRecommendations");

            migrationBuilder.DropTable(
                name: "TrackedSources");

            migrationBuilder.DropTable(
                name: "Campaigns");

            migrationBuilder.DropTable(
                name: "StrategyRuns");

            migrationBuilder.DropTable(
                name: "Platforms");

            migrationBuilder.DropTable(
                name: "AppUsers");

            migrationBuilder.DropTable(
                name: "AiExecutions");

            migrationBuilder.DropTable(
                name: "Businesses");

            migrationBuilder.DropTable(
                name: "Tenants");

            migrationBuilder.DropTable(
                name: "Statuses");
        }
    }
}
