namespace SmartMarketingPlatform.Common.Options;

// Where uploaded files and secrets are kept in development.
// Relative paths are resolved against the content root. App_Data is git-ignored.
public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    public string MediaRoot { get; init; } = "App_Data/media";
    public string SecretsRoot { get; init; } = "App_Data/secrets";
}
