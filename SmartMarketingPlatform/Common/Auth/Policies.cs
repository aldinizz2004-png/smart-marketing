namespace SmartMarketingPlatform.Common.Auth;

// Authorization policies, built from the claims of the access token.
public static class Policies
{
    public const string TenantOwner = "TenantOwner";               // Owner only (payments, subscriptions)
    public const string TenantAdmin = "TenantAdmin";               // Owner or Admin (settings, members, businesses)
    public const string CanEdit = "CanEdit";                       // Owner, Admin or Editor (business content)
    public const string CanReview = "CanReview";                   // Owner, Admin or Reviewer (approve content)
    public const string ActiveSubscription = "ActiveSubscription"; // the tenant has a paid period running now
}
