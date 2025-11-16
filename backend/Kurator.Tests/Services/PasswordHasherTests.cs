using Xunit;
using FluentAssertions;
using Kurator.Infrastructure.Services;

namespace Kurator.Tests.Services;

public class PasswordHasherTests
{
    private readonly PasswordHasher _passwordHasher;

    public PasswordHasherTests()
    {
        _passwordHasher = new PasswordHasher();
    }

    [Fact]
    public void HashPassword_WithValidPassword_ShouldReturnHashedPassword()
    {
        // Arrange
        var password = "TestPassword123!";

        // Act
        var hashedPassword = _passwordHasher.HashPassword(password);

        // Assert
        hashedPassword.Should().NotBeNullOrEmpty();
        hashedPassword.Should().NotBe(password);
        hashedPassword.Length.Should().BeGreaterThan(50); // BCrypt hashes are typically 60 characters
    }

    [Fact]
    public void VerifyPassword_WithCorrectPassword_ShouldReturnTrue()
    {
        // Arrange
        var password = "TestPassword123!";
        var hashedPassword = _passwordHasher.HashPassword(password);

        // Act
        var result = _passwordHasher.VerifyPassword(password, hashedPassword);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_WithIncorrectPassword_ShouldReturnFalse()
    {
        // Arrange
        var password = "TestPassword123!";
        var wrongPassword = "WrongPassword456!";
        var hashedPassword = _passwordHasher.HashPassword(password);

        // Act
        var result = _passwordHasher.VerifyPassword(wrongPassword, hashedPassword);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void HashPassword_SamePasswordTwice_ShouldProduceDifferentHashes()
    {
        // Arrange
        var password = "TestPassword123!";

        // Act
        var hash1 = _passwordHasher.HashPassword(password);
        var hash2 = _passwordHasher.HashPassword(password);

        // Assert
        hash1.Should().NotBe(hash2); // BCrypt adds salt, so hashes should differ
    }

    [Fact]
    public void VerifyPassword_WithBothHashes_ShouldReturnTrue()
    {
        // Arrange
        var password = "TestPassword123!";
        var hash1 = _passwordHasher.HashPassword(password);
        var hash2 = _passwordHasher.HashPassword(password);

        // Act & Assert
        _passwordHasher.VerifyPassword(password, hash1).Should().BeTrue();
        _passwordHasher.VerifyPassword(password, hash2).Should().BeTrue();
    }

    [Fact]
    public void HashPassword_WithEmptyPassword_ShouldStillProduceHash()
    {
        // Arrange
        var password = "";

        // Act
        var hashedPassword = _passwordHasher.HashPassword(password);

        // Assert
        hashedPassword.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void HashPassword_WithRussianCharacters_ShouldWorkCorrectly()
    {
        // Arrange
        var password = "Пароль123!";

        // Act
        var hashedPassword = _passwordHasher.HashPassword(password);
        var isValid = _passwordHasher.VerifyPassword(password, hashedPassword);

        // Assert
        hashedPassword.Should().NotBeNullOrEmpty();
        isValid.Should().BeTrue();
    }

    [Fact]
    public void HashPassword_WithSpecialCharacters_ShouldWorkCorrectly()
    {
        // Arrange
        var password = "P@ssw0rd!@#$%^&*()";

        // Act
        var hashedPassword = _passwordHasher.HashPassword(password);
        var isValid = _passwordHasher.VerifyPassword(password, hashedPassword);

        // Assert
        hashedPassword.Should().NotBeNullOrEmpty();
        isValid.Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_IsCaseSensitive()
    {
        // Arrange
        var password = "TestPassword";
        var hashedPassword = _passwordHasher.HashPassword(password);

        // Act
        var result = _passwordHasher.VerifyPassword("testpassword", hashedPassword);

        // Assert
        result.Should().BeFalse();
    }
}
