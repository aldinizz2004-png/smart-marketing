using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using SmartMarketingPlatform.Common.Businesses;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using SmartMarketingPlatform.Common.Ai;
using SmartMarketingPlatform.Common.Ai.Llm;
using SmartMarketingPlatform.Common.Auth;
using SmartMarketingPlatform.Common.Brand;
using SmartMarketingPlatform.Common.Errors;
using SmartMarketingPlatform.Common.Health;
using SmartMarketingPlatform.Common.Competitors;
using SmartMarketingPlatform.Common.Integrations.Instagram;
using SmartMarketingPlatform.Common.Integrations.Postproxy;
using SmartMarketingPlatform.Common.Integrations.WhatsApp;
using SmartMarketingPlatform.Common.Logging;
using SmartMarketingPlatform.Common.Messaging;
using SmartMarketingPlatform.Common.Notifications;
using SmartMarketingPlatform.Common.Reports;
using SmartMarketingPlatform.Common.Options;
using SmartMarketingPlatform.Common.RateLimiting;
using SmartMarketingPlatform.Common.Publishing;
using SmartMarketingPlatform.Common.Secrets;
using SmartMarketingPlatform.Common.Storage;
using SmartMarketingPlatform.Common.Statuses;
using SmartMarketingPlatform.Common.Subscriptions;
using SmartMarketingPlatform.Common.Web;
using SmartMarketingPlatform.Data;
using SmartMarketingPlatform.Entities;
using SmartMarketingPlatform.Enums;
using SmartMarketingPlatform.Services;
using SmartMarketingPlatform.Workers;

var builder = WebApplication.CreateBuilder(args);
builder.AddAppLogging();

var connectionString = builder.Configuration.GetConnectionString("MarketingDb")
    ?? throw new InvalidOperationException("Connection string 'MarketingDb' was not found.");

// ---- Data -------------------------------------------------------------------
builder.Services.AddDbContext<MarketingDbContext>(options => options.UseSqlServer(connectionString));

// ---- Authentication (JWT) ---------------------------------------------------
var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
var jwt = jwtSection.Get<JwtOptions>() ?? throw new InvalidOperationException("The 'Jwt' configuration section is missing.");
if (jwt.Key.Length < JwtOptions.MinKeyLength)
    throw new InvalidOperationException(
        $"Jwt:Key must be at least {JwtOptions.MinKeyLength} characters. Set it with the Jwt__Key environment variable.");
builder.Services.Configure<JwtOptions>(jwtSection);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;   // keep "sub", "role", "tid" as they are
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = AppClaims.Name,
            RoleClaimType = AppClaims.Role
        };
    });

// Secure by default: every endpoint needs a valid token unless it says [AllowAnonymous].
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
    .AddPolicy(Policies.TenantOwner, p => p.RequireRole(nameof(MembershipRole.Owner)))
    .AddPolicy(Policies.TenantAdmin, p => p.RequireRole(nameof(MembershipRole.Owner), nameof(MembershipRole.Admin)))
    .AddPolicy(Policies.CanEdit, p => p.RequireRole(
        nameof(MembershipRole.Owner), nameof(MembershipRole.Admin), nameof(MembershipRole.Editor)))
    .AddPolicy(Policies.CanReview, p => p.RequireRole(
        nameof(MembershipRole.Owner), nameof(MembershipRole.Admin), nameof(MembershipRole.Reviewer)))
    .AddPolicy(Policies.ActiveSubscription, p => p.RequireAuthenticatedUser().AddRequirements(new SubscriptionRequirement()));

builder.Services.AddScoped<IAuthorizationHandler, SubscriptionRequirementHandler>();              // reads the DB per request
builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, ProblemDetailsAuthorizationResultHandler>();   // 402

// ---- API: controllers, JSON, errors, docs -----------------------------------
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));   // enums as "Owner", not 0

// Every error (validation, 401/403/404, exceptions) is returned as RFC 7807 ProblemDetails.
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
{
    context.ProblemDetails.Instance ??= $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}";
    context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
    if (CorrelationId.Of(context.HttpContext) is { } correlationId)
        context.ProblemDetails.Extensions["correlationId"] = correlationId;   // the id to search for in the logs
});
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // "Authorize" button in Swagger UI: paste the accessToken from /api/auth/login.
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Paste the accessToken returned by /api/auth/login."
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});

