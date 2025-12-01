using Xunit;
using FluentAssertions;
using Kurator.Core.Entities;
using Kurator.Core.Enums;

namespace Kurator.Core.Tests.Entities;

public class WatchlistHistoryTests
{
    [Fact]
    public void WatchlistHistory_ShouldInitializeWithDefaultValues()
    {
        // Act
        var history = new WatchlistHistory();

        // Assert
        history.Id.Should().Be(0);
        history.WatchlistId.Should().Be(0);
        history.OldRiskLevel.Should().BeNull();
        history.NewRiskLevel.Should().BeNull();
        history.ChangedBy.Should().Be(0);
        history.ChangedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        history.Comment.Should().BeNull();
        history.Watchlist.Should().BeNull();
        history.ChangedByUser.Should().BeNull();
    }

    [Fact]
    public void WatchlistHistory_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var changeTime = DateTime.UtcNow;

        // Act
        var history = new WatchlistHistory
        {
            Id = 1,
            WatchlistId = 10,
            OldRiskLevel = RiskLevel.Low,
            NewRiskLevel = RiskLevel.High,
            ChangedBy = 5,
            ChangedAt = changeTime,
            Comment = "Situation escalated"
        };

        // Assert
        history.Id.Should().Be(1);
        history.WatchlistId.Should().Be(10);
        history.OldRiskLevel.Should().Be(RiskLevel.Low);
        history.NewRiskLevel.Should().Be(RiskLevel.High);
        history.ChangedBy.Should().Be(5);
        history.ChangedAt.Should().Be(changeTime);
        history.Comment.Should().Be("Situation escalated");
    }

    [Fact]
    public void WatchlistHistory_ShouldTrackRiskLevelEscalation()
    {
        // Arrange & Act
        var history = new WatchlistHistory
        {
            WatchlistId = 1,
            OldRiskLevel = RiskLevel.Medium,
            NewRiskLevel = RiskLevel.High,
            ChangedBy = 1,
            ChangedAt = DateTime.UtcNow,
            Comment = "Increased media attention"
        };

        // Assert
        history.OldRiskLevel.Should().Be(RiskLevel.Medium);
        history.NewRiskLevel.Should().Be(RiskLevel.High);
        history.Comment.Should().Contain("Increased");
    }

    [Fact]
    public void WatchlistHistory_ShouldTrackRiskLevelDeescalation()
    {
        // Arrange & Act
        var history = new WatchlistHistory
        {
            WatchlistId = 1,
            OldRiskLevel = RiskLevel.Critical,
            NewRiskLevel = RiskLevel.High,
            ChangedBy = 1,
            ChangedAt = DateTime.UtcNow,
            Comment = "Situation improved"
        };

        // Assert
        history.OldRiskLevel.Should().Be(RiskLevel.Critical);
        history.NewRiskLevel.Should().Be(RiskLevel.High);
        history.Comment.Should().Contain("improved");
    }

    [Fact]
    public void WatchlistHistory_Comment_CanBeNull()
    {
        // Arrange & Act
        var history = new WatchlistHistory
        {
            WatchlistId = 1,
            OldRiskLevel = RiskLevel.Low,
            NewRiskLevel = RiskLevel.Medium,
            ChangedBy = 1,
            ChangedAt = DateTime.UtcNow,
            Comment = null
        };

        // Assert
        history.Comment.Should().BeNull();
    }

    [Fact]
    public void WatchlistHistory_Comment_ShouldSupportLongText()
    {
        // Arrange
        var longComment = new string('A', 1000);

        // Act
        var history = new WatchlistHistory
        {
            WatchlistId = 1,
            OldRiskLevel = RiskLevel.Low,
            NewRiskLevel = RiskLevel.High,
            ChangedBy = 1,
            ChangedAt = DateTime.UtcNow,
            Comment = longComment
        };

        // Assert
        history.Comment.Should().HaveLength(1000);
    }

    [Fact]
    public void WatchlistHistory_ShouldLinkToWatchlistItem()
    {
        // Arrange
        var watchlistItem = new Watchlist
        {
            Id = 1,
            FullName = "Test Person",
            RiskLevel = RiskLevel.Medium,
            MonitoringFrequency = MonitoringFrequency.Weekly,
            IsActive = true,
            UpdatedBy = 1
        };

        // Act
        var history = new WatchlistHistory
        {
            WatchlistId = watchlistItem.Id,
            Watchlist = watchlistItem,
            OldRiskLevel = RiskLevel.Low,
            NewRiskLevel = RiskLevel.Medium,
            ChangedBy = 1,
            ChangedAt = DateTime.UtcNow
        };

        // Assert
        history.Watchlist.Should().NotBeNull();
        history.Watchlist.Id.Should().Be(watchlistItem.Id);
        history.WatchlistId.Should().Be(watchlistItem.Id);
    }

    [Fact]
    public void WatchlistHistory_ShouldLinkToUser()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Login = "analyst1",
            PasswordHash = "hash",
            Role = UserRole.ThreatAnalyst
        };

        // Act
        var history = new WatchlistHistory
        {
            WatchlistId = 10,
            OldRiskLevel = RiskLevel.Low,
            NewRiskLevel = RiskLevel.High,
            ChangedBy = user.Id,
            ChangedByUser = user,
            ChangedAt = DateTime.UtcNow
        };

        // Assert
        history.ChangedByUser.Should().NotBeNull();
        history.ChangedByUser.Id.Should().Be(user.Id);
        history.ChangedByUser.Login.Should().Be("analyst1");
        history.ChangedBy.Should().Be(user.Id);
    }

    [Fact]
    public void WatchlistHistory_ShouldSupportMultipleChangesForSameWatchlistItem()
    {
        // Arrange
        var watchlistId = 1;

        // Act
        var changes = new List<WatchlistHistory>
        {
            new WatchlistHistory
            {
                WatchlistId = watchlistId,
                OldRiskLevel = RiskLevel.Low,
                NewRiskLevel = RiskLevel.Medium,
                ChangedBy = 1,
                ChangedAt = DateTime.UtcNow.AddMonths(-6),
                Comment = "Initial escalation"
            },
            new WatchlistHistory
            {
                WatchlistId = watchlistId,
                OldRiskLevel = RiskLevel.Medium,
                NewRiskLevel = RiskLevel.High,
                ChangedBy = 1,
                ChangedAt = DateTime.UtcNow.AddMonths(-3),
                Comment = "Further escalation"
            },
            new WatchlistHistory
            {
                WatchlistId = watchlistId,
                OldRiskLevel = RiskLevel.High,
                NewRiskLevel = RiskLevel.Critical,
                ChangedBy = 1,
                ChangedAt = DateTime.UtcNow,
                Comment = "Critical situation"
            }
        };

        // Assert
        changes.Should().HaveCount(3);
        changes.All(c => c.WatchlistId == watchlistId).Should().BeTrue();

        // Verify progression
        var sorted = changes.OrderBy(c => c.ChangedAt).ToList();
        sorted[0].NewRiskLevel.Should().Be(RiskLevel.Medium);
        sorted[1].NewRiskLevel.Should().Be(RiskLevel.High);
        sorted[2].NewRiskLevel.Should().Be(RiskLevel.Critical);
    }

    [Theory]
    [InlineData(RiskLevel.Low, RiskLevel.Medium)]
    [InlineData(RiskLevel.Medium, RiskLevel.High)]
    [InlineData(RiskLevel.High, RiskLevel.Critical)]
    [InlineData(RiskLevel.Critical, RiskLevel.High)]
    [InlineData(RiskLevel.High, RiskLevel.Medium)]
    [InlineData(RiskLevel.Medium, RiskLevel.Low)]
    public void WatchlistHistory_ShouldSupportAllRiskLevelTransitions(RiskLevel oldLevel, RiskLevel newLevel)
    {
        // Arrange & Act
        var history = new WatchlistHistory
        {
            WatchlistId = 1,
            OldRiskLevel = oldLevel,
            NewRiskLevel = newLevel,
            ChangedBy = 1,
            ChangedAt = DateTime.UtcNow
        };

        // Assert
        history.OldRiskLevel.Should().Be(oldLevel);
        history.NewRiskLevel.Should().Be(newLevel);
    }

    [Fact]
    public void WatchlistHistory_OldRiskLevel_CanBeNull()
    {
        // Arrange & Act - Initial risk level assignment
        var history = new WatchlistHistory
        {
            WatchlistId = 1,
            OldRiskLevel = null,
            NewRiskLevel = RiskLevel.Medium,
            ChangedBy = 1,
            ChangedAt = DateTime.UtcNow,
            Comment = "Initial risk assessment"
        };

        // Assert
        history.OldRiskLevel.Should().BeNull();
        history.NewRiskLevel.Should().Be(RiskLevel.Medium);
    }

    [Fact]
    public void WatchlistHistory_NewRiskLevel_CanBeNull()
    {
        // Arrange & Act - Risk level removed
        var history = new WatchlistHistory
        {
            WatchlistId = 1,
            OldRiskLevel = RiskLevel.Low,
            NewRiskLevel = null,
            ChangedBy = 1,
            ChangedAt = DateTime.UtcNow,
            Comment = "Risk level cleared"
        };

        // Assert
        history.OldRiskLevel.Should().Be(RiskLevel.Low);
        history.NewRiskLevel.Should().BeNull();
    }

    [Fact]
    public void WatchlistHistory_ShouldBeOrderableByChangedAt()
    {
        // Arrange
        var baseDate = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var histories = new List<WatchlistHistory>
        {
            new WatchlistHistory
            {
                WatchlistId = 1,
                ChangedAt = baseDate.AddDays(-5),
                NewRiskLevel = RiskLevel.High,
                ChangedBy = 1
            },
            new WatchlistHistory
            {
                WatchlistId = 1,
                ChangedAt = baseDate.AddDays(-10),
                NewRiskLevel = RiskLevel.Medium,
                ChangedBy = 1
            },
            new WatchlistHistory
            {
                WatchlistId = 1,
                ChangedAt = baseDate.AddDays(-7),
                NewRiskLevel = RiskLevel.Medium,
                ChangedBy = 1
            }
        };

        // Act
        var sorted = histories.OrderBy(h => h.ChangedAt).ToList();

        // Assert
        sorted[0].ChangedAt.Should().Be(baseDate.AddDays(-10));
        sorted[1].ChangedAt.Should().Be(baseDate.AddDays(-7));
        sorted[2].ChangedAt.Should().Be(baseDate.AddDays(-5));
    }

    [Fact]
    public void WatchlistHistory_ShouldTrackDifferentUsersChangingRiskLevel()
    {
        // Arrange & Act
        var changes = new List<WatchlistHistory>
        {
            new WatchlistHistory
            {
                WatchlistId = 1,
                OldRiskLevel = RiskLevel.Low,
                NewRiskLevel = RiskLevel.Medium,
                ChangedBy = 1,
                ChangedAt = DateTime.UtcNow.AddDays(-10),
                Comment = "First analyst assessment"
            },
            new WatchlistHistory
            {
                WatchlistId = 1,
                OldRiskLevel = RiskLevel.Medium,
                NewRiskLevel = RiskLevel.High,
                ChangedBy = 2,
                ChangedAt = DateTime.UtcNow,
                Comment = "Second analyst escalation"
            }
        };

        // Assert
        changes[0].ChangedBy.Should().Be(1);
        changes[1].ChangedBy.Should().Be(2);
    }

    [Fact]
    public void WatchlistHistory_Comment_ShouldSupportCyrillicCharacters()
    {
        // Arrange & Act
        var history = new WatchlistHistory
        {
            WatchlistId = 1,
            OldRiskLevel = RiskLevel.Medium,
            NewRiskLevel = RiskLevel.High,
            ChangedBy = 1,
            ChangedAt = DateTime.UtcNow,
            Comment = "Ситуация обострилась, усилена активность в СМИ"
        };

        // Assert
        history.Comment.Should().Contain("Ситуация");
        history.Comment.Should().Contain("СМИ");
    }

    [Fact]
    public void WatchlistHistory_NavigationProperties_ShouldBeNullByDefault()
    {
        // Arrange & Act
        var history = new WatchlistHistory
        {
            WatchlistId = 1,
            ChangedBy = 1
        };

        // Assert
        history.Watchlist.Should().BeNull();
        history.ChangedByUser.Should().BeNull();
    }

    [Fact]
    public void WatchlistHistory_ShouldCreateCompleteHistoryRecord()
    {
        // Arrange
        var watchlistItem = new Watchlist
        {
            Id = 1,
            FullName = "Test Person",
            RiskLevel = RiskLevel.High,
            MonitoringFrequency = MonitoringFrequency.Weekly,
            IsActive = true,
            UpdatedBy = 1
        };

        var user = new User
        {
            Id = 1,
            Login = "analyst1",
            PasswordHash = "hash",
            Role = UserRole.ThreatAnalyst
        };

        var changeTime = DateTime.UtcNow;

        // Act
        var history = new WatchlistHistory
        {
            Id = 1,
            WatchlistId = watchlistItem.Id,
            Watchlist = watchlistItem,
            OldRiskLevel = RiskLevel.Medium,
            NewRiskLevel = RiskLevel.High,
            ChangedBy = user.Id,
            ChangedByUser = user,
            ChangedAt = changeTime,
            Comment = "Risk level increased due to recent events"
        };

        // Assert
        history.Should().NotBeNull();
        history.Id.Should().Be(1);
        history.WatchlistId.Should().Be(watchlistItem.Id);
        history.Watchlist.Should().Be(watchlistItem);
        history.OldRiskLevel.Should().Be(RiskLevel.Medium);
        history.NewRiskLevel.Should().Be(RiskLevel.High);
        history.ChangedBy.Should().Be(user.Id);
        history.ChangedByUser.Should().Be(user);
        history.ChangedAt.Should().Be(changeTime);
        history.Comment.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void WatchlistHistory_ShouldAllowSameRiskLevelChange()
    {
        // Arrange & Act - Risk level might be "re-confirmed"
        var history = new WatchlistHistory
        {
            WatchlistId = 1,
            OldRiskLevel = RiskLevel.High,
            NewRiskLevel = RiskLevel.High,
            ChangedBy = 1,
            ChangedAt = DateTime.UtcNow,
            Comment = "Risk level re-confirmed after review"
        };

        // Assert
        history.OldRiskLevel.Should().Be(RiskLevel.High);
        history.NewRiskLevel.Should().Be(RiskLevel.High);
        history.Comment.Should().Contain("re-confirmed");
    }

    [Fact]
    public void WatchlistHistory_ShouldTrackRiskLevelHistoryOverTime()
    {
        // Arrange
        var watchlistId = 1;
        var baseTime = DateTime.UtcNow.AddYears(-1);

        // Act - Create a year's worth of history
        var histories = new List<WatchlistHistory>
        {
            new WatchlistHistory
            {
                WatchlistId = watchlistId,
                OldRiskLevel = null,
                NewRiskLevel = RiskLevel.Low,
                ChangedBy = 1,
                ChangedAt = baseTime,
                Comment = "Initial assessment"
            },
            new WatchlistHistory
            {
                WatchlistId = watchlistId,
                OldRiskLevel = RiskLevel.Low,
                NewRiskLevel = RiskLevel.Medium,
                ChangedBy = 1,
                ChangedAt = baseTime.AddMonths(3),
                Comment = "Escalation Q1"
            },
            new WatchlistHistory
            {
                WatchlistId = watchlistId,
                OldRiskLevel = RiskLevel.Medium,
                NewRiskLevel = RiskLevel.High,
                ChangedBy = 1,
                ChangedAt = baseTime.AddMonths(6),
                Comment = "Escalation Q2"
            },
            new WatchlistHistory
            {
                WatchlistId = watchlistId,
                OldRiskLevel = RiskLevel.High,
                NewRiskLevel = RiskLevel.Medium,
                ChangedBy = 1,
                ChangedAt = baseTime.AddMonths(9),
                Comment = "De-escalation Q3"
            },
            new WatchlistHistory
            {
                WatchlistId = watchlistId,
                OldRiskLevel = RiskLevel.Medium,
                NewRiskLevel = RiskLevel.Low,
                ChangedBy = 1,
                ChangedAt = baseTime.AddMonths(12),
                Comment = "Situation resolved"
            }
        };

        // Assert
        histories.Should().HaveCount(5);
        histories.All(h => h.WatchlistId == watchlistId).Should().BeTrue();

        var timeline = histories.OrderBy(h => h.ChangedAt).ToList();
        timeline[0].NewRiskLevel.Should().Be(RiskLevel.Low);
        timeline[4].NewRiskLevel.Should().Be(RiskLevel.Low);
        timeline[2].NewRiskLevel.Should().Be(RiskLevel.High); // Peak at Q2
    }
}
