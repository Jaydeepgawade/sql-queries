namespace EquityAudit.Api.Models;

public class IncentiveAmounts
{
    public decimal GrossIncentive { get; set; }
    public decimal AdjustmentAmt { get; set; }
    public decimal FileAdjAmt { get; set; }
    public decimal NetIncentive { get; set; }
}

public sealed class DashboardTotals : IncentiveAmounts
{
    public long IncentiveRecordCount { get; set; }
    public int TotalRM { get; set; }
    public long RMQuarterCount { get; set; }
    public decimal YearHoldAmt { get; set; }
    public decimal FinPay { get; set; }
}

public sealed class StatusCard : IncentiveAmounts
{
    public short StatusCode { get; set; }
    public string StatusName { get; set; } = "";
    public int FlowOrder { get; set; }
    public bool IsRejected { get; set; }
    public long IncentiveRecordCount { get; set; }
    public int TotalRM { get; set; }
    public long RMQuarterCount { get; set; }
}

public sealed class QuarterSummary : IncentiveAmounts
{
    public string Quarter { get; set; } = "";
    public long IncentiveRecordCount { get; set; }
    public int TotalRM { get; set; }
}

public sealed class PositionSummary : IncentiveAmounts
{
    public string? PositionName { get; set; }
    public long IncentiveRecordCount { get; set; }
    public int TotalRM { get; set; }
}

public sealed class RmSummary : IncentiveAmounts
{
    public string RmCode { get; set; } = "";
    public string? RmName { get; set; }
    public string Quarter { get; set; } = "";
    public long IncentiveRecordCount { get; set; }
}

public sealed class IncentiveTypeSummary : IncentiveAmounts
{
    public string IncentiveType { get; set; } = "";
    public long IncentiveRecordCount { get; set; }
}

public sealed class IncentiveDetail : IncentiveAmounts
{
    public string Quarter { get; set; } = "";
    public string RmCode { get; set; } = "";
    public string? RmName { get; set; }
    public string? PositionName { get; set; }
    public string IncentiveType { get; set; } = "";
    public short StatusCode { get; set; }
    public string StatusName { get; set; } = "";
    public int? NismValid { get; set; }
    public string NismStatus { get; set; } = "";
    public string? FilePath { get; set; }
    public string? UpdatedBy { get; set; }
    public string? Remarks { get; set; }
    public decimal YearHoldAmt { get; set; }
    public decimal FinPay { get; set; }
    public string BtnFlag { get; set; } = "N";
}

public sealed class DashboardData
{
    public DashboardTotals Totals { get; init; } = new();
    public IReadOnlyList<StatusCard> StatusCards { get; init; } = [];
    public IReadOnlyList<QuarterSummary> Quarters { get; init; } = [];
    public IReadOnlyList<PositionSummary> Positions { get; init; } = [];
    public IReadOnlyList<RmSummary> Rms { get; init; } = [];
    public IReadOnlyList<IncentiveTypeSummary> IncentiveTypes { get; init; } = [];
    public IReadOnlyList<IncentiveDetail> Details { get; init; } = [];
}
