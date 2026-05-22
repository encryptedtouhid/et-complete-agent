using ET.CompleteAgent.Application;
using ET.CompleteAgent.Host.Authentication;
using ET.CompleteAgent.Host.Budgeting;
using ET.CompleteAgent.Host.Endpoints;
using ET.CompleteAgent.Host.Exceptions;
using ET.CompleteAgent.Host.HealthChecks;
using ET.CompleteAgent.Host.Idempotency;
using ET.CompleteAgent.Host.Models;
using ET.CompleteAgent.Host.OpenApi;
using ET.CompleteAgent.Host.RateLimiting;
using ET.CompleteAgent.Host.Security;
using ET.CompleteAgent.Host.Telemetry;
using ET.CompleteAgent.Infrastructure;
using Scalar.AspNetCore;

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
    .AddCostBudgeting(builder.Configuration)
    .AddAgentSecurity(builder.Configuration)
    .AddIdempotency(builder.Configuration);

builder.Services.AddSingleton<AgentOptionsHealthCheck>();
var healthChecks = builder.Services
    .AddHealthChecks()
    .AddCheck<AgentOptionsHealthCheck>("agent-options", tags: ["ready"]);

var rateLimitOpts = builder.Configuration.GetSection(ET.CompleteAgent.Host.RateLimiting.RateLimitOptions.SectionName)
    .Get<ET.CompleteAgent.Host.RateLimiting.RateLimitOptions>();
if (rateLimitOpts?.Store == ET.CompleteAgent.Host.RateLimiting.RateLimitStoreKind.Redis)
{
    builder.Services.AddSingleton<RedisHealthCheck>();
    healthChecks.AddCheck<RedisHealthCheck>("redis", tags: ["ready"]);
}

var persistenceOpts = builder.Configuration.GetSection(ET.CompleteAgent.Infrastructure.Configuration.PersistenceOptions.SectionName)
    .Get<ET.CompleteAgent.Infrastructure.Configuration.PersistenceOptions>();
if (persistenceOpts?.ConversationStore == ET.CompleteAgent.Infrastructure.Configuration.ConversationStoreKind.Sqlite)
{
    builder.Services.AddSingleton<SqliteHealthCheck>();
    healthChecks.AddCheck<SqliteHealthCheck>("sqlite", tags: ["ready"]);
}

var retrievalOpts = builder.Configuration.GetSection(ET.CompleteAgent.Infrastructure.Configuration.RetrievalOptions.SectionName)
    .Get<ET.CompleteAgent.Infrastructure.Configuration.RetrievalOptions>();
if (retrievalOpts?.VectorStore == ET.CompleteAgent.Infrastructure.Configuration.VectorStoreKind.Qdrant)
{
    builder.Services.AddSingleton<QdrantHealthCheck>();
    healthChecks.AddCheck<QdrantHealthCheck>("qdrant", tags: ["ready"]);
}

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AgentJsonContext.Default);
});

builder.Services.AddOpenApi();
builder.Services.AddExceptionHandler<AgentExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();
app.UseAgentSecurity();
app.UseCors(CorsOptions.PolicyName);
app.UseApiKeyAuthentication();
app.UseAuthentication();
app.UseAuthorization();
app.UseAgentRateLimiting(builder.Configuration);
app.UseCostBudgeting();
app.UseIdempotency();

app.MapOpenApi();
app.MapScalarApiReference("/scalar", options =>
{
    options.Title = "Complete Agent — API Reference";
    options.Theme = ScalarTheme.None;
    options.DarkMode = true;
    options.CustomCss = GitHubScalarTheme.Css;
    options.DefaultHttpClient = new(ScalarTarget.CSharp, ScalarClient.HttpClient);
});

app.MapHealthChecks("/healthz");
app.MapHealthChecks("/readyz", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.MapAgentEndpoints();
app.MapVersionEndpoint();

await app.RunAsync();
