using Xunit;
using FluentAssertions;
using Kurator.Core.Entities;
using Kurator.Core.Enums;

namespace Kurator.Core.Tests.Entities;

/// <summary>
/// Тесты для сущности AuditLog (журнал аудита)
/// </summary>
public class AuditLogTests
{
    [Fact]
    public void AuditLog_ShouldHaveDefaultValues()
    {
        // Act
        var auditLog = new AuditLog();

        // Assert
        auditLog.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        auditLog.EntityType.Should().BeEmpty();
        auditLog.EntityId.Should().BeEmpty();
        auditLog.OldValuesJson.Should().BeNull();
        auditLog.NewValuesJson.Should().BeNull();
    }

    [Fact]
    public void AuditLog_ShouldAllowSettingAllProperties()
    {
        // Arrange
        var specificTime = new DateTime(2024, 11, 20, 16, 45, 0, DateTimeKind.Utc);
        var oldValuesJson = "{\"name\":\"Old Name\",\"status\":\"Active\"}";
        var newValuesJson = "{\"name\":\"New Name\",\"status\":\"Inactive\"}";

        // Act
        var auditLog = new AuditLog
        {
            UserId = 5,
            Action = AuditActionType.Update,
            EntityType = "Contact",
            EntityId = "123",
            OldValuesJson = oldValuesJson,
            NewValuesJson = newValuesJson,
            Timestamp = specificTime
        };

        // Assert
        auditLog.UserId.Should().Be(5);
        auditLog.Action.Should().Be(AuditActionType.Update);
        auditLog.EntityType.Should().Be("Contact");
        auditLog.EntityId.Should().Be("123");
        auditLog.OldValuesJson.Should().Be(oldValuesJson);
        auditLog.NewValuesJson.Should().Be(newValuesJson);
        auditLog.Timestamp.Should().Be(specificTime);
    }

    [Fact]
    public void AuditLog_ShouldSupportAllAuditActionTypes()
    {
        // Arrange & Act & Assert
        var createLog = new AuditLog { Action = AuditActionType.Create };
        var updateLog = new AuditLog { Action = AuditActionType.Update };
        var deleteLog = new AuditLog { Action = AuditActionType.Delete };
        var statusChangeLog = new AuditLog { Action = AuditActionType.StatusChange };

        createLog.Action.Should().Be(AuditActionType.Create);
        updateLog.Action.Should().Be(AuditActionType.Update);
        deleteLog.Action.Should().Be(AuditActionType.Delete);
        statusChangeLog.Action.Should().Be(AuditActionType.StatusChange);
    }

    [Theory]
    [InlineData("Contact")]
    [InlineData("Interaction")]
    [InlineData("Block")]
    [InlineData("FAQ")]
    [InlineData("User")]
    [InlineData("Watchlist")]
    [InlineData("ReferenceValue")]
    public void AuditLog_ShouldSupportAllEntityTypes(string entityType)
    {
        // Arrange & Act
        var auditLog = new AuditLog { EntityType = entityType };

        // Assert
        auditLog.EntityType.Should().Be(entityType);
    }

    [Fact]
    public void AuditLog_EntityId_ShouldSupportStringIds()
    {
        // Arrange
        var stringIds = new[] { "123", "CONTACT-001", "USER-42", "BLOCK-5" };

        foreach (var id in stringIds)
        {
            // Act
            var auditLog = new AuditLog { EntityId = id };

            // Assert
            auditLog.EntityId.Should().Be(id);
        }
    }

    [Fact]
    public void AuditLog_ShouldSupportCreateOperationsWithNullOldValues()
    {
        // Act
        var createLog = new AuditLog
        {
            Action = AuditActionType.Create,
            EntityType = "Contact",
            EntityId = "456",
            OldValuesJson = null, // Для CREATE операций старые значения должны быть null
            NewValuesJson = "{\"name\":\"New Contact\",\"status\":\"Active\"}"
        };

        // Assert
        createLog.Action.Should().Be(AuditActionType.Create);
        createLog.OldValuesJson.Should().BeNull();
        createLog.NewValuesJson.Should().NotBeNull();
    }

