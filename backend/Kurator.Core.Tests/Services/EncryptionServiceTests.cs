using Xunit;
using FluentAssertions;
using Kurator.Infrastructure.Services;
using Kurator.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Kurator.Core.Tests.Services;

/// <summary>
/// Тесты для сервиса шифрования данных
/// </summary>
public class EncryptionServiceTests
{
    private readonly IEncryptionService _encryptionService;
    private const string TestKey = "ThisIsATest32CharacterKeyForAES256!";

    public EncryptionServiceTests()
    {
        var configuration = new Mock<IConfiguration>();
        configuration.Setup(c => c["Encryption:Key"]).Returns(TestKey);
        _encryptionService = new EncryptionService(configuration.Object);
    }

    [Fact]
    public void Encrypt_ShouldReturnNonEmptyString()
    {
        // Arrange
        var plaintext = "Тестовые данные для шифрования";

        // Act
        var encrypted = _encryptionService.Encrypt(plaintext);

        // Assert
        encrypted.Should().NotBeNullOrEmpty();
        encrypted.Should().NotBe(plaintext);
    }

    [Fact]
    public void Decrypt_ShouldReturnOriginalText()
    {
        // Arrange
        var plaintext = "Секретная информация";
        var encrypted = _encryptionService.Encrypt(plaintext);

        // Act
        var decrypted = _encryptionService.Decrypt(encrypted);

        // Assert
        decrypted.Should().Be(plaintext);
    }

    [Fact]
    public void Encrypt_ShouldGenerateSameCiphertext_ForSameInput()
    {
        // Arrange
        var plaintext = "Same input text";

        // Act
        var encrypted1 = _encryptionService.Encrypt(plaintext);
        var encrypted2 = _encryptionService.Encrypt(plaintext);

        // Assert
        // ПРИМЕЧАНИЕ: Текущая реализация использует фиксированный IV
        // Это упрощает реализацию но снижает безопасность
        // TODO: Рассмотреть использование случайного IV для каждого шифрования
        encrypted1.Should().Be(encrypted2);
    }

    [Fact]
    public void Encrypt_ShouldReturnEmpty_ForEmptyInput()
    {
        // Act
        var encrypted = _encryptionService.Encrypt("");

        // Assert
        // EncryptionService возвращает пустую строку для пустого input
        encrypted.Should().BeEmpty();
    }

    [Fact]
    public void Encrypt_ShouldEncrypt_WhitespaceString()
    {
        // Arrange
        var whitespace = "   ";

        // Act
        var encrypted = _encryptionService.Encrypt(whitespace);
        var decrypted = _encryptionService.Decrypt(encrypted);

        // Assert
        encrypted.Should().NotBeEmpty();
        decrypted.Should().Be(whitespace);
    }

    [Fact]
    public void Decrypt_ShouldHandleMultipleEncryptDecryptCycles()
    {
        // Arrange
        var plaintext = "Циклическое шифрование";

        // Act & Assert
        for (int i = 0; i < 10; i++)
        {
            var encrypted = _encryptionService.Encrypt(plaintext);
            var decrypted = _encryptionService.Decrypt(encrypted);
            decrypted.Should().Be(plaintext);
        }
    }

    [Fact]
    public void Encrypt_ShouldHandleUnicodeCharacters()
    {
        // Arrange
        var plaintext = "Тест 中文 العربية emoji 🔐🚀";

        // Act
        var encrypted = _encryptionService.Encrypt(plaintext);
        var decrypted = _encryptionService.Decrypt(encrypted);

        // Assert
        decrypted.Should().Be(plaintext);
    }

    [Fact]
    public void Encrypt_ShouldHandleLargeText()
    {
        // Arrange
        var largeText = new string('A', 10000);

        // Act
        var encrypted = _encryptionService.Encrypt(largeText);
        var decrypted = _encryptionService.Decrypt(encrypted);

        // Assert
        decrypted.Should().Be(largeText);
        decrypted.Length.Should().Be(10000);
    }

    [Fact]
    public void Decrypt_ShouldThrowException_ForInvalidCiphertext()
    {
        // Arrange
        var invalidCiphertext = "this_is_not_valid_base64_encrypted_data";

        // Act & Assert
        var action = () => _encryptionService.Decrypt(invalidCiphertext);
        action.Should().Throw<Exception>();
    }

    [Fact]
    public void Decrypt_ShouldThrowException_ForTamperedCiphertext()
    {
        // Arrange
        var plaintext = "Original text";
        var encrypted = _encryptionService.Encrypt(plaintext);
        var tampered = encrypted.Substring(0, encrypted.Length - 5) + "XXXXX";

        // Act & Assert
        var action = () => _encryptionService.Decrypt(tampered);
        action.Should().Throw<Exception>();
    }

    [Theory]
    [InlineData("Admin123!")]
    [InlineData("Очень длинный пароль с русскими буквами и спецсимволами !@#$%")]
    [InlineData("短密码")]
    public void EncryptDecrypt_ShouldWork_ForVariousPasswords(string password)
    {
        // Act
        var encrypted = _encryptionService.Encrypt(password);
        var decrypted = _encryptionService.Decrypt(encrypted);

        // Assert
        decrypted.Should().Be(password);
    }
}
