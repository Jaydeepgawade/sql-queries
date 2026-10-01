namespace EquityAudit.Api.Models;

// HTTP 200 with HasData=false is a valid empty collection, not a server error.
public sealed record DashboardResponse(
    bool HasData, bool ShowPopup, string Code, string Message, DashboardData Data);

public sealed record DashboardSectionResponse<T>(
    bool HasData, bool ShowPopup, string Code, string Message, T Data);
