using System.ComponentModel;

namespace EncryptedTouhid.CompleteAgent.Application.Tools;

public sealed class GetCurrentTimeTool
{
    private readonly TimeProvider _timeProvider;

    public GetCurrentTimeTool(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    [Description("Returns the current UTC time in ISO 8601 format. Use when the user asks what time it is.")]
    public string GetCurrentTimeUtc() =>
        _timeProvider.GetUtcNow().UtcDateTime.ToString("O", System.Globalization.CultureInfo.InvariantCulture);
}
