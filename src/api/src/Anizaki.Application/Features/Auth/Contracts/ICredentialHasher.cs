namespace Anizaki.Application.Features.Auth.Contracts;

public interface ICredentialHasher
{
    string HashPassword(string plainPassword);

    bool VerifyPassword(string plainPassword, string passwordHash);
}

