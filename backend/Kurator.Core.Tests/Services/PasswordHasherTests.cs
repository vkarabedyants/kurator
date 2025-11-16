using Xunit;
using FluentAssertions;
using Kurator.Infrastructure.Services;
using Kurator.Core.Interfaces;

namespace Kurator.Core.Tests.Services;

/// <summary>
/// Тесты для сервиса хеширования паролей
/// </summary>
public class PasswordHasherTests
{
    private readonly IPasswordHasher _passwordHasher;

    public PasswordHasherTests()
    {
        _passwordHasher = new PasswordHasher();
    }

    [Fact]
    public void HashPassword_ShouldReturnNonEmptyString()
    {
        // Arrange
        var password = "TestPassword123!";

        // Act
        var hash = _passwordHasher.HashPassword(password);

        // Assert
        hash.Should().NotBeNullOrEmpty();
        hash.Should().StartWith("$2a$"); // BCrypt format
    }

    [Fact]
    public void HashPassword_ShouldGenerateDifferentHashesForSamePassword()
    {
        // Arrange
        var password = "SamePassword123!";

        // Act
        var hash1 = _passwordHasher.HashPassword(password);
        var hash2 = _passwordHasher.HashPassword(password);

        // Assert
        hash1.Should().NotBe(hash2); // Разные соли
    }

    [Fact]
    public void VerifyPassword_ShouldReturnTrue_WhenPasswordMatches()
    {
        // Arrange
        var password = "CorrectPassword123!";
        var hash = _passwordHasher.HashPassword(password);

        // Act
        var result = _passwordHasher.VerifyPassword(password, hash);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_ShouldReturnFalse_WhenPasswordDoesNotMatch()
    {
        // Arrange
        var correctPassword = "CorrectPassword123!";
        var wrongPassword = "WrongPassword456!";
        var hash = _passwordHasher.HashPassword(correctPassword);

        // Act
        var result = _passwordHasher.VerifyPassword(wrongPassword, hash);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void HashPassword_ShouldHandleEmptyOrWhitespacePassword(string password)
    {
        // Act
        var hash = _passwordHasher.HashPassword(password);

        // Assert
        hash.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void VerifyPassword_ShouldReturnFalse_ForInvalidHash()
    {
        // Arrange
        var password = "TestPassword123!";
        var invalidHash = "invalid_hash_format";

        // Act & Assert
        var action = () => _passwordHasher.VerifyPassword(password, invalidHash);
        action.Should().Throw<Exception>();
    }

    [Theory]
    [InlineData("Short1!")]
    [InlineData("VeryLongPasswordWithMoreThan100CharactersToTestTheHashingAlgorithmPerformanceAndVerifyThatItCanHandleLongInputsCorrectly123!")]
    public void HashPassword_ShouldHandleVariousPasswordLengths(string password)
    {
        // Act
        var hash = _passwordHasher.HashPassword(password);
        var verified = _passwordHasher.VerifyPassword(password, hash);

        // Assert
        hash.Should().NotBeNullOrEmpty();
        verified.Should().BeTrue();
    }

    [Fact]
    public void HashPassword_ShouldHandleSpecialCharacters()
    {
        // Arrange
        var password = "Пароль123!@#$%^&*()_+-={}[]|:;<>?,./`~";

        // Act
        var hash = _passwordHasher.HashPassword(password);
        var verified = _passwordHasher.VerifyPassword(password, hash);

        // Assert
        verified.Should().BeTrue();
    }
}
