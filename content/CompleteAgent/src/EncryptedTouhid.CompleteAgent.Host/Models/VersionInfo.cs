namespace EncryptedTouhid.CompleteAgent.Host.Models;

internal sealed record VersionInfo(
    string Version,
    string InformationalVersion,
    string? CommitSha,
    bool? IsDirty,
    string Environment);
