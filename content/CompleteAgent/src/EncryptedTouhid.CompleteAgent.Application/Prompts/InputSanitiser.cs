namespace EncryptedTouhid.CompleteAgent.Application.Prompts;

public static class InputSanitiser
{
    private const string OpenDelimiter = "<user_input>";
    private const string CloseDelimiter = "</user_input>";
    private const int MaxLength = 8_000;

    public static string Wrap(string userInput)
    {
        ArgumentNullException.ThrowIfNull(userInput);

        var trimmed = userInput.Length > MaxLength
            ? userInput[..MaxLength]
            : userInput;

        var escaped = trimmed
            .Replace(OpenDelimiter, "&lt;user_input&gt;", StringComparison.OrdinalIgnoreCase)
            .Replace(CloseDelimiter, "&lt;/user_input&gt;", StringComparison.OrdinalIgnoreCase);

        return $"{OpenDelimiter}\n{escaped}\n{CloseDelimiter}";
    }
}
