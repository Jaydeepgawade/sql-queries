# .NET 8 dashboard REST API

## Flow

Controller → IDashboardService / DashboardService → IDashboardRepository / DashboardRepository → SQL Server SP → typed response.

The controller accepts quarter, RM, position and status filters. It gets the role from a validated JWT claim called `dashboard_role`, never a role query parameter. The service validates keys, calls the repository, and distinguishes missing-quarter data from unmatched filters. The repository uses parameterized ADO.NET commands and reads the seven SP results asynchronously.

## Visual Studio setup

1. Open `EquityAuditDashboard.sln` in Visual Studio with .NET 8 installed.
2. Execute the bundled `Fab_Ims_EquityDashboard.sql` in your TEST database. Re-run this version: it accepts any valid six-digit YYYYMM key stored in your database, without a hardcoded list of quarters.
3. Right-click `EquityAudit.Api` → Manage User Secrets. Configure this locally (never commit real secrets):

```json
{
  "ConnectionStrings:AuditDb": "Server=YOUR_SERVER;Database=YOUR_TEST_DATABASE;Integrated Security=True;Encrypt=True;TrustServerCertificate=False;",
  "Jwt:Issuer": "YOUR_EXISTING_TOKEN_ISSUER",
  "Jwt:Audience": "YOUR_EXISTING_API_AUDIENCE",
  "Jwt:SigningKey": "YOUR_EXISTING_HS256_SECRET_AT_LEAST_32_BYTES"
}
```

4. Use your existing authentication service to issue a signed HS256 JWT with matching issuer/audience, expiry, and exactly one `dashboard_role` claim containing `1`, `2`, `3`, `4`, or `5`.
5. Set the API as startup project and run. Use the `.http` file in Visual Studio to test.

For local SQL Server using a self-signed certificate, use `TrustServerCertificate=True` only in local user secrets after verifying the server. Use a trusted SQL certificate for deployed environments. For asymmetric/OIDC tokens, replace the HS256 configuration with your existing authority/public-key validation; do not copy an unrelated signing key.

Production configuration uses environment variables such as `ConnectionStrings__AuditDb`, `Jwt__Issuer`, `Jwt__Audience` and `Jwt__SigningKey`. The app refuses to start with missing SQL configuration or missing JWT settings. It does not implement a new login system.

## Endpoints

All dashboard endpoints require JWT authentication and a valid role claim.

| Method | Path | Content |
|---|---|---|
| GET | `/api/equity-dashboard` | All seven dashboard sections |
| GET | `/api/equity-dashboard/totals` | Amount cards |
| GET | `/api/equity-dashboard/statuses` | Eight workflow cards |
| GET | `/api/equity-dashboard/quarters` | Quarter chart |
| GET | `/api/equity-dashboard/positions` | Position chart |
| GET | `/api/equity-dashboard/rms` | RM chart |
| GET | `/api/equity-dashboard/incentive-types` | B/S/E/R chart |
| GET | `/api/equity-dashboard/details` | Detail grid |
| GET | `/api/equity-dashboard/quarters/available` | Role-visible quarter keys for dropdown |

Filters: `quarterFrom`, `quarterTo`, `rmCode`, `rmFrom`, `rmTo`, `positionName`, `status`. Omit optional filters to select all role-visible values. For a single quarter set both bounds to the same key. Arbitrary years/month keys are accepted if they have valid YYYYMM format; invalid values like 202613 return HTTP 400. Quarter ranges are inclusive. A partially populated range returns available records; it does not warn for every missing quarter within the range.

Each section endpoint currently calls the same seven-result SP and selects one section from its response. For a dashboard page call the combined endpoint once; calling all seven endpoints multiplies database work. These are read-only REST resources. No approval/rejection POST endpoint is added; the existing workflow remains separate.

## Empty-quarter popup

No matching data returns HTTP 200, not HTTP 500:

```json
{
  "hasData": false,
  "showPopup": true,
  "code": "QUARTER_DATA_NOT_FOUND",
  "message": "No dashboard data is available for quarter 202703 in your role's scope.",
  "data": { "totals": {}, "statusCards": [], "quarters": [], "positions": [], "rms": [], "incentiveTypes": [], "details": [] }
}
```

The example above shortens the nested amounts and cards for readability; the real response includes zero-valued totals and all eight zero-valued status cards from the SP.

If the quarter has role-visible positive-incentive records but an RM/position/status filter matches none, `code` is `FILTER_DATA_NOT_FOUND`. The quarter check ignores these user filters, while enforcing the same role statuses and positive-incentive rule. Consequently it describes reporting availability, not whether a raw table contains a row with zero incentive or another role's status.

With data, `hasData=true`, `showPopup=false`, `code=OK`. The UI checks metadata, clears stale results, and shows a native dialog for empty data. Copy `ui/DashboardPopup.cshtml` and `ui/dashboard-popup.js` into your MVC application. The example uses existing jQuery and your existing token/login flow; charts and a complete dashboard layout are not included. No data popups are separate from invalid filters, unauthenticated requests, forbidden requests and SQL failures.

For status cards, @PStatus is ignored by SQL. The response metadata still describes the selected dashboard filter as a whole, so all eight cards can be returned even when the selected status has zero rows. For card-only calls omit `status`.

## Existing session-based MVC project

This standalone API uses JWT. If your MVC app uses session/cookie authentication, call the API from a same-origin server-side controller/proxy using the established auth flow, or adapt the authentication scheme to your app. Do not hardcode role or expose signing keys to JavaScript. The popup helper expects a same-origin `/api/equity-dashboard` route. Configure an explicit CORS allowlist if your actual UI/API are hosted on separate origins; no permissive CORS policy is enabled here.

The supplied role policy only reproduces the original workflow role/status visibility. Add your existing branch, vertical and employee authorization to both dashboard and availability queries before applying them in a scoped multi-branch app. The source does not contain those relationships, so no guessed branch restriction is implemented.

## Checks

```text
dotnet restore EquityAuditDashboard.sln
dotnet build EquityAuditDashboard.sln --no-restore
dotnet run --project tests/EquityAudit.Checks
```

The executable checks exercise the real service with an in-memory repository dependency: missing quarters, unmatched filters, valid data, arbitrary YYYYMM keys, invalid months, reversed ranges, and invalid roles. They do not prove SQL mapping or HTTP authentication. The .http file and testing.sql provide database/authentication integration checks.

Neither the .NET SDK nor SQL Server was available in the authoring environment. Build, executable checks and database execution have NOT been run successfully. Verify in Visual Studio before integration.

## Contract notes

Amounts are decimals, workflow statuses remain numeric, nullable SQL text fields are nullable models, and record/pair counts use long. Net excludes FileAdjAmt exactly as in the supplied SP. NISM is a separate validity flag. Reflection-based name mapping fails on missing columns, malformed result count or unexpected numeric NULL, rather than silently filling default totals. HTTP responses do not reveal connection strings or internal SQL exception messages.

The detail grid is currently unpaged, matching the requested SP. Large result sets need a measured paging or export design before high-volume use. No cache or unrequested workflow mutation is included.
