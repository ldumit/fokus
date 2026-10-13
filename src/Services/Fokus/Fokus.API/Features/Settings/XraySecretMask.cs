namespace Fokus.API.Features.Settings;

public static class XraySecretMask
{
    public const string Value = "****";

    public static string Apply(string? secret) =>
        string.IsNullOrEmpty(secret) ? string.Empty : Value;

    public static bool IsKeepExisting(string? submitted) =>
        string.IsNullOrEmpty(submitted) || submitted == Value;
}
