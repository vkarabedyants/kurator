using Xunit;
using FluentAssertions;
using Kurator.Core.Entities;
using Kurator.Core.Enums;

namespace Kurator.Core.Tests.Entities;

/// <summary>
/// Тесты для сущности BlockCurator (связь кураторов с блоками)
/// </summary>
public class BlockCuratorTests
{
    [Fact]
    public void BlockCurator_ShouldHaveDefaultAssignedAtValue()
    {
        // Act
        var blockCurator = new BlockCurator();

        // Assert
        blockCurator.AssignedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData(CuratorType.Primary)]
    [InlineData(CuratorType.Backup)]
    public void BlockCurator_ShouldSupportBothCuratorTypes(CuratorType curatorType)
    {
        // Arrange & Act
        var blockCurator = new BlockCurator
        {
            CuratorType = curatorType
        };

        // Assert
        blockCurator.CuratorType.Should().Be(curatorType);
    }

    [Fact]
    public void BlockCurator_ShouldAllowSettingAllRequiredFields()
    {
        // Arrange & Act
        var blockCurator = new BlockCurator
        {
            BlockId = 1,
            UserId = 10,
            CuratorType = CuratorType.Primary,
            AssignedBy = 5
        };

        // Assert
        blockCurator.BlockId.Should().Be(1);
        blockCurator.UserId.Should().Be(10);
        blockCurator.CuratorType.Should().Be(CuratorType.Primary);
        blockCurator.AssignedBy.Should().Be(5);
    }

    [Fact]
    public void BlockCurator_AssignedBy_CanBeNull()
    {
        // Act
        var blockCurator = new BlockCurator
        {
            AssignedBy = null
        };

        // Assert
        blockCurator.AssignedBy.Should().BeNull();
    }

    [Fact]
    public void BlockCurator_ShouldTrackAssignmentTime()
    {
        // Arrange
        var specificTime = new DateTime(2025, 11, 15, 10, 30, 0, DateTimeKind.Utc);

        // Act
        var blockCurator = new BlockCurator
        {
            AssignedAt = specificTime
        };

        // Assert
        blockCurator.AssignedAt.Should().Be(specificTime);
    }
}
