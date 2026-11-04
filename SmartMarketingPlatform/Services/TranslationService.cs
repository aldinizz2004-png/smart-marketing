using System.Text.Json;

namespace SmartMarketingPlatform.Services;

// Reads the static translation files Resources/en.json and Resources/ar.json.
// The database stores only TranslationKeys (e.g. "status.campaign.active");
// this service turns a key into the text the user sees.
public class TranslationService
{
    public const string DefaultLanguage = "en";

    private readonly Dictionary<string, Dictionary<string, string>> _texts = new(StringComparer.OrdinalIgnoreCase);

    public TranslationService()
    {
        var folder = Path.Combine(AppContext.BaseDirectory, "Resources");
        foreach (var file in Directory.GetFiles(folder, "*.json"))
        {
            var language = Path.GetFileNameWithoutExtension(file);
            _texts[language] = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(file))
                               ?? new Dictionary<string, string>();
        }
    }

    // Falls back to English, then to the key itself, so a missing text never breaks the UI.
    public string Translate(string key, string? language)
    {
        if (language is not null && _texts.TryGetValue(language, out var texts) && texts.TryGetValue(key, out var text))
            return text;

        if (_texts.TryGetValue(DefaultLanguage, out var english) && english.TryGetValue(key, out var englishText))
            return englishText;

        return key;
    }
}
