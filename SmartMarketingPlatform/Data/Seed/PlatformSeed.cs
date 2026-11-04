using SmartMarketingPlatform.Entities;

namespace SmartMarketingPlatform.Data.Seed;

public static class PlatformSeed
{
    public static readonly Platform[] Platforms =
    {
        new() { Code = "facebook",  DisplayNameKey = "platform.facebook",  SupportsPublishing = true,  SupportsMessaging = true,  SupportsCompetitorTracking = true },
        new() { Code = "instagram", DisplayNameKey = "platform.instagram", SupportsPublishing = true,  SupportsMessaging = true,  SupportsCompetitorTracking = true },
        new() { Code = "whatsapp",  DisplayNameKey = "platform.whatsapp",  SupportsPublishing = true,  SupportsMessaging = true,  SupportsCompetitorTracking = false },
        new() { Code = "tiktok",    DisplayNameKey = "platform.tiktok",    SupportsPublishing = true,  SupportsMessaging = false, SupportsCompetitorTracking = true },
        new() { Code = "x",         DisplayNameKey = "platform.x",         SupportsPublishing = true,  SupportsMessaging = false, SupportsCompetitorTracking = true },
        new() { Code = "snapchat",  DisplayNameKey = "platform.snapchat",  SupportsPublishing = true,  SupportsMessaging = false, SupportsCompetitorTracking = true },
        new() { Code = "youtube",   DisplayNameKey = "platform.youtube",   SupportsPublishing = true,  SupportsMessaging = false, SupportsCompetitorTracking = true },
        new() { Code = "linkedin",  DisplayNameKey = "platform.linkedin",  SupportsPublishing = true,  SupportsMessaging = false, SupportsCompetitorTracking = true },
        new() { Code = "website",   DisplayNameKey = "platform.website",   SupportsPublishing = false, SupportsMessaging = false, SupportsCompetitorTracking = true },
    };

    public static readonly PlatformDomain[] Domains =
    {
        new() { Domain = "facebook.com",  PlatformCode = "facebook" },
        new() { Domain = "fb.com",        PlatformCode = "facebook" },
        new() { Domain = "fb.me",         PlatformCode = "facebook" },
        new() { Domain = "instagram.com", PlatformCode = "instagram" },
        new() { Domain = "instagr.am",    PlatformCode = "instagram" },
        new() { Domain = "wa.me",         PlatformCode = "whatsapp" },
        new() { Domain = "whatsapp.com",  PlatformCode = "whatsapp" },
        new() { Domain = "tiktok.com",    PlatformCode = "tiktok" },
        new() { Domain = "x.com",         PlatformCode = "x" },
        new() { Domain = "twitter.com",   PlatformCode = "x" },
        new() { Domain = "snapchat.com",  PlatformCode = "snapchat" },
        new() { Domain = "youtube.com",   PlatformCode = "youtube" },
        new() { Domain = "youtu.be",      PlatformCode = "youtube" },
        new() { Domain = "linkedin.com",  PlatformCode = "linkedin" },
    };
}