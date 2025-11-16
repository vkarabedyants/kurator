using Xunit;
using FluentAssertions;
using Kurator.Core.Entities;

namespace Kurator.Core.Tests.Entities;

/// <summary>
/// Тесты для сущности Contact
/// </summary>
public class ContactTests
{
    [Fact]
    public void Contact_ShouldHaveDefaultValues()
    {
        // Act
        var contact = new Contact();

        // Assert
        contact.IsActive.Should().BeTrue();
        contact.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        contact.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Contact_ShouldAllowSettingContactId()
    {
        // Arrange
        var contactId = "OP-001";

        // Act
        var contact = new Contact { ContactId = contactId };

        // Assert
        contact.ContactId.Should().Be(contactId);
    }

    [Theory]
    [InlineData("OP-001")]
    [InlineData("MEDIA-042")]
    [InlineData("SIL-999")]
    public void Contact_ShouldSupportVariousContactIdFormats(string contactId)
    {
        // Arrange & Act
        var contact = new Contact { ContactId = contactId };

        // Assert
        contact.ContactId.Should().MatchRegex(@"^[A-Z]+-\d{3}$");
    }

    [Fact]
    public void Contact_NavigationProperties_ShouldBeInitialized()
    {
        // Act
        var contact = new Contact();

        // Assert
        contact.Interactions.Should().NotBeNull();
        contact.StatusHistory.Should().NotBeNull();
    }

    [Fact]
    public void Contact_ShouldStoreEncryptedFullName()
    {
        // Arrange
        var encryptedName = "encrypted_name_base64_string";

        // Act
        var contact = new Contact { FullNameEncrypted = encryptedName };

        // Assert
        contact.FullNameEncrypted.Should().Be(encryptedName);
    }

    [Fact]
    public void Contact_ShouldAllowNullableFields()
    {
        // Act
        var contact = new Contact
        {
            OrganizationId = null,
            Position = null,
            InfluenceStatusId = null,
            InfluenceTypeId = null,
            NextTouchDate = null
        };

        // Assert
        contact.OrganizationId.Should().BeNull();
        contact.Position.Should().BeNull();
        contact.NextTouchDate.Should().BeNull();
    }

    [Fact]
    public void Contact_ShouldTrackUpdateInformation()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var userId = 42;

        // Act
        var contact = new Contact
        {
            UpdatedAt = now,
            UpdatedBy = userId
        };

        // Assert
        contact.UpdatedAt.Should().Be(now);
        contact.UpdatedBy.Should().Be(userId);
    }
}
