using Xunit;
using FluentAssertions;
using Kurator.Core.Entities;
using Kurator.Core.Enums;

namespace Kurator.Core.Tests.Entities;

public class InfluenceStatusHistoryTests
{
    [Fact]
    public void InfluenceStatusHistory_ShouldInitializeWithDefaultValues()
    {
        // Act
        var history = new InfluenceStatusHistory();

        // Assert
        history.Id.Should().Be(0);
        history.ContactId.Should().Be(0);
        history.PreviousStatus.Should().Be(string.Empty);
        history.NewStatus.Should().Be(string.Empty);
        history.ChangedByUserId.Should().Be(0);
        history.ChangedAt.Should().Be(default(DateTime));
        history.Contact.Should().BeNull();
        history.ChangedBy.Should().BeNull();
    }

    [Fact]
    public void InfluenceStatusHistory_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var changeTime = DateTime.UtcNow;

        // Act
        var history = new InfluenceStatusHistory
        {
            Id = 1,
            ContactId = 10,
            PreviousStatus = "A",
            NewStatus = "B",
            ChangedByUserId = 5,
            ChangedAt = changeTime
        };

        // Assert
        history.Id.Should().Be(1);
        history.ContactId.Should().Be(10);
        history.PreviousStatus.Should().Be("A");
        history.NewStatus.Should().Be("B");
        history.ChangedByUserId.Should().Be(5);
        history.ChangedAt.Should().Be(changeTime);
    }

    [Fact]
    public void InfluenceStatusHistory_ShouldTrackStatusUpgrade()
    {
        // Arrange & Act
        var history = new InfluenceStatusHistory
        {
            ContactId = 1,
            PreviousStatus = "C",
            NewStatus = "B",
            ChangedByUserId = 1,
            ChangedAt = DateTime.UtcNow
        };

        // Assert
        history.PreviousStatus.Should().Be("C");
        history.NewStatus.Should().Be("B");
        // B is higher than C (A > B > C > D in influence hierarchy)
    }

    [Fact]
    public void InfluenceStatusHistory_ShouldTrackStatusDowngrade()
    {
        // Arrange & Act
        var history = new InfluenceStatusHistory
        {
            ContactId = 1,
            PreviousStatus = "A",
            NewStatus = "C",
            ChangedByUserId = 1,
            ChangedAt = DateTime.UtcNow
        };

        // Assert
        history.PreviousStatus.Should().Be("A");
        history.NewStatus.Should().Be("C");
        // C is lower than A (A > B > C > D in influence hierarchy)
    }

    [Fact]
    public void InfluenceStatusHistory_ShouldRecordTimestamp()
    {
        // Arrange
        var changeTime = DateTime.UtcNow;

        // Act
        var history = new InfluenceStatusHistory
        {
            ContactId = 1,
            PreviousStatus = "B",
            NewStatus = "A",
            ChangedByUserId = 1,
            ChangedAt = changeTime
        };

        // Assert
        history.ChangedAt.Should().BeCloseTo(changeTime, TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public void InfluenceStatusHistory_ShouldLinkToContact()
    {
        // Arrange
        var contact = new Contact
        {
            Id = 1,
            ContactId = "TEST-001",
            BlockId = 1,
            FullNameEncrypted = "encrypted",
            IsActive = true
        };

        // Act
        var history = new InfluenceStatusHistory
        {
            ContactId = contact.Id,
            Contact = contact,
            PreviousStatus = "C",
            NewStatus = "B",
            ChangedByUserId = 1,
            ChangedAt = DateTime.UtcNow
        };

        // Assert
        history.Contact.Should().NotBeNull();
        history.Contact.Id.Should().Be(contact.Id);
        history.ContactId.Should().Be(contact.Id);
    }

    [Fact]
    public void InfluenceStatusHistory_ShouldLinkToUser()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Login = "curator1",
            PasswordHash = "hash",
            Role = UserRole.Curator
        };

        // Act
        var history = new InfluenceStatusHistory
        {
            ContactId = 10,
            PreviousStatus = "D",
            NewStatus = "C",
            ChangedByUserId = user.Id,
            ChangedBy = user,
            ChangedAt = DateTime.UtcNow
        };

        // Assert
        history.ChangedBy.Should().NotBeNull();
        history.ChangedBy.Id.Should().Be(user.Id);
        history.ChangedBy.Login.Should().Be("curator1");
        history.ChangedByUserId.Should().Be(user.Id);
    }

    [Fact]
    public void InfluenceStatusHistory_ShouldSupportMultipleChangesForSameContact()
    {
        // Arrange
        var contactId = 1;

        // Act
        var changes = new List<InfluenceStatusHistory>
        {
            new InfluenceStatusHistory
            {
                ContactId = contactId,
                PreviousStatus = "D",
                NewStatus = "C",
                ChangedByUserId = 1,
                ChangedAt = DateTime.UtcNow.AddMonths(-6)
            },
            new InfluenceStatusHistory
            {
                ContactId = contactId,
                PreviousStatus = "C",
                NewStatus = "B",
                ChangedByUserId = 1,
                ChangedAt = DateTime.UtcNow.AddMonths(-3)
            },
            new InfluenceStatusHistory
            {
                ContactId = contactId,
                PreviousStatus = "B",
                NewStatus = "A",
                ChangedByUserId = 1,
                ChangedAt = DateTime.UtcNow
            }
        };

        // Assert
        changes.Should().HaveCount(3);
        changes.All(c => c.ContactId == contactId).Should().BeTrue();

        // Verify progression
        var sorted = changes.OrderBy(c => c.ChangedAt).ToList();
        sorted[0].NewStatus.Should().Be("C");
        sorted[1].NewStatus.Should().Be("B");
        sorted[2].NewStatus.Should().Be("A");
    }

    [Fact]
    public void InfluenceStatusHistory_ShouldAllowSameStatusChange()
    {
        // Arrange & Act - Sometimes status might be "re-confirmed"
        var history = new InfluenceStatusHistory
        {
            ContactId = 1,
            PreviousStatus = "B",
            NewStatus = "B",
            ChangedByUserId = 1,
            ChangedAt = DateTime.UtcNow
        };

        // Assert
        history.PreviousStatus.Should().Be("B");
        history.NewStatus.Should().Be("B");
    }

    [Theory]
    [InlineData("A", "B")]
    [InlineData("B", "C")]
    [InlineData("C", "D")]
    [InlineData("D", "C")]
    [InlineData("C", "B")]
    [InlineData("B", "A")]
    public void InfluenceStatusHistory_ShouldSupportAllStatusTransitions(string previousStatus, string newStatus)
    {
        // Arrange & Act
        var history = new InfluenceStatusHistory
        {
            ContactId = 1,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            ChangedByUserId = 1,
            ChangedAt = DateTime.UtcNow
        };

        // Assert
        history.PreviousStatus.Should().Be(previousStatus);
        history.NewStatus.Should().Be(newStatus);
    }

    [Fact]
    public void InfluenceStatusHistory_ShouldBeOrderableByChangedAt()
    {
        // Arrange
        var histories = new List<InfluenceStatusHistory>
        {
            new InfluenceStatusHistory
            {
                ContactId = 1,
                ChangedAt = DateTime.UtcNow.AddDays(-5),
                NewStatus = "Third"
            },
            new InfluenceStatusHistory
            {
                ContactId = 1,
                ChangedAt = DateTime.UtcNow.AddDays(-10),
                NewStatus = "First"
            },
            new InfluenceStatusHistory
            {
                ContactId = 1,
                ChangedAt = DateTime.UtcNow.AddDays(-7),
                NewStatus = "Second"
            }
        };

        // Act
        var sorted = histories.OrderBy(h => h.ChangedAt).ToList();

        // Assert
        sorted[0].NewStatus.Should().Be("First");
        sorted[1].NewStatus.Should().Be("Second");
        sorted[2].NewStatus.Should().Be("Third");
    }

    [Fact]
    public void InfluenceStatusHistory_ShouldTrackDifferentUsersChangingStatus()
    {
        // Arrange & Act
        var changes = new List<InfluenceStatusHistory>
        {
            new InfluenceStatusHistory
            {
                ContactId = 1,
                PreviousStatus = "C",
                NewStatus = "B",
                ChangedByUserId = 1,
                ChangedAt = DateTime.UtcNow.AddDays(-10)
            },
            new InfluenceStatusHistory
            {
                ContactId = 1,
                PreviousStatus = "B",
                NewStatus = "A",
                ChangedByUserId = 2,
                ChangedAt = DateTime.UtcNow
            }
        };

        // Assert
        changes[0].ChangedByUserId.Should().Be(1);
        changes[1].ChangedByUserId.Should().Be(2);
    }

    [Fact]
    public void InfluenceStatusHistory_ShouldSupportEmptyPreviousStatus()
    {
        // Arrange & Act - First status assignment
        var history = new InfluenceStatusHistory
        {
            ContactId = 1,
            PreviousStatus = string.Empty,
            NewStatus = "C",
            ChangedByUserId = 1,
            ChangedAt = DateTime.UtcNow
        };

        // Assert
        history.PreviousStatus.Should().BeEmpty();
        history.NewStatus.Should().Be("C");
    }

    [Fact]
    public void InfluenceStatusHistory_NavigationProperties_ShouldBeNullByDefault()
    {
        // Arrange & Act
        var history = new InfluenceStatusHistory
        {
            ContactId = 1,
            ChangedByUserId = 1
        };

        // Assert
        history.Contact.Should().BeNull();
        history.ChangedBy.Should().BeNull();
    }

    [Fact]
    public void InfluenceStatusHistory_ShouldCreateCompleteHistoryRecord()
    {
        // Arrange
        var contact = new Contact
        {
            Id = 1,
            ContactId = "TEST-001",
            BlockId = 1,
            FullNameEncrypted = "encrypted",
            IsActive = true
        };

        var user = new User
        {
            Id = 1,
            Login = "curator1",
            PasswordHash = "hash",
            Role = UserRole.Curator
        };

        var changeTime = DateTime.UtcNow;

        // Act
        var history = new InfluenceStatusHistory
        {
            Id = 1,
            ContactId = contact.Id,
            Contact = contact,
            PreviousStatus = "B",
            NewStatus = "A",
            ChangedByUserId = user.Id,
            ChangedBy = user,
            ChangedAt = changeTime
        };

        // Assert
        history.Should().NotBeNull();
        history.Id.Should().Be(1);
        history.ContactId.Should().Be(contact.Id);
        history.Contact.Should().Be(contact);
        history.PreviousStatus.Should().Be("B");
        history.NewStatus.Should().Be("A");
        history.ChangedByUserId.Should().Be(user.Id);
        history.ChangedBy.Should().Be(user);
        history.ChangedAt.Should().Be(changeTime);
    }
}
