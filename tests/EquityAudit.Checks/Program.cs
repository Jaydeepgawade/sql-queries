using System.ComponentModel.DataAnnotations;
using EquityAudit.Api.Interfaces;
using EquityAudit.Api.Models;
using EquityAudit.Api.Services;

var checks = new List<(string Name, Func<Task> Run)>
{
    ("Missing quarter produces popup metadata", async () =>
    {
        var repo = new FakeRepository();
        var result = await new DashboardService(repo).GetAsync(
            new DashboardFilter { QuarterFrom = "202703", QuarterTo = "202703" }, DashboardRole.Admin, default);
        Require(!result.HasData && result.ShowPopup && result.Code == "QUARTER_DATA_NOT_FOUND", "Missing quarter metadata");
        Require(result.Message.Contains("202703"), "Message identifies selected quarter");
    }),
    ("Existing quarter with unmatched filters has a different message", async () =>
    {
        var repo = new FakeRepository { Quarters = ["202603"] };
        var result = await new DashboardService(repo).GetAsync(
            new DashboardFilter { QuarterFrom = "202603", RmCode = "RM-NOT-FOUND" }, DashboardRole.Verifier, default);
        Require(result.Code == "FILTER_DATA_NOT_FOUND", "Do not report a missing quarter");
        Require(repo.LastRole == DashboardRole.Verifier, "Role is propagated");
    }),
    ("Populated result does not display popup", async () =>
    {
        var repo = new FakeRepository { Data = new DashboardData { Totals = new DashboardTotals { IncentiveRecordCount = 2 } } };
        var result = await new DashboardService(repo).GetAsync(new DashboardFilter(), DashboardRole.Hr, default);
        Require(result.HasData && !result.ShowPopup && result.Code == "OK", "Data response");
        Require(repo.QuarterCalls == 0, "No unnecessary availability query");
    }),
    ("Any valid YYYYMM key is accepted, including non-quarter-end source keys", async () =>
    {
        await new DashboardService(new FakeRepository()).GetAsync(
            new DashboardFilter { QuarterFrom = "202604" }, DashboardRole.Admin, default);
    }),
    ("Invalid month fails before accessing SQL", async () =>
    {
        var repo = new FakeRepository();
        await ExpectInvalid(() => new DashboardService(repo).GetAsync(
            new DashboardFilter { QuarterFrom = "202613" }, DashboardRole.Admin, default));
        Require(repo.DataCalls == 0, "Invalid filter must not reach SQL");
    }),
    ("Reversed quarter range is rejected", () => ExpectInvalid(() =>
        new DashboardService(new FakeRepository()).GetAsync(
            new DashboardFilter { QuarterFrom = "202606", QuarterTo = "202603" }, DashboardRole.Admin, default))),
    ("Unknown role is rejected before SQL", async () =>
    {
        var repo = new FakeRepository();
        await ExpectInvalid(() => new DashboardService(repo).GetAsync(new DashboardFilter(), (DashboardRole)0, default));
        Require(repo.DataCalls == 0, "Unknown role must not reach SQL");
    })
};
var failed = 0;
foreach (var (name, run) in checks)
{
    try { await run(); Console.WriteLine($"PASS {name}"); }
    catch (Exception ex) { failed++; Console.Error.WriteLine($"FAIL {name}: {ex.Message}"); }
}
Environment.ExitCode = failed == 0 ? 0 : 1;

static void Require(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}
static async Task ExpectInvalid(Func<Task<DashboardResponse>> action)
{
    try { await action(); }
    catch (ValidationException) { return; }
    throw new Exception("Expected ValidationException");
}
sealed class FakeRepository : IDashboardRepository
{
    public DashboardData Data { get; init; } = new();
    public IReadOnlyList<string> Quarters { get; init; } = [];
    public DashboardRole LastRole { get; private set; }
    public int DataCalls { get; private set; }
    public int QuarterCalls { get; private set; }
    public Task<DashboardData> GetAsync(DashboardFilter filter, DashboardRole role, CancellationToken token)
    {
        token.ThrowIfCancellationRequested(); DataCalls++; LastRole = role;
        return Task.FromResult(Data);
    }
    public Task<IReadOnlyList<string>> GetAvailableQuartersAsync(DashboardRole role, CancellationToken token)
    {
        token.ThrowIfCancellationRequested(); QuarterCalls++; LastRole = role;
        return Task.FromResult(Quarters);
    }
}
