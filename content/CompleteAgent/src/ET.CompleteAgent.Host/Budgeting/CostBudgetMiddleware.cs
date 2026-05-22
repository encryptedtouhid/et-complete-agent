using ET.CompleteAgent.Host.Authentication;
using Microsoft.Extensions.Options;

namespace ET.CompleteAgent.Host.Budgeting;

internal sealed class CostBudgetMiddleware : IMiddleware
{
    private readonly ITokenUsageTracker _tracker;
    private readonly CostBudgetOptions _options;
    private readonly TimeProvider _timeProvider;

    public CostBudgetMiddleware(
        ITokenUsageTracker tracker,
        IOptions<CostBudgetOptions> options,
        TimeProvider timeProvider)
    {
        _tracker = tracker ?? throw new ArgumentNullException(nameof(tracker));
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        if (!_options.Enabled || !context.Request.Path.StartsWithSegments("/agent", StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        var subject = context.Request.Headers[ApiKeyOptions.HeaderName].ToString();
        if (string.IsNullOrEmpty(subject))
        {
            subject = context.User.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }

        var today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
        var used = _tracker.GetUsage(subject, today);

        if (used >= _options.DailyTokenLimitPerKey)
        {
            context.Response.StatusCode = StatusCodes.Status402PaymentRequired;
            await context.Response.WriteAsync(
                $"Daily token budget exceeded ({used} / {_options.DailyTokenLimitPerKey}).",
                context.RequestAborted);
            return;
        }

        await next(context);
    }
}
