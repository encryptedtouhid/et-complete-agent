using EncryptedTouhid.CompleteAgent.Application;
using EncryptedTouhid.CompleteAgent.Host.Authentication;
using EncryptedTouhid.CompleteAgent.Host.Budgeting;
using EncryptedTouhid.CompleteAgent.Host.Endpoints;
using EncryptedTouhid.CompleteAgent.Host.Exceptions;
using EncryptedTouhid.CompleteAgent.Host.HealthChecks;
using EncryptedTouhid.CompleteAgent.Host.Idempotency;
using EncryptedTouhid.CompleteAgent.Host.Models;
using EncryptedTouhid.CompleteAgent.Host.OpenApi;
using EncryptedTouhid.CompleteAgent.Host.RateLimiting;
using EncryptedTouhid.CompleteAgent.Host.Security;
using EncryptedTouhid.CompleteAgent.Host.Telemetry;
using EncryptedTouhid.CompleteAgent.Infrastructure;
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

var rateLimitOpts = builder.Configuration.GetSection(EncryptedTouhid.CompleteAgent.Host.RateLimiting.RateLimitOptions.SectionName)
    .Get<EncryptedTouhid.CompleteAgent.Host.RateLimiting.RateLimitOptions>();
if (rateLimitOpts?.Store == EncryptedTouhid.CompleteAgent.Host.RateLimiting.RateLimitStoreKind.Redis)
{
    builder.Services.AddSingleton<RedisHealthCheck>();
    healthChecks.AddCheck<RedisHealthCheck>("redis", tags: ["ready"]);
}

var persistenceOpts = builder.Configuration.GetSection(EncryptedTouhid.CompleteAgent.Infrastructure.Configuration.PersistenceOptions.SectionName)
    .Get<EncryptedTouhid.CompleteAgent.Infrastructure.Configuration.PersistenceOptions>();
switch (persistenceOpts?.ConversationStore)
{
    case EncryptedTouhid.CompleteAgent.Infrastructure.Configuration.ConversationStoreKind.Sqlite:
    case EncryptedTouhid.CompleteAgent.Infrastructure.Configuration.ConversationStoreKind.SqlServer:
    case EncryptedTouhid.CompleteAgent.Infrastructure.Configuration.ConversationStoreKind.AzureSql:
    case EncryptedTouhid.CompleteAgent.Infrastructure.Configuration.ConversationStoreKind.Postgres:
    case EncryptedTouhid.CompleteAgent.Infrastructure.Configuration.ConversationStoreKind.MySql:
        builder.Services.AddSingleton<RelationalDbHealthCheck>();
        healthChecks.AddCheck<RelationalDbHealthCheck>("relational-db", tags: ["ready"]);
        break;
    case EncryptedTouhid.CompleteAgent.Infrastructure.Configuration.ConversationStoreKind.Cosmos:
        builder.Services.AddSingleton<CosmosHealthCheck>();
        healthChecks.AddCheck<CosmosHealthCheck>("cosmos", tags: ["ready"]);
        break;
    case EncryptedTouhid.CompleteAgent.Infrastructure.Configuration.ConversationStoreKind.Mongo:
        builder.Services.AddSingleton<MongoHealthCheck>();
        healthChecks.AddCheck<MongoHealthCheck>("mongo", tags: ["ready"]);
        break;
}

var retrievalOpts = builder.Configuration.GetSection(EncryptedTouhid.CompleteAgent.Infrastructure.Configuration.RetrievalOptions.SectionName)
    .Get<EncryptedTouhid.CompleteAgent.Infrastructure.Configuration.RetrievalOptions>();
if (retrievalOpts?.VectorStore == EncryptedTouhid.CompleteAgent.Infrastructure.Configuration.VectorStoreKind.Qdrant)
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
var jwtEnabled = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()?.Enabled == true;
if (jwtEnabled)
{
    app.UseAuthentication();
}
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
