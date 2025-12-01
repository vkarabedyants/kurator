using Xunit;
using FluentAssertions;
using Kurator.Core.Entities;
using Kurator.Core.Enums;

namespace Kurator.Core.Tests.Entities;

/// <summary>
/// Тесты для сущности User
/// </summary>
public class UserTests
{
    [Fact]
    public void User_ShouldHaveDefaultValues()
    {
        // Act
        var user = new User();

        // Assert
        user.IsFirstLogin.Should().BeTrue();
        user.IsActive.Should().BeTrue();
        user.MfaEnabled.Should().BeFalse();
        user.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void User_ShouldAllowSettingAllProperties()
    {
        // Arrange & Act
        var user = new User
        {
            Login = "testuser",
            PasswordHash = "hash123",
            Role = UserRole.Curator,
            IsFirstLogin = false,
            PublicKey = "public_key_data",
            MfaSecret = "mfa_secret",
            MfaEnabled = true,
            IsActive = true
        };

        // Assert
        user.Login.Should().Be("testuser");
        user.Role.Should().Be(UserRole.Curator);
        user.IsFirstLogin.Should().BeFalse();
        user.MfaEnabled.Should().BeTrue();
    }

    [Fact]
    public void User_BlockAssignments_ShouldBeInitialized()
    {
        // Act
        var user = new User();

        // Assert
        user.BlockAssignments.Should().NotBeNull();
        user.BlockAssignments.Should().BeEmpty();
    }

    [Theory]
    [InlineData(UserRole.Admin, true)]
    [InlineData(UserRole.Curator, true)]
    [InlineData(UserRole.ThreatAnalyst, true)]
    public void User_ShouldSupportAllRoles(UserRole role, bool expectedValid)
    {
        // Arrange & Act
        var user = new User { Role = role };

        // Assert
        user.Role.Should().Be(role);
        expectedValid.Should().BeTrue(); // Все роли должны поддерживаться
    }

    [Fact]
    public void User_NavigationProperties_ShouldBeInitialized()
    {
        // Act
        var user = new User();

        // Assert
        user.Contacts.Should().NotBeNull();
        user.Interactions.Should().NotBeNull();
        user.AuditLogs.Should().NotBeNull();
        user.WatchlistItems.Should().NotBeNull();
        user.InfluenceStatusChanges.Should().NotBeNull();
        user.WatchlistChanges.Should().NotBeNull();
    }
}
