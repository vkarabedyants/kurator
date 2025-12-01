using Xunit;
using FluentAssertions;
using Kurator.Core.Entities;

namespace Kurator.Core.Tests.Entities;

/// <summary>
/// Тесты для сущности Interaction (касание/взаимодействие)
/// </summary>
public class InteractionTests
{
    [Fact]
    public void Interaction_ShouldHaveDefaultValues()
    {
        // Act
        var interaction = new Interaction();

        // Assert
        interaction.IsActive.Should().BeTrue();
        interaction.InteractionDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        interaction.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        interaction.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        interaction.InteractionTypeId.Should().BeNull();
        interaction.ResultId.Should().BeNull();
        interaction.CommentEncrypted.Should().BeNull();
        interaction.StatusChangeJson.Should().BeNull();
        interaction.AttachmentsJson.Should().BeNull();
        interaction.NextTouchDate.Should().BeNull();
    }

    [Fact]
    public void Interaction_ShouldAllowSettingAllProperties()
    {
        // Arrange
        var specificTime = new DateTime(2024, 11, 20, 14, 30, 0, DateTimeKind.Utc);
        var nextTouchDate = specificTime.AddDays(7);

        // Act
        var interaction = new Interaction
        {
            ContactId = 1,
            InteractionDate = specificTime,
            InteractionTypeId = 2,
            CuratorId = 3,
            ResultId = 4,
            CommentEncrypted = "encrypted_comment_data",
            StatusChangeJson = "{\"oldStatus\":\"B\",\"newStatus\":\"A\"}",
            AttachmentsJson = "[\"path/to/file1.pdf\",\"path/to/file2.docx\"]",
            NextTouchDate = nextTouchDate,
            IsActive = true,
            CreatedAt = specificTime,
            UpdatedAt = specificTime.AddHours(1),
            UpdatedBy = 5
        };

        // Assert
        interaction.ContactId.Should().Be(1);
        interaction.InteractionDate.Should().Be(specificTime);
        interaction.InteractionTypeId.Should().Be(2);
        interaction.CuratorId.Should().Be(3);
        interaction.ResultId.Should().Be(4);
        interaction.CommentEncrypted.Should().Be("encrypted_comment_data");
        interaction.StatusChangeJson.Should().Be("{\"oldStatus\":\"B\",\"newStatus\":\"A\"}");
        interaction.AttachmentsJson.Should().Be("[\"path/to/file1.pdf\",\"path/to/file2.docx\"]");
        interaction.NextTouchDate.Should().Be(nextTouchDate);
        interaction.IsActive.Should().BeTrue();
        interaction.CreatedAt.Should().Be(specificTime);
        interaction.UpdatedAt.Should().Be(specificTime.AddHours(1));
        interaction.UpdatedBy.Should().Be(5);
    }

    [Fact]
    public void Interaction_ShouldSupportNullableReferenceFields()
    {
        // Act
        var interaction = new Interaction
        {
            ContactId = 1,
            CuratorId = 1,
            UpdatedBy = 1,
            InteractionTypeId = null,
            ResultId = null,
            CommentEncrypted = null,
            StatusChangeJson = null,
            AttachmentsJson = null,
            NextTouchDate = null
        };

        // Assert
        interaction.InteractionTypeId.Should().BeNull();
        interaction.ResultId.Should().BeNull();
        interaction.CommentEncrypted.Should().BeNull();
        interaction.StatusChangeJson.Should().BeNull();
        interaction.AttachmentsJson.Should().BeNull();
        interaction.NextTouchDate.Should().BeNull();
    }

    [Fact]
    public void Interaction_StatusChangeJson_ShouldSupportValidJsonFormat()
    {
        // Arrange
        var validJson = "{\"oldStatus\":\"C\",\"newStatus\":\"B\"}";
        var complexJson = "{\"oldStatus\":\"D\",\"newStatus\":\"A\",\"changedBy\":\"curator\",\"timestamp\":\"2024-11-20T14:30:00Z\"}";

        // Act
        var interaction1 = new Interaction { StatusChangeJson = validJson };
        var interaction2 = new Interaction { StatusChangeJson = complexJson };

        // Assert
        interaction1.StatusChangeJson.Should().Be(validJson);
        interaction2.StatusChangeJson.Should().Be(complexJson);

        // Проверяем, что JSON содержит ожидаемые поля
        interaction1.StatusChangeJson.Should().Contain("oldStatus");
        interaction1.StatusChangeJson.Should().Contain("newStatus");
        interaction2.StatusChangeJson.Should().Contain("changedBy");
    }

