namespace EncryptedTouhid.CompleteAgent.Host.OpenApi;

/// <summary>
/// GitHub Primer-inspired theme for Scalar. Mirrors the colour tokens that
/// GitHub uses on github.com — light mode and dark mode driven by the user's
/// OS / browser preference via the standard <c>prefers-color-scheme</c> media query.
/// </summary>
internal static class GitHubScalarTheme
{
    public const string Css = """
        :root {
          --scalar-font: -apple-system, BlinkMacSystemFont, "Segoe UI", "Noto Sans", Helvetica, Arial, sans-serif, "Apple Color Emoji", "Segoe UI Emoji";
          --scalar-font-code: ui-monospace, SFMono-Regular, "SF Mono", Menlo, Consolas, "Liberation Mono", monospace;
        }

        /* Light mode — GitHub Primer light palette */
        .light-mode {
          --scalar-color-1: #1f2328;
          --scalar-color-2: #59636e;
          --scalar-color-3: #6e7781;
          --scalar-color-accent: #0969da;

          --scalar-background-1: #ffffff;
          --scalar-background-2: #f6f8fa;
          --scalar-background-3: #eaeef2;
          --scalar-background-accent: #ddf4ff;

          --scalar-border-color: #d1d9e0;

          --scalar-color-green: #1a7f37;
          --scalar-color-red:   #d1242f;
          --scalar-color-yellow:#9a6700;
          --scalar-color-blue:  #0969da;
          --scalar-color-orange:#bc4c00;
          --scalar-color-purple:#8250df;

          --scalar-button-1: #1f2328;
          --scalar-button-1-color: #ffffff;
          --scalar-button-1-hover: #2c333a;
        }

        /* Dark mode — GitHub Primer dark palette */
        .dark-mode {
          --scalar-color-1: #e6edf3;
          --scalar-color-2: #7d8590;
          --scalar-color-3: #6e7681;
          --scalar-color-accent: #2f81f7;

          --scalar-background-1: #0d1117;
          --scalar-background-2: #161b22;
          --scalar-background-3: #21262d;
          --scalar-background-accent: #1f6feb1a;

          --scalar-border-color: #30363d;

          --scalar-color-green: #3fb950;
          --scalar-color-red:   #f85149;
          --scalar-color-yellow:#d29922;
          --scalar-color-blue:  #2f81f7;
          --scalar-color-orange:#db6d28;
          --scalar-color-purple:#a371f7;

          --scalar-button-1: #e6edf3;
          --scalar-button-1-color: #0d1117;
          --scalar-button-1-hover: #f0f6fc;
        }

        /* Sidebar uses the canvas-subtle background like github.com */
        .scalar-app .sidebar { background: var(--scalar-background-2); }

        /* Code blocks use GitHub-style background and mono font */
        .scalar-card-simple pre,
        .scalar-app code {
          font-family: var(--scalar-font-code);
        }

        /* HTTP method badges in Primer accent colours */
        .scalar-card-simple .endpoint-method[data-method="get"]    { color: var(--scalar-color-blue); }
        .scalar-card-simple .endpoint-method[data-method="post"]   { color: var(--scalar-color-green); }
        .scalar-card-simple .endpoint-method[data-method="put"]    { color: var(--scalar-color-yellow); }
        .scalar-card-simple .endpoint-method[data-method="delete"] { color: var(--scalar-color-red); }
        .scalar-card-simple .endpoint-method[data-method="patch"]  { color: var(--scalar-color-purple); }
        """;
}
