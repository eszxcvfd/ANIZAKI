using System.Security.Cryptography;
using System.Text;
using Anizaki.Application.Features.Auth.Contracts;

namespace Anizaki.Infrastructure.Auth;

public sealed class Sha256CredentialHasher : ICredentialHasher
{
    private const string CurrentFormatPrefix = "pbkdf2-sha256.v1";
    private const int CurrentIterations = 210_000;
    private const int SaltLengthInBytes = 16;
    private const int DerivedKeyLengthInBytes = 32;

    public string HashPassword(string plainPassword)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plainPassword);

        var salt = RandomNumberGenerator.GetBytes(SaltLengthInBytes);
        var derivedKey = Rfc2898DeriveBytes.Pbkdf2(
            plainPassword,
            salt,
            CurrentIterations,
            HashAlgorithmName.SHA256,
            DerivedKeyLengthInBytes);

        return $"{CurrentFormatPrefix}${CurrentIterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(derivedKey)}";
    }

    public bool VerifyPassword(string plainPassword, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(plainPassword) || string.IsNullOrWhiteSpace(passwordHash))
        {
            return false;
        }

        return passwordHash.StartsWith($"{CurrentFormatPrefix}$", StringComparison.Ordinal)
            ? VerifyCurrentFormat(plainPassword, passwordHash)
            : VerifyLegacySha256HexFormat(plainPassword, passwordHash);
    }

    private static bool VerifyCurrentFormat(string plainPassword, string passwordHash)
    {
        var segments = passwordHash.Split('$');
        if (segments.Length != 4)
        {
            return false;
        }

        if (!int.TryParse(segments[1], out var iterations) || iterations < 100_000)
        {
            return false;
        }

        if (!TryDecodeBase64(segments[2], out var salt) || salt.Length < 8)
        {
            return false;
        }

        if (!TryDecodeBase64(segments[3], out var expectedDerivedKey) || expectedDerivedKey.Length < 16)
        {
            return false;
        }

        var computedDerivedKey = Rfc2898DeriveBytes.Pbkdf2(
            plainPassword,
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            expectedDerivedKey.Length);

        return CryptographicOperations.FixedTimeEquals(computedDerivedKey, expectedDerivedKey);
    }

    private static bool VerifyLegacySha256HexFormat(string plainPassword, string passwordHash)
    {
        if (passwordHash.Length != 64)
        {
            return false;
        }

        byte[] decodedHash;
        try
        {
            decodedHash = Convert.FromHexString(passwordHash);
        }
        catch (FormatException)
        {
            return false;
        }

        if (decodedHash.Length != 32)
        {
            return false;
        }

        var computedHash = SHA256.HashData(Encoding.UTF8.GetBytes(plainPassword));
        return CryptographicOperations.FixedTimeEquals(decodedHash, computedHash);
    }

    private static bool TryDecodeBase64(string encoded, out byte[] value)
    {
        try
        {
            value = Convert.FromBase64String(encoded);
            return true;
        }
        catch (FormatException)
        {
            value = [];
            return false;
        }
    }
}
