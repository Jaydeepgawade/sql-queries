using System.Text;
using EquityAudit.Api.Infrastructure;
using EquityAudit.Api.Interfaces;
using EquityAudit.Api.Models;
using EquityAudit.Api.Repositories;
using EquityAudit.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddOptions<DatabaseSettings>()
    .Configure(settings =>
    {
        settings.ConnectionString = builder.Configuration.GetConnectionString("AuditDb") ?? "";
        settings.CommandTimeoutSeconds = builder.Configuration.GetValue("Database:CommandTimeoutSeconds", 60);
    })
    .Validate(settings => !string.IsNullOrWhiteSpace(settings.ConnectionString), "Set ConnectionStrings:AuditDb using user secrets or environment variables.")
    .Validate(settings => settings.CommandTimeoutSeconds is >= 1 and <= 300, "Database timeout must be 1-300 seconds.")
    .ValidateOnStart();
builder.Services.AddScoped<IDashboardRepository, DashboardRepository>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

var issuer = builder.Configuration["Jwt:Issuer"];
var audience = builder.Configuration["Jwt:Audience"];
var key = builder.Configuration["Jwt:SigningKey"];
if (string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(audience) ||
    string.IsNullOrWhiteSpace(key) || Encoding.UTF8.GetByteCount(key) < 32)
    throw new InvalidOperationException("Set Jwt:Issuer, Jwt:Audience and a signing key of at least 32 UTF-8 bytes using secrets/environment variables.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true, ValidIssuer = issuer,
            ValidateAudience = true, ValidAudience = audience,
            ValidateLifetime = true, RequireExpirationTime = true,
            ValidateIssuerSigningKey = true, RequireSignedTokens = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });
builder.Services.AddAuthorization(options => options.AddPolicy("DashboardReader", policy =>
    policy.RequireAuthenticatedUser().RequireAssertion(context =>
    {
        var roles = context.User.FindAll("dashboard_role").ToArray();
        return roles.Length == 1 && short.TryParse(roles[0].Value, out var role) &&
               Enum.IsDefined(typeof(DashboardRole), (DashboardRole)role);
    })));

var app = builder.Build();
app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();
app.Run();
