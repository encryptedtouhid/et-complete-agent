using Microsoft.Extensions.Logging;

namespace EncryptedTouhid.CompleteAgent.Application.Resilience;

public sealed partial class RetryPolicy
{
    private readonly int _maxAttempts;
    private readonly TimeSpan _baseBackoff;
    private readonly ILogger<RetryPolicy> _logger;

    public RetryPolicy(int maxAttempts, TimeSpan baseBackoff, ILogger<RetryPolicy> logger)
    {
        if (maxAttempts < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxAttempts), "Must be >= 1");
        }
        _maxAttempts = maxAttempts;
        _baseBackoff = baseBackoff;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(action);

        var attempt = 0;
        while (true)
        {
            try
            {
                return await action(cancellationToken);
            }
            catch (HttpRequestException ex) when (attempt < _maxAttempts - 1)
            {
                attempt++;
                var delay = TimeSpan.FromTicks(_baseBackoff.Ticks * (1L << (attempt - 1)));
                LogRetry(attempt, _maxAttempts, delay.TotalSeconds, ex.Message);
                await Task.Delay(delay, cancellationToken);
            }
            catch (TimeoutException ex) when (attempt < _maxAttempts - 1)
            {
                attempt++;
                var delay = TimeSpan.FromTicks(_baseBackoff.Ticks * (1L << (attempt - 1)));
                LogRetry(attempt, _maxAttempts, delay.TotalSeconds, ex.Message);
                await Task.Delay(delay, cancellationToken);
            }
        }
    }

    [LoggerMessage(LogLevel.Warning, "Transient failure on attempt {Attempt}/{Max}, retrying after {DelaySec:F1}s: {Reason}")]
    private partial void LogRetry(int attempt, int max, double delaySec, string reason);
}
