using Xunit;
using FluentAssertions;
using Kurator.Core.Entities;

namespace Kurator.Core.Tests.Entities;

public class ReferenceValueTests
{
    [Fact]
    public void ReferenceValue_ShouldInitializeWithDefaultValues()
    {
        // Act
        var referenceValue = new ReferenceValue();

        // Assert
        referenceValue.Id.Should().Be(0);
        referenceValue.Category.Should().Be(string.Empty);
        referenceValue.Code.Should().Be(string.Empty);
        referenceValue.Name.Should().Be(string.Empty);
        referenceValue.Description.Should().BeNull();
        referenceValue.SortOrder.Should().Be(0);
        referenceValue.IsActive.Should().BeTrue();
        referenceValue.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        referenceValue.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void ReferenceValue_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var createdAt = DateTime.UtcNow.AddDays(-10);
        var updatedAt = DateTime.UtcNow.AddDays(-1);

        // Act
        var referenceValue = new ReferenceValue
        {
            Id = 1,
            Category = "influence_status",
            Code = "A",
            Name = "Уровень А",
            Description = "Высший уровень влияния",
            SortOrder = 1,
            IsActive = true,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };

        // Assert
        referenceValue.Id.Should().Be(1);
        referenceValue.Category.Should().Be("influence_status");
        referenceValue.Code.Should().Be("A");
        referenceValue.Name.Should().Be("Уровень А");
        referenceValue.Description.Should().Be("Высший уровень влияния");
        referenceValue.SortOrder.Should().Be(1);
        referenceValue.IsActive.Should().BeTrue();
        referenceValue.CreatedAt.Should().Be(createdAt);
        referenceValue.UpdatedAt.Should().Be(updatedAt);
    }

    [Fact]
    public void ReferenceValue_Category_ShouldAcceptDifferentTypes()
    {
        // Arrange & Act
        var statusReference = new ReferenceValue { Category = "influence_status" };
        var typeReference = new ReferenceValue { Category = "influence_type" };
        var channelReference = new ReferenceValue { Category = "communication_channel" };
        var sourceReference = new ReferenceValue { Category = "contact_source" };
        var riskSphereReference = new ReferenceValue { Category = "risk_sphere" };

        // Assert
        statusReference.Category.Should().Be("influence_status");
        typeReference.Category.Should().Be("influence_type");
        channelReference.Category.Should().Be("communication_channel");
        sourceReference.Category.Should().Be("contact_source");
        riskSphereReference.Category.Should().Be("risk_sphere");
    }

    [Fact]
    public void ReferenceValue_Code_ShouldAcceptAlphanumericValues()
    {
        // Arrange & Act
        var ref1 = new ReferenceValue { Code = "A" };
        var ref2 = new ReferenceValue { Code = "1" };
        var ref3 = new ReferenceValue { Code = "A1" };
        var ref4 = new ReferenceValue { Code = "STATUS_HIGH" };

        // Assert
        ref1.Code.Should().Be("A");
        ref2.Code.Should().Be("1");
        ref3.Code.Should().Be("A1");
        ref4.Code.Should().Be("STATUS_HIGH");
    }

    [Fact]
    public void ReferenceValue_SortOrder_ShouldAllowOrdering()
    {
        // Arrange
        var values = new List<ReferenceValue>
        {
            new ReferenceValue { SortOrder = 3, Name = "Third" },
            new ReferenceValue { SortOrder = 1, Name = "First" },
            new ReferenceValue { SortOrder = 2, Name = "Second" }
        };

        // Act
        var sorted = values.OrderBy(r => r.SortOrder).ToList();

        // Assert
        sorted[0].Name.Should().Be("First");
        sorted[1].Name.Should().Be("Second");
        sorted[2].Name.Should().Be("Third");
    }

