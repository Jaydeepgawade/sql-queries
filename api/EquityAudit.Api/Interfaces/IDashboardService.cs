using EquityAudit.Api.Models;

namespace EquityAudit.Api.Interfaces;

public interface IDashboardService
{
    Task<DashboardResponse> GetAsync(DashboardFilter filter, DashboardRole role, CancellationToken token);
    Task<IReadOnlyList<string>> GetAvailableQuartersAsync(DashboardRole role, CancellationToken token);
}
