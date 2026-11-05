using SmartMarketingPlatform.Common.Pagination;
using SmartMarketingPlatform.DTOs.Reports;

namespace SmartMarketingPlatform.Services;

public interface IReportService
{
    Task<PagedResult<ReportSummaryResponse>> GetAllAsync(Guid businessId, PageQuery query, CancellationToken cancellationToken);
    Task<ReportResponse> GetAsync(Guid businessId, Guid id, CancellationToken cancellationToken);
    Task<string> GetHtmlAsync(Guid businessId, Guid id, CancellationToken cancellationToken);

    Task<ReportResponse> GenerateAsync(Guid businessId, GenerateReportRequest request, CancellationToken cancellationToken);
    Task<ReportResponse> SendAsync(Guid businessId, Guid id, CancellationToken cancellationToken);

    // Background: the weekly report of every business whose report moment has passed. Returns how many were made.
    Task<int> GenerateDueReportsAsync(CancellationToken cancellationToken);
}
