namespace SmartMarketingPlatform.Entities;

// Maps a host (instagram.com, youtu.be ...) to a platform, used to detect pasted competitor URLs.
public class PlatformDomain
{
    public string Domain { get; set; } = string.Empty;          // primary key
    public string PlatformCode { get; set; } = string.Empty;    // FK -> Platform

    public Platform Platform { get; set; } = null!;
}