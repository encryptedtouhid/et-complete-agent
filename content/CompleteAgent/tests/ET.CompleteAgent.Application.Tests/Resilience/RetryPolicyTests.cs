using ET.CompleteAgent.Application.Resilience;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ET.CompleteAgent.Application.Tests.Resilience;

public sealed class RetryPolicyTests
{
    [Fact]
    public async Task ExecuteAsync_ReturnsValue_WhenActionSucceedsFirstTime()
    {
        var policy = new RetryPolicy(3, TimeSpan.FromMilliseconds(1), NullLogger<RetryPolicy>.Instance);

        var result = await policy.ExecuteAsync(_ => Task.FromResult(42), CancellationToken.None);

        Assert.Equal(42, result);
    }

    [Fact]
    public async Task ExecuteAsync_RetriesAndSucceeds_OnTransientHttpException()
    {
        var policy = new RetryPolicy(3, TimeSpan.FromMilliseconds(1), NullLogger<RetryPolicy>.Instance);
        var attempts = 0;

        var result = await policy.ExecuteAsync(_ =>
        {
            attempts++;
            if (attempts < 2)
            {
                throw new HttpRequestException("transient");
            }
            return Task.FromResult("ok");
        }, CancellationToken.None);

        Assert.Equal("ok", result);
        Assert.Equal(2, attempts);
    }

    [Fact]
    public async Task ExecuteAsync_Throws_AfterMaxAttemptsExceeded()
    {
        var policy = new RetryPolicy(2, TimeSpan.FromMilliseconds(1), NullLogger<RetryPolicy>.Instance);

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            policy.ExecuteAsync<int>(_ => throw new HttpRequestException("always fails"), CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_DoesNotRetry_OnNonTransientException()
    {
        var policy = new RetryPolicy(3, TimeSpan.FromMilliseconds(1), NullLogger<RetryPolicy>.Instance);
        var attempts = 0;

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            policy.ExecuteAsync<int>(_ =>
            {
                attempts++;
                throw new InvalidOperationException("config error");
            }, CancellationToken.None));

        Assert.Equal(1, attempts);
    }

    [Fact]
    public void Construct_Throws_WhenMaxAttemptsLessThanOne()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new RetryPolicy(0, TimeSpan.FromMilliseconds(1), NullLogger<RetryPolicy>.Instance));
    }
}
