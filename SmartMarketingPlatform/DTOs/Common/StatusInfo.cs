namespace SmartMarketingPlatform.DTOs.Common;

// How every status appears inside a response: id + code for the client logic, text for the UI.
public sealed record StatusInfo(int Id, string Code, string Text);
