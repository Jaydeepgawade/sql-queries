using System.Data;
using System.Globalization;
using System.Reflection;
using EquityAudit.Api.Infrastructure;
using EquityAudit.Api.Interfaces;
using EquityAudit.Api.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace EquityAudit.Api.Repositories;

public sealed class DashboardRepository(IOptions<DatabaseSettings> options) : IDashboardRepository
{
    private readonly DatabaseSettings _settings = options.Value;

    public async Task<DashboardData> GetAsync(
        DashboardFilter filter, DashboardRole role, CancellationToken token)
    {
        await using var connection = new SqlConnection(_settings.ConnectionString);
        await connection.OpenAsync(token);
        await using var command = new SqlCommand("dbo.Fab_Ims_EquityDashboard", connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = _settings.CommandTimeoutSeconds
        };
        AddText(command, "@GenDtFrm", 30, filter.QuarterFrom);
        AddText(command, "@GenDtTo", 30, filter.QuarterTo);
        AddText(command, "@RmCode", 20, filter.RmCode);
        AddText(command, "@PRmFrm", 20, filter.RmFrom);
        AddText(command, "@PRmTo", 20, filter.RmTo);
        AddText(command, "@PositionName", 100, filter.PositionName);
        command.Parameters.Add("@PStatus", SqlDbType.SmallInt).Value = (object?)filter.Status ?? DBNull.Value;
        command.Parameters.Add("@Prole", SqlDbType.SmallInt).Value = (short)role;

        await using var reader = await command.ExecuteReaderAsync(token);
        var totals = await ReadAsync<DashboardTotals>(reader, token);
        if (totals.Count != 1) throw new InvalidOperationException("Expected one dashboard totals row.");
        await NextAsync(reader, token);
        var statusCards = await ReadAsync<StatusCard>(reader, token);
        await NextAsync(reader, token);
        var quarters = await ReadAsync<QuarterSummary>(reader, token);
        await NextAsync(reader, token);
        var positions = await ReadAsync<PositionSummary>(reader, token);
        await NextAsync(reader, token);
        var rms = await ReadAsync<RmSummary>(reader, token);
        await NextAsync(reader, token);
        var types = await ReadAsync<IncentiveTypeSummary>(reader, token);
        await NextAsync(reader, token);
        var details = await ReadAsync<IncentiveDetail>(reader, token);
        if (await reader.NextResultAsync(token))
            throw new InvalidOperationException("Dashboard procedure returned unexpected extra results.");
        return new DashboardData
        {
            Totals = totals[0], StatusCards = statusCards, Quarters = quarters,
            Positions = positions, Rms = rms, IncentiveTypes = types, Details = details
        };
    }

    public async Task<IReadOnlyList<string>> GetAvailableQuartersAsync(
        DashboardRole role, CancellationToken token)
    {
        await using var connection = new SqlConnection(_settings.ConnectionString);
        await connection.OpenAsync(token);
        await using var command = new SqlCommand(AvailableQuartersSql, connection)
        {
            CommandTimeout = _settings.CommandTimeoutSeconds
        };
        command.Parameters.Add("@Role", SqlDbType.SmallInt).Value = (short)role;
        await using var reader = await command.ExecuteReaderAsync(token);
        List<string> quarters = [];
        while (await reader.ReadAsync(token))
            quarters.Add(Convert.ToString(reader.GetValue(0), CultureInfo.InvariantCulture)!.Trim());
        return quarters;
    }

    private static void AddText(SqlCommand command, string name, int size, string? value) =>
        command.Parameters.Add(name, SqlDbType.VarChar, size).Value = (object?)value ?? DBNull.Value;

    private static async Task NextAsync(SqlDataReader reader, CancellationToken token)
    {
        if (!await reader.NextResultAsync(token))
            throw new InvalidOperationException("Dashboard procedure returned fewer than seven results.");
    }

    // Strict name mapping makes a changed SQL result contract fail visibly instead of
    // silently returning zeros. Inherited monetary properties are included.
    private static async Task<List<T>> ReadAsync<T>(SqlDataReader reader, CancellationToken token) where T : new()
    {
        var columns = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite)
            .Select(p => (Property: p, Ordinal: reader.GetOrdinal(p.Name))).ToArray();
        List<T> rows = [];
        while (await reader.ReadAsync(token))
        {
            var row = new T();
            foreach (var (property, ordinal) in columns)
            {
                if (await reader.IsDBNullAsync(ordinal, token))
                {
                    if (property.PropertyType.IsValueType && Nullable.GetUnderlyingType(property.PropertyType) is null)
                        throw new InvalidOperationException($"Unexpected NULL in dashboard column {property.Name}.");
                    property.SetValue(row, null);
                    continue;
                }
                var target = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                var value = Convert.ChangeType(reader.GetValue(ordinal), target, CultureInfo.InvariantCulture);
                property.SetValue(row, value);
            }
            rows.Add(row);
        }
        return rows;
    }

    private const string AvailableQuartersSql = """
        WITH SourceRows AS
        (
            SELECT IncQtr, Incentive, ISNULL(Status,0) AS StatusCode FROM dbo.EqRmBrkIncSumm
            UNION ALL
            SELECT IncQtr, Incentive, ISNULL(Status,0) FROM dbo.EqRmNwSelfClntRevSumm
            UNION ALL
            SELECT IncQtr, Incentive, ISNULL(Status,0) FROM dbo.EqMnExisClntIncRevSumm
            UNION ALL
            SELECT IncQtr, Incentive, ISNULL(Status,0) FROM dbo.EqReactIncentiveSumm
        )
        SELECT DISTINCT IncQtr AS Quarter
        FROM SourceRows
        WHERE Incentive > 0
          AND LEN(IncQtr) = 6
          AND IncQtr COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9]%'
          AND TRY_CONVERT(DATE, IncQtr + '01',112) IS NOT NULL
          AND ((@Role=1 AND StatusCode IN (0,1,3))
            OR (@Role=2 AND StatusCode IN (1,5))
            OR (@Role=3 AND StatusCode=6)
            OR (@Role=4 AND StatusCode IN (2,7))
            OR (@Role=5 AND StatusCode=4))
        ORDER BY IncQtr;
        """;
}
