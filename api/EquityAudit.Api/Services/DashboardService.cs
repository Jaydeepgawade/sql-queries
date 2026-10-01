using System.ComponentModel.DataAnnotations;
using System.Globalization;
using EquityAudit.Api.Interfaces;
using EquityAudit.Api.Models;

namespace EquityAudit.Api.Services;

public sealed class DashboardService(IDashboardRepository repository) : IDashboardService
{
    public async Task<DashboardResponse> GetAsync(
        DashboardFilter filter, DashboardRole role, CancellationToken token)
    {
        filter = filter.Normalize();
        Validate(filter, role);
        var data = await repository.GetAsync(filter, role, token);
        if (data.Totals.IncentiveRecordCount > 0)
            return new(true, false, "OK", "Dashboard data loaded.", data);

        // Check quarter availability independently of RM/position/status filters.
        // Availability stays role-scoped: do not disclose another role's records.
        if (filter.QuarterFrom is not null || filter.QuarterTo is not null)
        {
            var quarters = await repository.GetAvailableQuartersAsync(role, token);
            var exists = quarters.Any(q =>
                (filter.QuarterFrom is null || string.CompareOrdinal(q, filter.QuarterFrom) >= 0) &&
                (filter.QuarterTo is null || string.CompareOrdinal(q, filter.QuarterTo) <= 0));
            if (!exists)
                return new(false, true, "QUARTER_DATA_NOT_FOUND",
                    $"No dashboard data is available for quarter {DescribeQuarter(filter)} in your role's scope.", data);
        }
        return new(false, true, "FILTER_DATA_NOT_FOUND",
            "No records match the selected filters in your role's scope.", data);
    }

    public Task<IReadOnlyList<string>> GetAvailableQuartersAsync(DashboardRole role, CancellationToken token)
    {
        ValidateRole(role);
        return repository.GetAvailableQuartersAsync(role, token);
    }

    private static void Validate(DashboardFilter filter, DashboardRole role)
    {
        ValidateRole(role);
        Validator.ValidateObject(filter, new ValidationContext(filter), validateAllProperties: true);
        ValidateQuarter(filter.QuarterFrom);
        ValidateQuarter(filter.QuarterTo);
        if (filter.QuarterFrom is not null && filter.QuarterTo is not null &&
            string.CompareOrdinal(filter.QuarterFrom, filter.QuarterTo) > 0)
            throw new ValidationException("QuarterFrom must not exceed QuarterTo.");
        // RM range ordering is determined by SQL Server's collation, not .NET ordinal comparison.
    }

    private static void ValidateRole(DashboardRole role)
    {
        if (!Enum.IsDefined(typeof(DashboardRole), role))
            throw new ValidationException("A valid authenticated dashboard role is required.");
    }

    private static void ValidateQuarter(string? quarter)
    {
        if (quarter is null) return;
        if (quarter.Length != 6 || quarter.Any(c => c is < '0' or > '9') ||
            !DateTime.TryParseExact(quarter + "01", "yyyyMMdd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out _))
            throw new ValidationException("Quarter must be a valid YYYYMM key, for example 202603.");
    }

    private static string DescribeQuarter(DashboardFilter filter) =>
        filter.QuarterFrom == filter.QuarterTo ? filter.QuarterFrom! :
        filter.QuarterFrom is null ? $"up to {filter.QuarterTo}" :
        filter.QuarterTo is null ? $"from {filter.QuarterFrom}" :
        $"range {filter.QuarterFrom} to {filter.QuarterTo}";
}