    [Fact]
    public void ReferenceValue_IsActive_ShouldSupportSoftDelete()
    {
        // Arrange
        var referenceValue = new ReferenceValue
        {
            Name = "Old Value",
            IsActive = true
        };

        // Act - Deactivate
        referenceValue.IsActive = false;
        referenceValue.UpdatedAt = DateTime.UtcNow;

        // Assert
        referenceValue.IsActive.Should().BeFalse();
        referenceValue.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void ReferenceValue_Description_CanBeNull()
    {
        // Arrange & Act
        var referenceValue = new ReferenceValue
        {
            Category = "test_category",
            Code = "TEST",
            Name = "Test Value",
            Description = null
        };

        // Assert
        referenceValue.Description.Should().BeNull();
    }

    [Fact]
    public void ReferenceValue_Description_CanContainLongText()
    {
        // Arrange
        var longDescription = new string('A', 1000);

        // Act
        var referenceValue = new ReferenceValue
        {
            Description = longDescription
        };

        // Assert
        referenceValue.Description.Should().HaveLength(1000);
    }

    [Fact]
    public void ReferenceValue_CreatedAt_ShouldNotChangeOnUpdate()
    {
        // Arrange
        var originalCreatedAt = DateTime.UtcNow.AddDays(-30);
        var referenceValue = new ReferenceValue
        {
            Name = "Original Name",
            CreatedAt = originalCreatedAt,
            UpdatedAt = originalCreatedAt
        };

        // Act - Simulate update
        referenceValue.Name = "Updated Name";
        referenceValue.UpdatedAt = DateTime.UtcNow;

        // Assert
        referenceValue.CreatedAt.Should().Be(originalCreatedAt);
        referenceValue.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void ReferenceValue_ShouldSupportMultipleValuesInSameCategory()
    {
        // Arrange & Act
        var values = new List<ReferenceValue>
        {
            new ReferenceValue { Category = "influence_status", Code = "A", Name = "Уровень А", SortOrder = 1 },
            new ReferenceValue { Category = "influence_status", Code = "B", Name = "Уровень Б", SortOrder = 2 },
            new ReferenceValue { Category = "influence_status", Code = "C", Name = "Уровень В", SortOrder = 3 },
            new ReferenceValue { Category = "influence_status", Code = "D", Name = "Уровень Г", SortOrder = 4 }
        };

        // Assert
        values.Should().HaveCount(4);
        values.All(v => v.Category == "influence_status").Should().BeTrue();
        values.Select(v => v.Code).Should().BeEquivalentTo(new[] { "A", "B", "C", "D" });
    }

    [Fact]
    public void ReferenceValue_Name_ShouldSupportCyrillicCharacters()
    {
        // Arrange & Act
        var referenceValue = new ReferenceValue
        {
            Name = "Уровень влияния А",
            Description = "Высший уровень, прямое влияние на ключевые решения"
        };

        // Assert
        referenceValue.Name.Should().Be("Уровень влияния А");
        referenceValue.Description.Should().Contain("влияние");
    }

    [Fact]
    public void ReferenceValue_ShouldSupportZeroSortOrder()
    {
        // Arrange & Act
        var referenceValue = new ReferenceValue
        {
            SortOrder = 0
        };

        // Assert
        referenceValue.SortOrder.Should().Be(0);
    }

    [Fact]
    public void ReferenceValue_ShouldSupportNegativeSortOrder()
    {
        // Arrange & Act
        var referenceValue = new ReferenceValue
        {
            SortOrder = -1
        };

        // Assert
        referenceValue.SortOrder.Should().Be(-1);
    }

    [Theory]
    [InlineData("influence_status")]
    [InlineData("influence_type")]
    [InlineData("communication_channel")]
    [InlineData("contact_source")]
    [InlineData("risk_sphere")]
    public void ReferenceValue_Category_ShouldAcceptValidCategories(string category)
    {
        // Arrange & Act
        var referenceValue = new ReferenceValue
        {
            Category = category
        };

        // Assert
        referenceValue.Category.Should().Be(category);
    }
}
