using Xunit;
using FluentAssertions;
using Kurator.Core.Entities;
using Kurator.Core.Enums;

namespace Kurator.Tests.Entities;

public class UserEntityTests
{
    [Fact]
    public void User_DefaultValues_ShouldBeSetCorrectly()
    {
        // Arrange & Act
        var user = new User();

        // Assert
        user.IsFirstLogin.Should().BeTrue();
        user.MfaEnabled.Should().BeFalse();
        user.IsActive.Should().BeTrue();
        user.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        user.BlockAssignments.Should().NotBeNull().And.BeEmpty();
        user.Contacts.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void User_WithRole_ShouldStoreRoleCorrectly()
    {
        // Arrange
        var user = new User
        {
            Login = "admin@test.com",
            Role = UserRole.Admin
        };

        // Act & Assert
        user.Role.Should().Be(UserRole.Admin);
    }

    [Fact]
    public void User_WithMfaSettings_ShouldStoreCorrectly()
    {
        // Arrange
        var user = new User
        {
            Login = "user@test.com",
            MfaSecret = "TEST_SECRET",
            MfaEnabled = true
        };

        // Act & Assert
        user.MfaSecret.Should().Be("TEST_SECRET");
        user.MfaEnabled.Should().BeTrue();
    }

    [Fact]
    public void User_WithPublicKey_ShouldStoreCorrectly()
    {
        // Arrange
        var publicKey = "-----BEGIN PUBLIC KEY-----\ntest_key\n-----END PUBLIC KEY-----";
        var user = new User
        {
            Login = "user@test.com",
            PublicKey = publicKey
        };

        // Act & Assert
        user.PublicKey.Should().Be(publicKey);
    }

    [Fact]
    public void User_AllRoles_ShouldBeAvailable()
    {
        // Arrange & Act & Assert
        var admin = new User { Role = UserRole.Admin };
        var curator = new User { Role = UserRole.Curator };
        var analyst = new User { Role = UserRole.ThreatAnalyst };

        admin.Role.Should().Be(UserRole.Admin);
        curator.Role.Should().Be(UserRole.Curator);
        analyst.Role.Should().Be(UserRole.ThreatAnalyst);
    }
}
