using ET.CompleteAgent.Application;
using ET.CompleteAgent.Host.Authentication;
using ET.CompleteAgent.Host.Budgeting;
using ET.CompleteAgent.Host.Endpoints;
using ET.CompleteAgent.Host.HealthChecks;
using ET.CompleteAgent.Host.Models;
using ET.CompleteAgent.Host.RateLimiting;
using ET.CompleteAgent.Host.Telemetry;
using ET.CompleteAgent.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddEnvironmentVariables(prefix: "COMPLETEAGENT_")
    .AddUserSecrets<Program>(optional: true);

var promptsRoot = Path.Combine(builder.Environment.ContentRootPath, "Prompts");

builder.Services
    .AddInfrastructure(builder.Configuration)
    .AddApplication(builder.Configuration, promptsRoot)
    .AddAgentAuthentication(builder.Configuration)
    .AddAgentTelemetry(builder.Configuration)
    .AddAgentRateLimiting(builder.Configuration)
    .AddCostBudgeting(builder.Configuration);

builder.Services.AddSingleton<AgentOptionsHealthCheck>();
builder.Services
    .AddHealthChecks()
    .AddCheck<AgentOptionsHealthCheck>("agent-options", tags: ["ready"]);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AgentJsonContext.Default);
});

builder.Services.AddOpenApi();

var app = builder.Build();

app.UseApiKeyAuthentication();
app.UseAuthentication();
app.UseAuthorization();
app.UseAgentRateLimiting(builder.Configuration);
app.UseCostBudgeting();

app.MapOpenApi();
app.MapHealthChecks("/healthz");
app.MapHealthChecks("/readyz", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.MapAgentEndpoints();

await app.RunAsync();
