namespace Anizaki.Api.Auth;

public static class AuthenticationDefaults
{
    public const string Scheme = "AnizakiBearer";

    public const string UserIdHeader = "X-Anizaki-User-Id";
    public const string EmailHeader = "X-Anizaki-User-Email";
    public const string RoleHeader = "X-Anizaki-User-Role";
}
