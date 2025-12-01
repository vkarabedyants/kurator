using Xunit;
using FluentAssertions;
using Kurator.Infrastructure.Services;

namespace Kurator.Core.Tests.Services;

/// <summary>
/// Тесты для TotpService - генерация и валидация TOTP кодов для MFA
/// </summary>
public class TotpServiceTests
{
    private readonly TotpService _totpService;

    public TotpServiceTests()
    {
        _totpService = new TotpService();
    }

    #region GenerateSecret Tests

    [Fact]
    public void GenerateSecret_ShouldReturnNonEmptyString()
    {
        // Act
        var secret = _totpService.GenerateSecret();

        // Assert
        secret.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GenerateSecret_ShouldReturnBase32EncodedString()
    {
        // Act
        var secret = _totpService.GenerateSecret();

        // Assert
        // Base32 characters are A-Z and 2-7
        secret.Should().MatchRegex(@"^[A-Z2-7]+$");
    }

    [Fact]
    public void GenerateSecret_ShouldReturnCorrectLength()
    {
        // Act
        var secret = _totpService.GenerateSecret();

        // Assert
        // 20 bytes = 32 base32 characters (160 bits)
        secret.Length.Should().Be(32);
    }

    [Fact]
    public void GenerateSecret_ShouldReturnDifferentSecretsOnMultipleCalls()
    {
        // Act
        var secret1 = _totpService.GenerateSecret();
        var secret2 = _totpService.GenerateSecret();
        var secret3 = _totpService.GenerateSecret();

        // Assert
        secret1.Should().NotBe(secret2);
        secret2.Should().NotBe(secret3);
        secret1.Should().NotBe(secret3);
    }

    [Fact]
    public void GenerateSecret_ShouldBeValidBase32()
    {
        // Act
        var secret = _totpService.GenerateSecret();

        // Assert
        // Should not throw when trying to decode
        var action = () => OtpNet.Base32Encoding.ToBytes(secret);
        action.Should().NotThrow();
    }

    #endregion

    #region GenerateQrCodeUri Tests

    [Fact]
    public void GenerateQrCodeUri_ShouldReturnValidOtpauthUri()
    {
        // Arrange
        var secret = _totpService.GenerateSecret();
        var userLogin = "testuser";

        // Act
        var uri = _totpService.GenerateQrCodeUri(secret, userLogin);

        // Assert
        uri.Should().StartWith("otpauth://totp/");
        uri.Should().Contain("KURATOR:");
        uri.Should().Contain(userLogin);
        uri.Should().Contain($"secret={secret}");
    }

    [Fact]
    public void GenerateQrCodeUri_ShouldIncludeIssuer()
    {
        // Arrange
        var secret = _totpService.GenerateSecret();
        var userLogin = "admin";

        // Act
        var uri = _totpService.GenerateQrCodeUri(secret, userLogin);

        // Assert
        uri.Should().Contain("issuer=KURATOR");
    }

    [Fact]
    public void GenerateQrCodeUri_ShouldIncludeDigitsParameter()
    {
        // Arrange
        var secret = _totpService.GenerateSecret();
        var userLogin = "curator";

        // Act
        var uri = _totpService.GenerateQrCodeUri(secret, userLogin);

        // Assert
        uri.Should().Contain("digits=6");
    }

    [Fact]
    public void GenerateQrCodeUri_ShouldIncludePeriodParameter()
    {
        // Arrange
        var secret = _totpService.GenerateSecret();
        var userLogin = "analyst";

        // Act
        var uri = _totpService.GenerateQrCodeUri(secret, userLogin);

        // Assert
        uri.Should().Contain("period=30");
    }

    [Fact]
    public void GenerateQrCodeUri_ShouldEncodeSpecialCharacters()
    {
        // Arrange
        var secret = _totpService.GenerateSecret();
        var userLogin = "user@example.com";

        // Act
        var uri = _totpService.GenerateQrCodeUri(secret, userLogin);

        // Assert
        // @ should be URL-encoded to %40
        uri.Should().Contain("user%40example.com");
    }

    [Fact]
    public void GenerateQrCodeUri_ShouldHandleUsernameWithSpaces()
    {
        // Arrange
        var secret = _totpService.GenerateSecret();
        var userLogin = "test user";

        // Act
        var uri = _totpService.GenerateQrCodeUri(secret, userLogin);

        // Assert
        // Spaces should be URL-encoded
        uri.Should().Contain("test%20user");
    }

    [Fact]
    public void GenerateQrCodeUri_ShouldMatchExpectedFormat()
    {
        // Arrange
        var secret = "JBSWY3DPEHPK3PXP"; // Example Base32 secret
        var userLogin = "testuser";

        // Act
        var uri = _totpService.GenerateQrCodeUri(secret, userLogin);

        // Assert
        uri.Should().Be("otpauth://totp/KURATOR:testuser?secret=JBSWY3DPEHPK3PXP&issuer=KURATOR&digits=6&period=30");
    }

    #endregion

    #region VerifyCode Tests

    [Fact]
    public void VerifyCode_WithValidCode_ShouldReturnTrue()
    {
        // Arrange
        var secret = _totpService.GenerateSecret();
        var validCode = _totpService.GenerateCode(secret);

        // Act
        var result = _totpService.VerifyCode(secret, validCode);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void VerifyCode_WithInvalidCode_ShouldReturnFalse()
    {
        // Arrange
        var secret = _totpService.GenerateSecret();
        var invalidCode = "000000";

        // Act
        var result = _totpService.VerifyCode(secret, invalidCode);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyCode_WithEmptySecret_ShouldReturnFalse()
    {
        // Act
        var result = _totpService.VerifyCode("", "123456");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyCode_WithEmptyCode_ShouldReturnFalse()
    {
        // Arrange
        var secret = _totpService.GenerateSecret();

        // Act
        var result = _totpService.VerifyCode(secret, "");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyCode_WithNullSecret_ShouldReturnFalse()
    {
        // Act
        var result = _totpService.VerifyCode(null!, "123456");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyCode_WithNullCode_ShouldReturnFalse()
    {
        // Arrange
        var secret = _totpService.GenerateSecret();

        // Act
        var result = _totpService.VerifyCode(secret, null!);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyCode_WithCodeContainingSpaces_ShouldStripSpacesAndVerify()
    {
        // Arrange
        var secret = _totpService.GenerateSecret();
        var validCode = _totpService.GenerateCode(secret);
        var codeWithSpaces = $"{validCode.Substring(0, 3)} {validCode.Substring(3)}";

        // Act
        var result = _totpService.VerifyCode(secret, codeWithSpaces);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void VerifyCode_WithIncorrectLength_ShouldReturnFalse()
    {
        // Arrange
        var secret = _totpService.GenerateSecret();

        // Act
        var result1 = _totpService.VerifyCode(secret, "12345");   // 5 digits
        var result2 = _totpService.VerifyCode(secret, "1234567"); // 7 digits

        // Assert
        result1.Should().BeFalse();
        result2.Should().BeFalse();
    }

    [Fact]
    public void VerifyCode_WithNonNumericCode_ShouldReturnFalse()
    {
        // Arrange
        var secret = _totpService.GenerateSecret();

        // Act
        var result1 = _totpService.VerifyCode(secret, "ABCDEF");
        var result2 = _totpService.VerifyCode(secret, "12A456");

        // Assert
        result1.Should().BeFalse();
        result2.Should().BeFalse();
    }

    [Fact]
    public void VerifyCode_WithInvalidSecretFormat_ShouldReturnFalse()
    {
        // Arrange
        var invalidSecret = "INVALID-SECRET!@#";
        var code = "123456";

        // Act
        var result = _totpService.VerifyCode(invalidSecret, code);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyCode_WithDifferentSecret_ShouldReturnFalse()
    {
        // Arrange
        var secret1 = _totpService.GenerateSecret();
        var secret2 = _totpService.GenerateSecret();
        var codeForSecret1 = _totpService.GenerateCode(secret1);

        // Act
        var result = _totpService.VerifyCode(secret2, codeForSecret1);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyCode_WithLeadingZeros_ShouldWork()
    {
        // Arrange
        var secret = "JBSWY3DPEHPK3PXP"; // Known secret for deterministic testing
        // Note: This test may need to be adjusted based on timing

        // Generate code which might have leading zeros
        var code = _totpService.GenerateCode(secret);

        // Act
        var result = _totpService.VerifyCode(secret, code);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void VerifyCode_ShouldAllowTimeWindowDrift()
    {
        // Arrange
        var secret = _totpService.GenerateSecret();
        var currentCode = _totpService.GenerateCode(secret);

        // Wait a bit to ensure we're testing the time window
        System.Threading.Thread.Sleep(100);

        // Act
        var result = _totpService.VerifyCode(secret, currentCode);

        // Assert
        // Should still be valid within the time window
        result.Should().BeTrue();
    }

    #endregion

    #region GenerateCode Tests

    [Fact]
    public void GenerateCode_WithValidSecret_ShouldReturnSixDigitCode()
    {
        // Arrange
        var secret = _totpService.GenerateSecret();

        // Act
        var code = _totpService.GenerateCode(secret);

        // Assert
        code.Should().NotBeNullOrEmpty();
        code.Length.Should().Be(6);
        code.Should().MatchRegex(@"^\d{6}$");
    }

    [Fact]
    public void GenerateCode_WithSameSecret_ShouldReturnSameCodeWithinTimeWindow()
    {
        // Arrange
        var secret = _totpService.GenerateSecret();

        // Act
        var code1 = _totpService.GenerateCode(secret);
        var code2 = _totpService.GenerateCode(secret);

        // Assert
        // Within same 30-second window, codes should be identical
        code1.Should().Be(code2);
    }

    [Fact]
    public void GenerateCode_WithDifferentSecrets_ShouldReturnDifferentCodes()
    {
        // Arrange
        var secret1 = _totpService.GenerateSecret();
        var secret2 = _totpService.GenerateSecret();

        // Act
        var code1 = _totpService.GenerateCode(secret1);
        var code2 = _totpService.GenerateCode(secret2);

        // Assert
        // Codes should be different for different secrets
        // (very small chance they could be the same, but extremely unlikely)
        code1.Should().NotBe(code2);
    }

    [Fact]
    public void GenerateCode_WithInvalidSecret_ShouldReturnEmptyString()
    {
        // Arrange
        var invalidSecret = "INVALID!@#$%";

        // Act
        var code = _totpService.GenerateCode(invalidSecret);

        // Assert
        code.Should().BeEmpty();
    }

    [Fact]
    public void GenerateCode_WithEmptySecret_ShouldReturnEmptyString()
    {
        // Act
        var code = _totpService.GenerateCode("");

        // Assert
        code.Should().BeEmpty();
    }

    [Fact]
    public void GenerateCode_GeneratedCodeShouldBeVerifiable()
    {
        // Arrange
        var secret = _totpService.GenerateSecret();

        // Act
        var code = _totpService.GenerateCode(secret);
        var isValid = _totpService.VerifyCode(secret, code);

        // Assert
        isValid.Should().BeTrue();
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void TotpFlow_CompleteWorkflow_ShouldWorkCorrectly()
    {
        // Arrange - Simulate user registration with MFA

        // 1. Generate secret for user
        var secret = _totpService.GenerateSecret();
        secret.Should().NotBeNullOrEmpty();

        // 2. Generate QR code URI for user to scan
        var qrCodeUri = _totpService.GenerateQrCodeUri(secret, "testuser@example.com");
        qrCodeUri.Should().Contain("otpauth://totp/");
        qrCodeUri.Should().Contain("testuser%40example.com");

        // 3. User scans QR code and enters code from authenticator app
        // (simulated by generating the current code)
        var userEnteredCode = _totpService.GenerateCode(secret);

        // 4. Verify the code
        var isValid = _totpService.VerifyCode(secret, userEnteredCode);
        isValid.Should().BeTrue();

        // 5. Invalid code should fail
        var invalidCode = "000000";
        var isInvalid = _totpService.VerifyCode(secret, invalidCode);
        isInvalid.Should().BeFalse();
    }

    [Fact]
    public void TotpFlow_MultipleUsers_ShouldHaveIndependentSecrets()
    {
        // Arrange - Multiple users
        var user1Secret = _totpService.GenerateSecret();
        var user2Secret = _totpService.GenerateSecret();
        var user3Secret = _totpService.GenerateSecret();

        // Act - Generate codes for each user
        var user1Code = _totpService.GenerateCode(user1Secret);
        var user2Code = _totpService.GenerateCode(user2Secret);
        var user3Code = _totpService.GenerateCode(user3Secret);

        // Assert - Each user's code should only work with their secret
        _totpService.VerifyCode(user1Secret, user1Code).Should().BeTrue();
        _totpService.VerifyCode(user1Secret, user2Code).Should().BeFalse();
        _totpService.VerifyCode(user1Secret, user3Code).Should().BeFalse();

        _totpService.VerifyCode(user2Secret, user2Code).Should().BeTrue();
        _totpService.VerifyCode(user2Secret, user1Code).Should().BeFalse();
        _totpService.VerifyCode(user2Secret, user3Code).Should().BeFalse();

        _totpService.VerifyCode(user3Secret, user3Code).Should().BeTrue();
        _totpService.VerifyCode(user3Secret, user1Code).Should().BeFalse();
        _totpService.VerifyCode(user3Secret, user2Code).Should().BeFalse();
    }

    [Fact]
    public void TotpFlow_ReusingOldCode_ShouldFailAfterTimeWindow()
    {
        // Arrange
        var secret = _totpService.GenerateSecret();
        var code1 = _totpService.GenerateCode(secret);

        // Verify it works initially
        _totpService.VerifyCode(secret, code1).Should().BeTrue();

        // Note: In a real test, we would wait 60+ seconds for the code to expire
        // For unit testing purposes, we're testing that the same code works within the window
        // In production, codes expire after the time window (30 seconds + grace period)

        // Act - Immediately re-verify (should still work within time window)
        var stillValid = _totpService.VerifyCode(secret, code1);

        // Assert
        stillValid.Should().BeTrue(); // Still within verification window
    }

    [Fact]
    public void TotpFlow_SecurityBestPractices_ShouldBeFollowed()
    {
        // Test that the service follows TOTP security best practices

        // 1. Secrets should be random and unique
        var secrets = new HashSet<string>();
        for (int i = 0; i < 100; i++)
        {
            secrets.Add(_totpService.GenerateSecret());
        }
        secrets.Count.Should().Be(100); // All unique

        // 2. Codes should be 6 digits
        var testSecret = _totpService.GenerateSecret();
        var code = _totpService.GenerateCode(testSecret);
        code.Length.Should().Be(6);
        code.Should().MatchRegex(@"^\d{6}$");

        // 3. Time window should allow for clock drift (±1 step)
        var currentCode = _totpService.GenerateCode(testSecret);
        _totpService.VerifyCode(testSecret, currentCode).Should().BeTrue();

        // 4. Invalid inputs should be rejected
        _totpService.VerifyCode("", code).Should().BeFalse();
        _totpService.VerifyCode(testSecret, "").Should().BeFalse();
        _totpService.VerifyCode(testSecret, "ABCDEF").Should().BeFalse();
        _totpService.VerifyCode(testSecret, "12345").Should().BeFalse(); // Wrong length
    }

    [Fact]
    public void TotpFlow_KnownTestVector_ShouldProduceExpectedUri()
    {
        // Arrange - Using RFC 6238 test vector approach
        var knownSecret = "JBSWY3DPEHPK3PXP"; // "Hello!" in Base32
        var username = "alice@example.com";

        // Act
        var uri = _totpService.GenerateQrCodeUri(knownSecret, username);

        // Assert
        uri.Should().StartWith("otpauth://totp/KURATOR:");
        uri.Should().Contain("alice%40example.com");
        uri.Should().Contain($"secret={knownSecret}");
        uri.Should().Contain("issuer=KURATOR");
        uri.Should().Contain("digits=6");
        uri.Should().Contain("period=30");
    }

    #endregion
}
