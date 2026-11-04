namespace SmartMarketingPlatform.Enums;

// Stored as text ("Owner", "Admin", ...). Validation happens in the application.
public enum MembershipRole
{
    Owner,
    Admin,
    Editor,
    Reviewer,
    Viewer
}