using System.Globalization;
using Microsoft.Extensions.Options;

namespace ET.CompleteAgent.Host.Security;

internal sealed class SecurityHeadersMiddleware : IMiddleware
{
    private readonly SecurityHeadersOptions _options;

    public SecurityHeadersMiddleware(IOptions<SecurityHeadersOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
    }

    public Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        if (!_options.Enabled)
        {
            return next(context);
        }

        var headers = context.Response.Headers;
        headers.Append("X-Content-Type-Options", "nosniff");
        headers.Append("X-Frame-Options", "DENY");
        headers.Append("Referrer-Policy", _options.ReferrerPolicy);
        headers.Append("Permissions-Policy", _options.PermissionsPolicy);
        headers.Append("Content-Security-Policy", _options.ContentSecurityPolicy);

        if (context.Request.IsHttps)
        {
            var includeSub = _options.HstsIncludeSubdomains ? "; includeSubDomains" : string.Empty;
            headers.Append(
                "Strict-Transport-Security",
                $"max-age={_options.HstsMaxAgeSeconds.ToString(CultureInfo.InvariantCulture)}{includeSub}");
        }

        return next(context);
    }
}
