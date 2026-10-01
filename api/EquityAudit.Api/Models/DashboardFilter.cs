using System.ComponentModel.DataAnnotations;

namespace EquityAudit.Api.Models;

// Role is deliberately absent: it comes from the validated token.
public sealed record DashboardFilter
{
    [StringLength(6)] public string? QuarterFrom { get; init; }
    [StringLength(6)] public string? QuarterTo { get; init; }
    [StringLength(20)] public string? RmCode { get; init; }
    [StringLength(20)] public string? RmFrom { get; init; }
    [StringLength(20)] public string? RmTo { get; init; }
    [StringLength(100)] public string? PositionName { get; init; }
    [Range(0, 7)] public short? Status { get; init; }

    public DashboardFilter Normalize() => this with
    {
        QuarterFrom = Clean(QuarterFrom), QuarterTo = Clean(QuarterTo),
        RmCode = Clean(RmCode), RmFrom = Clean(RmFrom), RmTo = Clean(RmTo),
        PositionName = Clean(PositionName)
    };

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
