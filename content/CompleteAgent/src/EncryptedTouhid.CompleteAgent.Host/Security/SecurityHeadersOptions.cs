using System.ComponentModel.DataAnnotations;

namespace EncryptedTouhid.CompleteAgent.Host.Security;

internal sealed class SecurityHeadersOptions
{
    public const string SectionName = "SecurityHeaders";

    public bool Enabled { get; init; } = true;

    [Range(0, 63072000)]
    public int HstsMaxAgeSeconds { get; init; } = 31_536_000;

    public bool HstsIncludeSubdomains { get; init; } = true;

    public string ContentSecurityPolicy { get; init; } =
        "default-src 'self'; img-src 'self' data:; style-src 'self' 'unsafe-inline'; script-src 'self'; object-src 'none'; frame-ancestors 'none'; base-uri 'self'";

    public string ReferrerPolicy { get; init; } = "strict-origin-when-cross-origin";

    public string PermissionsPolicy { get; init; } = "camera=(), microphone=(), geolocation=()";
}