builder.Services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database");

// Requests per minute: per user for everything, per IP for login/register, per tenant for AI calls (429 + Retry-After).
builder.Services.AddAppRateLimiting(builder.Configuration);

// The web app (web/) calls the API from its own origin: Cors:AllowedOrigins.
builder.Services.AddAppCors(builder.Configuration);

// ---- Application services ---------------------------------------------------
builder.Services.Configure<SubscriptionPricingOptions>(builder.Configuration.GetSection(SubscriptionPricingOptions.SectionName));

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<TranslationService>();
builder.Services.AddSingleton<StatusCatalog>();
builder.Services.AddSingleton<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();
builder.Services.AddSingleton<ITokenService, JwtTokenService>();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

builder.Services.AddScoped<PlatformDetector>();
builder.Services.AddScoped<ILookupService, LookupService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IPlanLimits, PlanLimits>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
builder.Services.AddScoped<ITenantService, TenantService>();

builder.Services.AddScoped<IBusinessAccess, BusinessAccess>();
builder.Services.AddScoped<IBusinessService, BusinessService>();
builder.Services.AddScoped<IBrandService, BrandService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IOfferService, OfferService>();
builder.Services.AddScoped<IAudienceSegmentService, AudienceSegmentService>();

// Files and secrets: local implementations in development, cloud services later (same interfaces).
builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection(StorageOptions.SectionName));
// Data Protection keys encrypt the social-media tokens. In Docker they are kept in a volume
// (DataProtection__KeysPath); otherwise a restarted container would get new keys and could
// no longer read the tokens it saved before.
var dataProtection = builder.Services.AddDataProtection();
var keysPath = builder.Configuration["DataProtection:KeysPath"];
if (!string.IsNullOrWhiteSpace(keysPath))
    dataProtection.PersistKeysToFileSystem(new DirectoryInfo(keysPath)).SetApplicationName("SmartMarketingPlatform");
builder.Services.AddSingleton<IFileStorage, LocalFileStorage>();
builder.Services.AddSingleton<ISecretStore, LocalSecretStore>();

builder.Services.AddScoped<IImageDescriber, ImageDescriber>();
builder.Services.AddScoped<IMediaService, MediaService>();
builder.Services.AddScoped<IKnowledgeSourceService, KnowledgeSourceService>();
builder.Services.AddScoped<ISocialAccountService, SocialAccountService>();
builder.Services.AddScoped<IAutomationPolicyService, AutomationPolicyService>();

// AI: every call goes through IMarketingAi and is recorded by IAiExecutionRunner.
// Ai:Provider = Fake (tests, demos, no keys) or Llm (real models from Ai:Chain, with fallback).
builder.Services.Configure<AiOptions>(builder.Configuration.GetSection(AiOptions.SectionName));
var aiOptions = builder.Configuration.GetSection(AiOptions.SectionName).Get<AiOptions>() ?? new AiOptions();
if (string.Equals(aiOptions.Provider, "Llm", StringComparison.OrdinalIgnoreCase))
{
    var aiTimeout = TimeSpan.FromSeconds(aiOptions.TimeoutSeconds);
    builder.Services.AddHttpClient(HttpClientNames.Gemini, c => { c.BaseAddress = new Uri(aiOptions.Gemini.BaseUrl); c.Timeout = aiTimeout; });
    builder.Services.AddHttpClient(HttpClientNames.Groq, c => { c.BaseAddress = new Uri(aiOptions.Groq.BaseUrl); c.Timeout = aiTimeout; });
    builder.Services.AddSingleton<ChatModelChain>();   // singleton: keeps the cooldown of failing models
    builder.Services.AddSingleton<IMarketingAi, LlmMarketingAi>();
}
else
{
    builder.Services.AddSingleton<IMarketingAi, FakeMarketingAi>();
}
builder.Services.AddScoped<IAiExecutionRunner, AiExecutionRunner>();
builder.Services.AddScoped<IMarketingContextBuilder, MarketingContextBuilder>();

