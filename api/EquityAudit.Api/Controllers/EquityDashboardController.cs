using System.Globalization;
using EquityAudit.Api.Interfaces;
using EquityAudit.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EquityAudit.Api.Controllers;

[ApiController]
[Route("api/equity-dashboard")]
[Authorize(Policy = "DashboardReader")]
public sealed class EquityDashboardController(IDashboardService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<DashboardResponse>> Get(
        [FromQuery] DashboardFilter filter, CancellationToken token) =>
        Ok(await service.GetAsync(filter, CurrentRole(), token));

    [HttpGet("quarters/available")]
    public async Task<ActionResult<IReadOnlyList<string>>> AvailableQuarters(CancellationToken token) =>
        Ok(await service.GetAvailableQuartersAsync(CurrentRole(), token));

    [HttpGet("totals")]
    public Task<ActionResult<DashboardSectionResponse<DashboardTotals>>> Totals(
        [FromQuery] DashboardFilter filter, CancellationToken token) =>
        Section(filter, d => d.Totals, token);

    [HttpGet("statuses")]
    public Task<ActionResult<DashboardSectionResponse<IReadOnlyList<StatusCard>>>> Statuses(
        [FromQuery] DashboardFilter filter, CancellationToken token) =>
        Section(filter, d => d.StatusCards, token);

    [HttpGet("quarters")]
    public Task<ActionResult<DashboardSectionResponse<IReadOnlyList<QuarterSummary>>>> Quarters(
        [FromQuery] DashboardFilter filter, CancellationToken token) =>
        Section(filter, d => d.Quarters, token);

    [HttpGet("positions")]
    public Task<ActionResult<DashboardSectionResponse<IReadOnlyList<PositionSummary>>>> Positions(
        [FromQuery] DashboardFilter filter, CancellationToken token) =>
        Section(filter, d => d.Positions, token);

    [HttpGet("rms")]
    public Task<ActionResult<DashboardSectionResponse<IReadOnlyList<RmSummary>>>> Rms(
        [FromQuery] DashboardFilter filter, CancellationToken token) =>
        Section(filter, d => d.Rms, token);

    [HttpGet("incentive-types")]
    public Task<ActionResult<DashboardSectionResponse<IReadOnlyList<IncentiveTypeSummary>>>> IncentiveTypes(
        [FromQuery] DashboardFilter filter, CancellationToken token) =>
        Section(filter, d => d.IncentiveTypes, token);

    [HttpGet("details")]
    public Task<ActionResult<DashboardSectionResponse<IReadOnlyList<IncentiveDetail>>>> Details(
        [FromQuery] DashboardFilter filter, CancellationToken token) =>
        Section(filter, d => d.Details, token);

    private async Task<ActionResult<DashboardSectionResponse<T>>> Section<T>(
        DashboardFilter filter, Func<DashboardData, T> select, CancellationToken token)
    {
        var response = await service.GetAsync(filter, CurrentRole(), token);
        return Ok(new DashboardSectionResponse<T>(response.HasData, response.ShowPopup,
            response.Code, response.Message, select(response.Data)));
    }

    private DashboardRole CurrentRole() => (DashboardRole)short.Parse(
        User.FindFirst("dashboard_role")!.Value, CultureInfo.InvariantCulture);
}