    [Fact]
    public void Interaction_AttachmentsJson_ShouldSupportMultipleFiles()
    {
        // Arrange
        var singleFileJson = "[\"path/to/document.pdf\"]";
        var multipleFilesJson = "[\"path/to/document1.pdf\",\"path/to/image.jpg\",\"path/to/spreadsheet.xlsx\"]";
        var emptyArrayJson = "[]";

        // Act
        var interaction1 = new Interaction { AttachmentsJson = singleFileJson };
        var interaction2 = new Interaction { AttachmentsJson = multipleFilesJson };
        var interaction3 = new Interaction { AttachmentsJson = emptyArrayJson };

        // Assert
        interaction1.AttachmentsJson.Should().Be(singleFileJson);
        interaction2.AttachmentsJson.Should().Be(multipleFilesJson);
        interaction3.AttachmentsJson.Should().Be(emptyArrayJson);
    }

    [Fact]
    public void Interaction_ShouldTrackTimestamps()
    {
        // Arrange
        var creationTime = DateTime.UtcNow.AddHours(-2);
        var updateTime = DateTime.UtcNow.AddHours(-1);
        var interactionTime = DateTime.UtcNow.AddHours(-3);

        // Act
        var interaction = new Interaction
        {
            InteractionDate = interactionTime,
            CreatedAt = creationTime,
            UpdatedAt = updateTime
        };

        // Assert
        interaction.InteractionDate.Should().Be(interactionTime);
        interaction.CreatedAt.Should().Be(creationTime);
        interaction.UpdatedAt.Should().Be(updateTime);
        interaction.UpdatedAt.Should().BeAfter(interaction.CreatedAt);
    }

    [Fact]
    public void Interaction_NextTouchDate_CanBeInFuture()
    {
        // Arrange
        var futureDate = DateTime.UtcNow.AddDays(30);

        // Act
        var interaction = new Interaction { NextTouchDate = futureDate };

        // Assert
        interaction.NextTouchDate.Should().Be(futureDate);
        interaction.NextTouchDate.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public void Interaction_NextTouchDate_CanBeInPast()
    {
        // Arrange
        var pastDate = DateTime.UtcNow.AddDays(-10);

        // Act
        var interaction = new Interaction { NextTouchDate = pastDate };

        // Assert
        interaction.NextTouchDate.Should().Be(pastDate);
        interaction.NextTouchDate.Should().BeBefore(DateTime.UtcNow);
    }

    [Fact]
    public void Interaction_ShouldSupportEncryptedComments()
    {
        // Arrange
        var encryptedComment = "U2FsdGVkX1+encrypted+data+here=="; // Base64-like encrypted string

        // Act
        var interaction = new Interaction { CommentEncrypted = encryptedComment };

        // Assert
        interaction.CommentEncrypted.Should().Be(encryptedComment);
        interaction.CommentEncrypted.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Interaction_CommentEncrypted_CanBeEmptyString()
    {
        // Act
        var interaction = new Interaction { CommentEncrypted = "" };

        // Assert
        interaction.CommentEncrypted.Should().BeEmpty();
    }

    [Fact]
    public void Interaction_ShouldSupportDeactivation()
    {
        // Act
        var activeInteraction = new Interaction { IsActive = true };
        var inactiveInteraction = new Interaction { IsActive = false };

        // Assert
        activeInteraction.IsActive.Should().BeTrue();
        inactiveInteraction.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Interaction_ShouldTrackWhoUpdated()
    {
        // Arrange
        var curatorId = 5;
        var updatedById = 10;

        // Act
        var interaction = new Interaction
        {
            CuratorId = curatorId,
            UpdatedBy = updatedById
        };

        // Assert
        interaction.CuratorId.Should().Be(curatorId);
        interaction.UpdatedBy.Should().Be(updatedById);
        // CuratorId и UpdatedBy могут быть разными пользователями
    }
}
