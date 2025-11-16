using Xunit;
using FluentAssertions;
using Moq;
using Microsoft.Extensions.Configuration;
using Kurator.Infrastructure.Services;

namespace Kurator.Tests.Services;

public class EncryptionServiceTests
{
    private readonly EncryptionService _encryptionService;

    public EncryptionServiceTests()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                {"Encryption:Key", "test-encryption-key-12345"}
            }!)
            .Build();

        _encryptionService = new EncryptionService(configuration);
    }

    [Fact]
    public void Encrypt_WithValidPlainText_ShouldReturnEncryptedString()
    {
        // Arrange
        var plainText = "Test sensitive data";

        // Act
        var encrypted = _encryptionService.Encrypt(plainText);

        // Assert
        encrypted.Should().NotBeNullOrEmpty();
        encrypted.Should().NotBe(plainText);
    }

    [Fact]
    public void Decrypt_WithValidEncryptedText_ShouldReturnOriginalPlainText()
    {
        // Arrange
        var plainText = "Test sensitive data";
        var encrypted = _encryptionService.Encrypt(plainText);

        // Act
        var decrypted = _encryptionService.Decrypt(encrypted);

        // Assert
        decrypted.Should().Be(plainText);
    }

    [Fact]
    public void Encrypt_WithEmptyString_ShouldReturnEmptyString()
    {
        // Arrange
        var plainText = string.Empty;

        // Act
        var encrypted = _encryptionService.Encrypt(plainText);

        // Assert
        encrypted.Should().BeEmpty();
    }

    [Fact]
    public void Decrypt_WithEmptyString_ShouldReturnEmptyString()
    {
        // Arrange
        var encryptedText = string.Empty;

        // Act
        var decrypted = _encryptionService.Decrypt(encryptedText);

        // Assert
        decrypted.Should().BeEmpty();
    }

    [Fact]
    public void EncryptDecrypt_WithRussianText_ShouldWorkCorrectly()
    {
        // Arrange
        var plainText = "Иванов Иван Иванович";

        // Act
        var encrypted = _encryptionService.Encrypt(plainText);
        var decrypted = _encryptionService.Decrypt(encrypted);

        // Assert
        decrypted.Should().Be(plainText);
    }

    [Fact]
    public void EncryptDecrypt_WithSpecialCharacters_ShouldWorkCorrectly()
    {
        // Arrange
        var plainText = "Test@#$%^&*()_+-={}[]|:;<>,.?/~";

        // Act
        var encrypted = _encryptionService.Encrypt(plainText);
        var decrypted = _encryptionService.Decrypt(encrypted);

        // Assert
        decrypted.Should().Be(plainText);
    }

    [Fact]
    public void EncryptDecrypt_WithLongText_ShouldWorkCorrectly()
    {
        // Arrange
        var plainText = new string('A', 1000);

        // Act
        var encrypted = _encryptionService.Encrypt(plainText);
        var decrypted = _encryptionService.Decrypt(encrypted);

        // Assert
        decrypted.Should().Be(plainText);
    }

    [Fact]
    public void Encrypt_DifferentPlainTexts_ShouldProduceDifferentEncryptions()
    {
        // Arrange
        var plainText1 = "Text 1";
        var plainText2 = "Text 2";

        // Act
        var encrypted1 = _encryptionService.Encrypt(plainText1);
        var encrypted2 = _encryptionService.Encrypt(plainText2);

        // Assert
        encrypted1.Should().NotBe(encrypted2);
    }
}
