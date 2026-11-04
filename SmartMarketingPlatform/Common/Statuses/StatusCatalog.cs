using Microsoft.EntityFrameworkCore;
using SmartMarketingPlatform.Common.Errors;
using SmartMarketingPlatform.Common.Localization;
using SmartMarketingPlatform.Data;
using SmartMarketingPlatform.DTOs.Common;
using SmartMarketingPlatform.Entities;
using SmartMarketingPlatform.Services;

namespace SmartMarketingPlatform.Common.Statuses;

// Statuses are seed data that only change with a deployment, so they are read once
// and kept in memory. Responses get a translated StatusInfo without joining the Statuses table.
public sealed class StatusCatalog(
    IServiceScopeFactory scopeFactory,
    TranslationService translations,
    IHttpContextAccessor httpContextAccessor)
{
    // PublicationOnly: if the database is down on the first call, the failure is not cached.
    private readonly Lazy<IReadOnlyDictionary<int, Status>> _statuses =
        new(() => Load(scopeFactory), LazyThreadSafetyMode.PublicationOnly);

    public StatusInfo Describe(int statusId)
    {
        if (!_statuses.Value.TryGetValue(statusId, out var status))
            throw new InvalidOperationException($"Unknown status id {statusId}.");

        var language = RequestLanguage.Current(httpContextAccessor.HttpContext);
        return new StatusInfo(status.Id, status.Code, translations.Translate(status.TranslationKey, language));
    }

    public string CodeOf(int statusId) =>
        _statuses.Value.TryGetValue(statusId, out var status) ? status.Code : statusId.ToString();

    // Turns a code sent by the client ("Paused") into its id; unknown codes are a 400.
    public int GetId(string category, string code)
    {
        var match = _statuses.Value.Values.FirstOrDefault(s =>
            s.Category == category && string.Equals(s.Code, code.Trim(), StringComparison.OrdinalIgnoreCase));

        return match?.Id ?? throw new BadRequestException(
            $"'{code}' is not a valid {category} status. Valid values: {string.Join(", ", Codes(category))}.");
    }

    private IEnumerable<string> Codes(string category) =>
        _statuses.Value.Values.Where(s => s.Category == category).OrderBy(s => s.SortOrder).Select(s => s.Code);

    private static IReadOnlyDictionary<int, Status> Load(IServiceScopeFactory scopeFactory)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MarketingDbContext>();
        return db.Statuses.AsNoTracking().ToDictionary(s => s.Id);
    }
}
