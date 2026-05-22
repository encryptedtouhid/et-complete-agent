using System.Security.Cryptography;
using System.Text;
using EncryptedTouhid.CompleteAgent.Host.Authentication;

namespace EncryptedTouhid.CompleteAgent.Host.Endpoints;

internal static class SubjectScoping
{
    public static string ResolveSubject(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var apiKey = context.Request.Headers[ApiKeyOptions.HeaderName].ToString();
        if (!string.IsNullOrEmpty(apiKey))
        {
            return HashOrdinal("k:", apiKey);
        }

        var userName = context.User.Identity?.Name;
        if (!string.IsNullOrEmpty(userName))
        {
            return HashOrdinal("u:", userName);
        }

        return "anonymous";
    }

    public static string? ScopeConversationId(string subject, string? conversationId) =>
        string.IsNullOrWhiteSpace(conversationId)
            ? null
            : $"{subject}:{conversationId}";

    private static string HashOrdinal(string prefix, string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return prefix + Convert.ToHexStringLower(bytes.AsSpan(0, 8));
    }
}