    [Fact]
    public void AuditLog_ShouldSupportDeleteOperationsWithNullNewValues()
    {
        // Act
        var deleteLog = new AuditLog
        {
            Action = AuditActionType.Delete,
            EntityType = "Contact",
            EntityId = "789",
            OldValuesJson = "{\"name\":\"Contact to Delete\",\"status\":\"Active\"}",
            NewValuesJson = null // Для DELETE операций новые значения могут быть null
        };

        // Assert
        deleteLog.Action.Should().Be(AuditActionType.Delete);
        deleteLog.OldValuesJson.Should().NotBeNull();
        deleteLog.NewValuesJson.Should().BeNull();
    }

    [Fact]
    public void AuditLog_ShouldSupportUpdateOperationsWithBothValues()
    {
        // Arrange
        var oldValues = "{\"name\":\"Old Name\",\"position\":\"Manager\"}";
        var newValues = "{\"name\":\"New Name\",\"position\":\"Senior Manager\"}";

        // Act
        var updateLog = new AuditLog
        {
            Action = AuditActionType.Update,
            EntityType = "Contact",
            EntityId = "321",
            OldValuesJson = oldValues,
            NewValuesJson = newValues
        };

        // Assert
        updateLog.Action.Should().Be(AuditActionType.Update);
        updateLog.OldValuesJson.Should().Be(oldValues);
        updateLog.NewValuesJson.Should().Be(newValues);
    }

    [Fact]
    public void AuditLog_ShouldTrackTimestampPrecisely()
    {
        // Arrange
        var specificTime = new DateTime(2024, 11, 20, 18, 30, 45, 123, DateTimeKind.Utc);

        // Act
        var auditLog = new AuditLog { Timestamp = specificTime };

        // Assert
        auditLog.Timestamp.Should().Be(specificTime);
        auditLog.Timestamp.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void AuditLog_OldValuesJson_CanBeNull()
    {
        // Act
        var auditLog = new AuditLog
        {
            OldValuesJson = null,
            NewValuesJson = "{\"some\":\"data\"}"
        };

        // Assert
        auditLog.OldValuesJson.Should().BeNull();
        auditLog.NewValuesJson.Should().NotBeNull();
    }

    [Fact]
    public void AuditLog_NewValuesJson_CanBeNull()
    {
        // Act
        var auditLog = new AuditLog
        {
            OldValuesJson = "{\"some\":\"old_data\"}",
            NewValuesJson = null
        };

        // Assert
        auditLog.OldValuesJson.Should().NotBeNull();
        auditLog.NewValuesJson.Should().BeNull();
    }

    [Fact]
    public void AuditLog_ShouldSupportComplexJsonValues()
    {
        // Arrange
        var complexOldValues = @"{
            ""name"": ""John Doe"",
            ""contacts"": [""email@domain.com"", ""+1234567890""],
            ""metadata"": {
                ""source"": ""web"",
                ""tags"": [""important"", ""vip""]
            }
        }";

        var complexNewValues = @"{
            ""name"": ""Jane Doe"",
            ""contacts"": [""newemail@domain.com"", ""+0987654321""],
            ""metadata"": {
                ""source"": ""web"",
                ""tags"": [""important"", ""vip"", ""updated""]
            }
        }";

        // Act
        var auditLog = new AuditLog
        {
            OldValuesJson = complexOldValues,
            NewValuesJson = complexNewValues
        };

        // Assert
        auditLog.OldValuesJson.Should().Contain("John Doe");
        auditLog.OldValuesJson.Should().Contain("email@domain.com");
        auditLog.NewValuesJson.Should().Contain("Jane Doe");
        auditLog.NewValuesJson.Should().Contain("newemail@domain.com");
        auditLog.NewValuesJson.Should().Contain("updated");
    }

    [Fact]
    public void AuditLog_ShouldSupportEmptyJsonObjects()
    {
        // Act
        var auditLog = new AuditLog
        {
            OldValuesJson = "{}",
            NewValuesJson = "{}"
        };

        // Assert
        auditLog.OldValuesJson.Should().Be("{}");
        auditLog.NewValuesJson.Should().Be("{}");
    }

    [Fact]
    public void AuditLog_EntityType_ShouldNotBeNull()
    {
        // Act
        var auditLog = new AuditLog();

        // Assert
        auditLog.EntityType.Should().NotBeNull();
        // По умолчанию пустая строка, но не null
    }

    [Fact]
    public void AuditLog_EntityId_ShouldNotBeNull()
    {
        // Act
        var auditLog = new AuditLog();

        // Assert
        auditLog.EntityId.Should().NotBeNull();
        // По умолчанию пустая строка, но не null
    }
}