builder.Services.AddScoped<ICampaignService, CampaignService>();
builder.Services.AddScoped<IStrategyService, StrategyService>();
builder.Services.AddScoped<IAiUsageService, AiUsageService>();
builder.Services.AddScoped<IBrandGuard, BrandGuard>();
builder.Services.AddScoped<IContentService, ContentService>();

// Publishing: jobs are created by the API and executed by a background worker in the same process.
builder.Services.Configure<PublishingOptions>(builder.Configuration.GetSection(PublishingOptions.SectionName));
builder.Services.Configure<PostproxyOptions>(builder.Configuration.GetSection(PostproxyOptions.SectionName));
var postproxy = builder.Configuration.GetSection(PostproxyOptions.SectionName).Get<PostproxyOptions>() ?? new PostproxyOptions();
builder.Services.AddHttpClient<PostproxyClient>(c =>
{
    c.BaseAddress = new Uri(postproxy.BaseUrl);
    c.Timeout = TimeSpan.FromSeconds(postproxy.TimeoutSeconds);
    if (postproxy.IsConfigured)
        c.DefaultRequestHeaders.Authorization = new("Bearer", postproxy.ApiKey);
});
builder.Services.AddScoped<ISocialConnectionService, SocialConnectionService>();
builder.Services.AddScoped<IPostproxyWebhookService, PostproxyWebhookService>();

// Publishing:Provider = Fake (default) or Postproxy (real Facebook, Instagram, TikTok ... accounts).
if (string.Equals(builder.Configuration["Publishing:Provider"], "Postproxy", StringComparison.OrdinalIgnoreCase))
{
    if (!postproxy.IsConfigured)
        throw new InvalidOperationException("Publishing:Provider is 'Postproxy' but Postproxy:ApiKey is not set.");
    builder.Services.AddScoped<ISocialPublisher, PostproxySocialPublisher>();
    builder.Services.AddScoped<IMetricsProvider, PostproxyMetricsProvider>();
    builder.Services.AddScoped<ICommentGateway, PostproxyCommentGateway>();
}
else
{
    builder.Services.AddSingleton<ISocialPublisher, FakeSocialPublisher>();
    builder.Services.AddSingleton<IMetricsProvider, FakeMetricsProvider>();
    builder.Services.AddSingleton<ICommentGateway, FakeCommentGateway>();
}
builder.Services.AddScoped<IPublishingService, PublishingService>();
builder.Services.AddScoped<IPublicationProcessor, PublicationProcessor>();
builder.Services.AddScoped<IPerformanceService, PerformanceService>();
builder.Services.AddScoped<ICommentService, CommentService>();
if (builder.Configuration.GetValue("Publishing:WorkerEnabled", true))
    builder.Services.AddHostedService<PublishingWorker>();

builder.Services.Configure<CommentOptions>(builder.Configuration.GetSection(CommentOptions.SectionName));
if (builder.Configuration.GetValue("Comments:WorkerEnabled", true))
    builder.Services.AddHostedService<CommentWorker>();

// Weekly owner reports, sent by email (Email:Provider = File in development: .html files in App_Data/outbox).
builder.Services.Configure<ReportOptions>(builder.Configuration.GetSection(ReportOptions.SectionName));
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection(EmailOptions.SectionName));
if (string.Equals(builder.Configuration["Email:Provider"], "Smtp", StringComparison.OrdinalIgnoreCase))
    builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();
else
    builder.Services.AddSingleton<IEmailSender, FileEmailSender>();
builder.Services.AddScoped<IReportService, ReportService>();
if (builder.Configuration.GetValue("Reports:WorkerEnabled", true))
    builder.Services.AddHostedService<ReportWorker>();

