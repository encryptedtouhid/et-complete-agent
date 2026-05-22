using System.Text.RegularExpressions;

namespace EncryptedTouhid.CompleteAgent.Application.Guardrails;

public static partial class OutputGuardrail
{
    private const string Placeholder = "[redacted]";

    public static string Scrub(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        var withoutEmails = EmailPattern().Replace(text, Placeholder);
        var withoutTokens = SecretPattern().Replace(withoutEmails, Placeholder);
        var withoutPhone = PhonePattern().Replace(withoutTokens, Placeholder);
        return withoutPhone;
    }

    [GeneratedRegex(@"[\w.+\-]+@[\w\-]+\.[\w.\-]+", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 500)]
    private static partial Regex EmailPattern();

    [GeneratedRegex(@"\b(sk-[A-Za-z0-9]{20,}|AKIA[0-9A-Z]{16}|ghp_[A-Za-z0-9]{36,})\b", RegexOptions.None, matchTimeoutMilliseconds: 500)]
    private static partial Regex SecretPattern();

    [GeneratedRegex(@"\+?\d[\d\s\-().]{9,}\d", RegexOptions.None, matchTimeoutMilliseconds: 500)]
    private static partial Regex PhonePattern();
}
