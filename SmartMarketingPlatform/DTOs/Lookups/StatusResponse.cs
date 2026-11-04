namespace SmartMarketingPlatform.DTOs.Lookups;

// Text is translated to the request language; Code is what the client sends back.
public sealed record StatusResponse(int Id, string Code, string TranslationKey, string Text);
