using EquityAudit.Api.Models;

namespace EquityAudit.Api.Interfaces;

public interface IDashboardRepository
{
    Task<DashboardData> GetAsync(DashboardFilter filter, DashboardRole role, CancellationToken token);
    Task<IReadOnlyList<string>> GetAvailableQuartersAsync(DashboardRole role, CancellationToken token);
}
