using System.Security.Cryptography;
using System.Text;
using Anizaki.Infrastructure.Auth;

namespace Anizaki.Api.Tests;

public class Sha256CredentialHasherTests
{
    [Fact]
    public void HashPassword_ShouldReturnVersionedHash_AndVerifySuccessfully()
    {
        var hasher = new Sha256CredentialHasher();
        var plainPassword = "Password123!";

        var hash = hasher.HashPassword(plainPassword);

        Assert.StartsWith("pbkdf2-sha256.v1$", hash, StringComparison.Ordinal);
        Assert.True(hasher.VerifyPassword(plainPassword, hash));
    }

    [Fact]
    public void HashPassword_WithSameInput_ShouldGenerateDifferentHashes()
    {
        var hasher = new Sha256CredentialHasher();
        var plainPassword = "Password123!";

        var firstHash = hasher.HashPassword(plainPassword);
        var secondHash = hasher.HashPassword(plainPassword);

        Assert.NotEqual(firstHash, secondHash);
        Assert.True(hasher.VerifyPassword(plainPassword, firstHash));
        Assert.True(hasher.VerifyPassword(plainPassword, secondHash));
    }

    [Fact]
    public void VerifyPassword_WithWrongPassword_ShouldReturnFalse()
    {
        var hasher = new Sha256CredentialHasher();
        var hash = hasher.HashPassword("Password123!");

        var isValid = hasher.VerifyPassword("WrongPassword123!", hash);

        Assert.False(isValid);
    }

    [Fact]
    public void VerifyPassword_WithLegacySha256HexHash_ShouldStillReturnTrue()
    {
        const string plainPassword = "Password123!";
        var legacyHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(plainPassword)));
        var hasher = new Sha256CredentialHasher();

        var isValid = hasher.VerifyPassword(plainPassword, legacyHash);

        Assert.True(isValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("pbkdf2-sha256.v1$abc$not-base64$also-not-base64")]
    [InlineData("pbkdf2-sha256.v1$5000$YWJjZA==$YWJjZA==")]
    [InlineData("pbkdf2-sha256.v1$210000$not-base64$YWJjZA==")]
    public void VerifyPassword_WithMalformedHash_ShouldReturnFalse(string passwordHash)
    {
        var hasher = new Sha256CredentialHasher();

        var isValid = hasher.VerifyPassword("Password123!", passwordHash);

        Assert.False(isValid);
    }
}
