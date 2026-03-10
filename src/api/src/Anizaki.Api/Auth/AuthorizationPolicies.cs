namespace Anizaki.Api.Auth;

public static class AuthorizationPolicies
{
    public const string AuthenticatedUser = "AuthenticatedUser";
    public const string SellerOrAdmin = "SellerOrAdmin";
    public const string AdminOnly = "AdminOnly";

    public const string UserRole = "user";
    public const string SellerRole = "seller";
    public const string AdminRole = "admin";
}
