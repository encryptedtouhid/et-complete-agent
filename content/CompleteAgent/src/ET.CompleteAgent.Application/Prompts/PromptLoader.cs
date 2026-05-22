using System.Text;
using ET.CompleteAgent.Domain.Prompts;

namespace ET.CompleteAgent.Application.Prompts;

public interface IPromptLoader
{
    Task<string> LoadSystemPromptAsync(PromptVersion version, CancellationToken cancellationToken = default);
}

public sealed class FileSystemPromptLoader : IPromptLoader
{
    private readonly string _rootPath;

    public FileSystemPromptLoader(string rootPath)
    {
        _rootPath = rootPath ?? throw new ArgumentNullException(nameof(rootPath));
    }

    public async Task<string> LoadSystemPromptAsync(PromptVersion version, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(version);

        var folder = Path.Combine(_rootPath, version.Folder);
        if (!Directory.Exists(folder))
        {
            throw new DirectoryNotFoundException($"Prompt version folder not found: {folder}");
        }

        var builder = new StringBuilder();
        foreach (var part in new[] { "system.md", "guardrails.md", "examples.md" })
        {
            var path = Path.Combine(folder, part);
            if (!File.Exists(path))
            {
                continue;
            }

            var contents = await File.ReadAllTextAsync(path, cancellationToken);
            builder.AppendLine(contents);
            builder.AppendLine();
        }

        return builder.ToString().Trim();
    }
}
