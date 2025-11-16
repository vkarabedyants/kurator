using Xunit;
using FluentAssertions;
using Kurator.Core.Entities;

namespace Kurator.Tests.Entities;

public class ContactEntityTests
{
    [Fact]
    public void Contact_DefaultValues_ShouldBeSetCorrectly()
    {
        // Arrange & Act
        var contact = new Contact();

        // Assert
        contact.IsActive.Should().BeTrue();
        contact.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        contact.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        contact.Interactions.Should().NotBeNull().And.BeEmpty();
        contact.StatusHistory.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void Contact_WithBasicInfo_ShouldStoreCorrectly()
    {
        // Arrange
        var contact = new Contact
        {
            ContactId = "OP-001",
            BlockId = 1,
            FullNameEncrypted = "encrypted_name",
            Position = "Директор",
            ResponsibleCuratorId = 1
        };

        // Act & Assert
        contact.ContactId.Should().Be("OP-001");
        contact.BlockId.Should().Be(1);
        contact.FullNameEncrypted.Should().Be("encrypted_name");
        contact.Position.Should().Be("Директор");
        contact.ResponsibleCuratorId.Should().Be(1);
    }

    [Fact]
    public void Contact_WithEncryptedData_ShouldStoreCorrectly()
    {
        // Arrange
        var contact = new Contact
        {
            ContactId = "SIL-042",
            FullNameEncrypted = "encrypted_full_name_data",
            NotesEncrypted = "encrypted_notes_data"
        };

        // Act & Assert
        contact.FullNameEncrypted.Should().Be("encrypted_full_name_data");
        contact.NotesEncrypted.Should().Be("encrypted_notes_data");
    }

    [Fact]
    public void Contact_WithDates_ShouldStoreCorrectly()
    {
        // Arrange
        var lastInteractionDate = DateTime.UtcNow.AddDays(-5);
        var nextTouchDate = DateTime.UtcNow.AddDays(10);

        var contact = new Contact
        {
            ContactId = "OP-100",
            LastInteractionDate = lastInteractionDate,
            NextTouchDate = nextTouchDate
        };

        // Act & Assert
        contact.LastInteractionDate.Should().Be(lastInteractionDate);
        contact.NextTouchDate.Should().Be(nextTouchDate);
    }

    [Fact]
    public void Contact_WithReferenceIds_ShouldStoreCorrectly()
    {
        // Arrange
        var contact = new Contact
        {
            ContactId = "OP-200",
            OrganizationId = 5,
            InfluenceStatusId = 1, // A
            InfluenceTypeId = 2,
            CommunicationChannelId = 3,
            ContactSourceId = 4
        };

        // Act & Assert
        contact.OrganizationId.Should().Be(5);
        contact.InfluenceStatusId.Should().Be(1);
        contact.InfluenceTypeId.Should().Be(2);
        contact.CommunicationChannelId.Should().Be(3);
        contact.ContactSourceId.Should().Be(4);
    }

    [Fact]
    public void Contact_ContactIdFormat_ShouldFollowPattern()
    {
        // Arrange
        var validFormats = new[] { "OP-001", "SIL-123", "TECH-999" };

        foreach (var format in validFormats)
        {
            // Act
            var contact = new Contact { ContactId = format };

            // Assert
            contact.ContactId.Should().Contain("-");
            var parts = contact.ContactId.Split('-');
            parts.Should().HaveCount(2);
            parts[1].Should().MatchRegex(@"^\d{3}$");
        }
    }
}
