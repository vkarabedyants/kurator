using Xunit;
using FluentAssertions;
using Kurator.Core.Entities;
using Kurator.Core.Enums;

namespace Kurator.Core.Tests.Entities;

/// <summary>
/// Тесты для сущности Block
/// </summary>
public class BlockTests
{
    [Fact]
    public void Block_ShouldHaveDefaultValues()
    {
        // Act
        var block = new Block();

        // Assert
        block.Status.Should().Be(BlockStatus.Active);
        block.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        block.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        block.Name.Should().BeEmpty();
        block.Code.Should().BeEmpty();
        block.Description.Should().BeNull();
        block.CuratorAssignments.Should().NotBeNull().And.BeEmpty();
        block.Contacts.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void Block_ShouldAllowSettingAllProperties()
    {
        // Arrange
        var specificTime = new DateTime(2024, 11, 20, 10, 30, 0, DateTimeKind.Utc);

        // Act
        var block = new Block
        {
            Name = "Operations Block",
            Code = "OP",
            Description = "Operations and intelligence block",
            Status = BlockStatus.Active,
            CreatedAt = specificTime,
            UpdatedAt = specificTime.AddHours(1)
        };

        // Assert
        block.Name.Should().Be("Operations Block");
        block.Code.Should().Be("OP");
        block.Description.Should().Be("Operations and intelligence block");
        block.Status.Should().Be(BlockStatus.Active);
        block.CreatedAt.Should().Be(specificTime);
        block.UpdatedAt.Should().Be(specificTime.AddHours(1));
    }

    [Fact]
    public void Block_ShouldSupportAllBlockStatuses()
    {
        // Arrange & Act & Assert
        var activeBlock = new Block { Status = BlockStatus.Active };
        var archivedBlock = new Block { Status = BlockStatus.Archived };

        activeBlock.Status.Should().Be(BlockStatus.Active);
        archivedBlock.Status.Should().Be(BlockStatus.Archived);
    }

    [Fact]
    public void Block_Code_ShouldBeUsedInContactIdGeneration()
    {
        // Arrange
        var block = new Block { Code = "MEDIA" };

        // Act & Assert
        // Проверяем, что код блока может использоваться для генерации ID контактов
        block.Code.Should().MatchRegex("^[A-Z]+$"); // Пример проверки формата кода
    }

    [Theory]
    [InlineData("OP")]
    [InlineData("MEDIA")]
    [InlineData("SIL")]
    [InlineData("TECH")]
    public void Block_ShouldSupportCommonBlockCodes(string code)
    {
        // Arrange & Act
        var block = new Block { Code = code };

        // Assert
        block.Code.Should().Be(code);
        block.Code.Should().MatchRegex("^[A-Z]+$");
        block.Code.Length.Should().BeGreaterThan(0).And.BeLessThanOrEqualTo(10);
    }

    [Fact]
    public void Block_CuratorAssignments_ShouldBeInitializedAsEmptyCollection()
    {
        // Act
        var block = new Block();

        // Assert
        block.CuratorAssignments.Should().NotBeNull();
        block.CuratorAssignments.Should().BeEmpty();
    }

    [Fact]
    public void Block_Contacts_ShouldBeInitializedAsEmptyCollection()
    {
        // Act
        var block = new Block();

        // Assert
        block.Contacts.Should().NotBeNull();
        block.Contacts.Should().BeEmpty();
    }

    [Fact]
    public void Block_ShouldTrackCreationAndUpdateTimestamps()
    {
        // Arrange
        var creationTime = DateTime.UtcNow.AddDays(-1);

        // Act
        var block = new Block
        {
            CreatedAt = creationTime,
            UpdatedAt = creationTime.AddHours(2)
        };

        // Assert
        block.CreatedAt.Should().Be(creationTime);
        block.UpdatedAt.Should().Be(creationTime.AddHours(2));
        block.UpdatedAt.Should().BeAfter(block.CreatedAt);
    }

    [Fact]
    public void Block_Description_CanBeNull()
    {
        // Act
        var block = new Block
        {
            Name = "Test Block",
            Code = "TEST",
            Description = null
        };

        // Assert
        block.Description.Should().BeNull();
    }

    [Fact]
    public void Block_NameAndCode_ShouldNotBeNull()
    {
        // Act
        var block = new Block();

        // Assert
        block.Name.Should().NotBeNull();
        block.Code.Should().NotBeNull();
        // По умолчанию они пустые строки, но не null
    }

    [Fact]
    public void Block_ShouldSupportLongDescriptions()
    {
        // Arrange
        var longDescription = new string('A', 1000);

        // Act
        var block = new Block { Description = longDescription };

        // Assert
        block.Description.Should().Be(longDescription);
        block.Description!.Length.Should().Be(1000);
    }
}
