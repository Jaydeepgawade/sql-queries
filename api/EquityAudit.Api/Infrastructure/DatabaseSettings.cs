namespace EquityAudit.Api.Infrastructure;

public sealed class DatabaseSettings
{
    public string ConnectionString { get; set; } = "";
    public int CommandTimeoutSeconds { get; set; } = 60;
}