// Competitors: accounts the owner learns from. Competitors:Provider = Fake (demo posts) or Instagram (Business Discovery).
builder.Services.Configure<CompetitorOptions>(builder.Configuration.GetSection(CompetitorOptions.SectionName));
var competitorOptions = builder.Configuration.GetSection(CompetitorOptions.SectionName).Get<CompetitorOptions>() ?? new CompetitorOptions();
if (string.Equals(competitorOptions.Provider, "Instagram", StringComparison.OrdinalIgnoreCase))
{
    if (!competitorOptions.Instagram.IsConfigured)
        throw new InvalidOperationException("Competitors:Provider is 'Instagram' but Competitors:Instagram:BusinessAccountId or AccessToken is not set.");
    builder.Services.AddHttpClient<ICompetitorSource, InstagramDiscoverySource>(c =>
    {
        c.BaseAddress = new Uri(competitorOptions.Instagram.GraphBaseUrl);
        c.Timeout = TimeSpan.FromSeconds(competitorOptions.Instagram.TimeoutSeconds);
    });
}
else
{
    builder.Services.AddSingleton<ICompetitorSource, FakeCompetitorSource>();
}
builder.Services.AddScoped<ICopyGuard, CopyGuard>();
builder.Services.AddScoped<ICompetitorService, CompetitorService>();
if (competitorOptions.WorkerEnabled)
    builder.Services.AddHostedService<CompetitorWorker>();

// WhatsApp inbox. WhatsApp:Provider = Fake (nothing is sent) or Meta (WhatsApp Cloud API).
builder.Services.Configure<WhatsAppOptions>(builder.Configuration.GetSection(WhatsAppOptions.SectionName));
var whatsApp = builder.Configuration.GetSection(WhatsAppOptions.SectionName).Get<WhatsAppOptions>() ?? new WhatsAppOptions();
if (string.Equals(whatsApp.Provider, "Meta", StringComparison.OrdinalIgnoreCase))
{
    if (string.IsNullOrWhiteSpace(whatsApp.AppSecret) || string.IsNullOrWhiteSpace(whatsApp.VerifyToken))
        throw new InvalidOperationException("WhatsApp:Provider is 'Meta' but WhatsApp:AppSecret or WhatsApp:VerifyToken is not set.");
    builder.Services.AddHttpClient<IWhatsAppGateway, MetaWhatsAppGateway>(c =>
    {
        c.BaseAddress = new Uri(whatsApp.GraphBaseUrl);
        c.Timeout = TimeSpan.FromSeconds(whatsApp.TimeoutSeconds);
    });
}
else
{
    builder.Services.AddSingleton<IWhatsAppGateway, FakeWhatsAppGateway>();
}
builder.Services.AddScoped<IConversationMessenger, ConversationMessenger>();
builder.Services.AddScoped<IWhatsAppInboxService, WhatsAppInboxService>();
builder.Services.AddScoped<IConversationService, ConversationService>();
if (whatsApp.WorkerEnabled)
    builder.Services.AddHostedService<WhatsAppWorker>();

var app = builder.Build();

// ---- Pipeline (order matters) -----------------------------------------------
app.UseMiddleware<CorrelationIdMiddleware>();   // first: every log line of the request carries its id
app.UseAppRequestLogging();                     // one summary line per request, with its final status code
app.UseExceptionHandler();      // catches exceptions from everything below
app.UseStatusCodePages();       // empty 401/403/404/405 responses also get a ProblemDetails body

// Swagger: always in Development; elsewhere only when Swagger__Enabled=true (e.g. the Docker demo).
if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Swagger:Enabled"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Inside a container the app listens on plain HTTP (8080); TLS belongs to the reverse proxy in front of it.
// The official .NET images set DOTNET_RUNNING_IN_CONTAINER=true.
if (!app.Configuration.GetValue<bool>("DOTNET_RUNNING_IN_CONTAINER"))
    app.UseHttpsRedirection();

app.UseCors();                  // before authentication and rate limiting: browser preflights end here
app.UseAuthentication();        // who are you? (reads the Bearer token)
app.UseMiddleware<RequestLogContextMiddleware>();  // user, tenant and business on every log line from here on
app.UseRateLimiter();           // how often? (after authentication, so limits can be per user and per tenant)
app.UseAuthorization();         // are you allowed? (policies, roles)

app.MapControllers();
app.MapHealthChecks("/health", new HealthCheckOptions { ResponseWriter = HealthResponseWriter.WriteAsync })
   .AllowAnonymous()
   .DisableRateLimiting();      // monitoring and Docker check it often

// Docker: apply the existing migrations before the API and the publishing worker start.
await DatabaseMigrator.ApplyMigrationsAsync(app);

app.Run();

// Lets the integration test project start the API (WebApplicationFactory<Program>).
public partial class Program;
