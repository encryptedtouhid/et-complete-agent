using System.Text.RegularExpressions;

namespace ET.CompleteAgent.Infrastructure.Logging;

public static partial class PromptRedactor
{
    private const string Placeholder = "[redacted]";
    private const int MaxLoggedLength = 256;

    public static string Redact(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var withoutEmails = EmailPattern().Replace(value, Placeholder);
        var withoutTokens = BearerTokenPattern().Replace(withoutEmails, Placeholder);
        var withoutCards = CreditCardPattern().Replace(withoutTokens, Placeholder);

        return withoutCards.Length > MaxLoggedLength
            ? withoutCards[..MaxLoggedLength] + "…"
            : withoutCards;
    }

    [GeneratedRegex(@"[\w.+\-]+@[\w\-]+\.[\w.\-]+", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 500)]
    private static partial Regex EmailPattern();

    [GeneratedRegex(@"\b(sk-[A-Za-z0-9]{20,}|Bearer\s+[A-Za-z0-9\-._~+/]+=*)", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 500)]
    private static partial Regex BearerTokenPattern();

    [GeneratedRegex(@"\b(?:\d[ -]*?){13,19}\b", RegexOptions.None, matchTimeoutMilliseconds: 500)]
    private static partial Regex CreditCardPattern();
}
