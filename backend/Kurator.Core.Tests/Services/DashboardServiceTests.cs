using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Kurator.Core.Entities;
using Kurator.Core.Enums;
using Kurator.Core.Interfaces;
using Kurator.Infrastructure.Services;
using Kurator.Infrastructure.Data;

namespace Kurator.Core.Tests.Services;

public class DashboardServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<DashboardService> _logger;
    private readonly DashboardService _service;
    private readonly User _adminUser;
    private readonly User _curatorUser;
    private readonly Block _testBlock;

    public DashboardServiceTests()
    {
        // Setup InMemory database
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);

        // Setup encryption
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                {"Encryption:Key", "test-key-32-characters-long!!"}
            }!)
            .Build();
        _encryptionService = new Infrastructure.Services.EncryptionService(config);

        // Setup logger
        _logger = new Mock<ILogger<DashboardService>>().Object;

        // Create service
        _service = new DashboardService(_context, _encryptionService, _logger);

        // Setup test data
        _adminUser = new User
        {
            Login = "admin",
            PasswordHash = "hash",
            Role = UserRole.Admin,
            IsActive = true
        };

        _curatorUser = new User
        {
            Login = "curator",
            PasswordHash = "hash",
            Role = UserRole.Curator,
            IsActive = true
        };

        _testBlock = new Block
        {
            Name = "Test Block",
            Code = "TEST",
            Status = BlockStatus.Active
        };

        _context.Users.Add(_adminUser);
        _context.Users.Add(_curatorUser);
        _context.Blocks.Add(_testBlock);
        _context.SaveChanges();

        // Assign curator to block
        var blockCurator = new BlockCurator
        {
            BlockId = _testBlock.Id,
            UserId = _curatorUser.Id,
            AssignedAt = DateTime.UtcNow
        };
        _context.BlockCurators.Add(blockCurator);
        _context.SaveChanges();
    }

    #region GetCuratorDashboardAsync Tests

    [Fact]
    public async Task GetCuratorDashboardAsync_WithNoBlocks_ShouldReturnEmptyMetrics()
    {
        // Arrange
        var userWithoutBlocks = new User
        {
            Login = "no-blocks-user",
            PasswordHash = "hash",
            Role = UserRole.Curator,
            IsActive = true
        };
        _context.Users.Add(userWithoutBlocks);
        await _context.SaveChangesAsync();

        // Act
        var dashboard = await _service.GetCuratorDashboardAsync(userWithoutBlocks.Id, isAdmin: false);

        // Assert
        dashboard.TotalContacts.Should().Be(0);
        dashboard.InteractionsLastMonth.Should().Be(0);
        dashboard.OverdueContacts.Should().Be(0);
    }

    [Fact]
    public async Task GetCuratorDashboardAsync_ShouldCountTotalContacts()
    {
        // Arrange
        for (int i = 0; i < 5; i++)
        {
            var contact = CreateTestContact($"Contact {i}", _testBlock.Id);
            _context.Contacts.Add(contact);
        }
        await _context.SaveChangesAsync();

        // Act
        var dashboard = await _service.GetCuratorDashboardAsync(_curatorUser.Id, isAdmin: false);

        // Assert
        dashboard.TotalContacts.Should().Be(5);
    }

    [Fact]
    public async Task GetCuratorDashboardAsync_ShouldCountInteractionsLastMonth()
    {
        // Arrange
        var contact = CreateTestContact("Test Contact", _testBlock.Id);
        _context.Contacts.Add(contact);
        await _context.SaveChangesAsync();

        // Recent interaction
        var recentInteraction = new Interaction
        {
            ContactId = contact.Id,
            CuratorId = _curatorUser.Id,
            InteractionDate = DateTime.UtcNow.AddDays(-5),
            IsActive = true,
            UpdatedBy = _curatorUser.Id
        };

        // Old interaction
        var oldInteraction = new Interaction
        {
            ContactId = contact.Id,
            CuratorId = _curatorUser.Id,
            InteractionDate = DateTime.UtcNow.AddMonths(-2),
            IsActive = true,
            UpdatedBy = _curatorUser.Id
        };

        _context.Interactions.AddRange(recentInteraction, oldInteraction);
        await _context.SaveChangesAsync();

        // Act
        var dashboard = await _service.GetCuratorDashboardAsync(_curatorUser.Id, isAdmin: false);

        // Assert
        dashboard.InteractionsLastMonth.Should().Be(1);
    }

    [Fact]
    public async Task GetCuratorDashboardAsync_ShouldCalculateAverageInteractionInterval()
    {
        // Arrange
        var contact1 = CreateTestContact("Contact 1", _testBlock.Id);
        contact1.LastInteractionDate = DateTime.UtcNow.AddDays(-10);

        var contact2 = CreateTestContact("Contact 2", _testBlock.Id);
        contact2.LastInteractionDate = DateTime.UtcNow.AddDays(-20);

        _context.Contacts.AddRange(contact1, contact2);
        await _context.SaveChangesAsync();

        // Act
        var dashboard = await _service.GetCuratorDashboardAsync(_curatorUser.Id, isAdmin: false);

        // Assert
        dashboard.AverageInteractionInterval.Should().BeGreaterThan(0);
        dashboard.AverageInteractionInterval.Should().BeApproximately(15, 5);
    }

    [Fact]
    public async Task GetCuratorDashboardAsync_ShouldCountOverdueContacts()
    {
        // Arrange
        var overdueContact = CreateTestContact("Overdue Contact", _testBlock.Id);
        overdueContact.NextTouchDate = DateTime.UtcNow.AddDays(-5);

        var futureContact = CreateTestContact("Future Contact", _testBlock.Id);
        futureContact.NextTouchDate = DateTime.UtcNow.AddDays(5);

        _context.Contacts.AddRange(overdueContact, futureContact);
        await _context.SaveChangesAsync();

        // Act
        var dashboard = await _service.GetCuratorDashboardAsync(_curatorUser.Id, isAdmin: false);

        // Assert
        dashboard.OverdueContacts.Should().Be(1);
    }

    [Fact]
    public async Task GetCuratorDashboardAsync_ShouldReturnRecentInteractions()
    {
        // Arrange
        var contact = CreateTestContact("Test Contact", _testBlock.Id);
        _context.Contacts.Add(contact);
        await _context.SaveChangesAsync();

        for (int i = 0; i < 10; i++)
        {
            var interaction = new Interaction
            {
                ContactId = contact.Id,
                CuratorId = _curatorUser.Id,
                InteractionDate = DateTime.UtcNow.AddDays(-i),
                IsActive = true,
                UpdatedBy = _curatorUser.Id
            };
            _context.Interactions.Add(interaction);
        }
        await _context.SaveChangesAsync();

        // Act
        var dashboard = await _service.GetCuratorDashboardAsync(_curatorUser.Id, isAdmin: false);

        // Assert
        dashboard.RecentInteractions.Should().HaveCount(5); // Limited to 5
    }

    [Fact]
    public async Task GetCuratorDashboardAsync_ShouldReturnContactsRequiringAttention()
    {
        // Arrange
        for (int i = 1; i <= 15; i++)
        {
            var contact = CreateTestContact($"Overdue Contact {i}", _testBlock.Id);
            contact.NextTouchDate = DateTime.UtcNow.AddDays(-i);
            _context.Contacts.Add(contact);
        }
        await _context.SaveChangesAsync();

        // Act
        var dashboard = await _service.GetCuratorDashboardAsync(_curatorUser.Id, isAdmin: false);

        // Assert
        dashboard.ContactsRequiringAttention.Should().HaveCount(10); // Limited to 10
        dashboard.ContactsRequiringAttention.First().DaysOverdue.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetCuratorDashboardAsync_ShouldGroupContactsByInfluenceStatus()
    {
        // Arrange
        var contact1 = CreateTestContact("Contact 1", _testBlock.Id);
        contact1.InfluenceStatusId = 1;

        var contact2 = CreateTestContact("Contact 2", _testBlock.Id);
        contact2.InfluenceStatusId = 1;

        var contact3 = CreateTestContact("Contact 3", _testBlock.Id);
        contact3.InfluenceStatusId = 2;

        _context.Contacts.AddRange(contact1, contact2, contact3);
        await _context.SaveChangesAsync();

        // Act
        var dashboard = await _service.GetCuratorDashboardAsync(_curatorUser.Id, isAdmin: false);

        // Assert
        dashboard.ContactsByInfluenceStatus.Should().ContainKey("1");
        dashboard.ContactsByInfluenceStatus["1"].Should().Be(2);
        dashboard.ContactsByInfluenceStatus.Should().ContainKey("2");
        dashboard.ContactsByInfluenceStatus["2"].Should().Be(1);
    }

    [Fact]
    public async Task GetCuratorDashboardAsync_ShouldExcludeArchivedBlockContacts()
    {
        // Arrange
        var archivedBlock = new Block
        {
            Name = "Archived Block",
            Code = "ARCH",
            Status = BlockStatus.Archived
        };
        _context.Blocks.Add(archivedBlock);
        await _context.SaveChangesAsync();

        var blockCurator = new BlockCurator
        {
            BlockId = archivedBlock.Id,
            UserId = _curatorUser.Id,
            AssignedAt = DateTime.UtcNow
        };
        _context.BlockCurators.Add(blockCurator);
        await _context.SaveChangesAsync();

        var archivedContact = CreateTestContact("Archived Contact", archivedBlock.Id);
        _context.Contacts.Add(archivedContact);
        await _context.SaveChangesAsync();

        // Act
        var dashboard = await _service.GetCuratorDashboardAsync(_curatorUser.Id, isAdmin: false);

        // Assert
        dashboard.TotalContacts.Should().Be(0); // Should not count archived block contacts
    }

    #endregion

    #region GetAdminDashboardAsync Tests

    [Fact]
    public async Task GetAdminDashboardAsync_ShouldCountAllContacts()
    {
        // Arrange
        var block1 = new Block { Name = "Block 1", Code = "BLK1", Status = BlockStatus.Active };
        var block2 = new Block { Name = "Block 2", Code = "BLK2", Status = BlockStatus.Active };
        _context.Blocks.AddRange(block1, block2);
        await _context.SaveChangesAsync();

        for (int i = 0; i < 3; i++)
        {
            _context.Contacts.Add(CreateTestContact($"Contact {i}", block1.Id));
        }
        for (int i = 0; i < 2; i++)
        {
            _context.Contacts.Add(CreateTestContact($"Contact {i + 3}", block2.Id));
        }
        await _context.SaveChangesAsync();

        // Act
        var dashboard = await _service.GetAdminDashboardAsync();

        // Assert
        dashboard.TotalContacts.Should().Be(5);
    }

    [Fact]
    public async Task GetAdminDashboardAsync_ShouldCountAllInteractions()
    {
        // Arrange
        var contact = CreateTestContact("Test Contact", _testBlock.Id);
        _context.Contacts.Add(contact);
        await _context.SaveChangesAsync();

        for (int i = 0; i < 3; i++)
        {
            var interaction = new Interaction
            {
                ContactId = contact.Id,
                CuratorId = _curatorUser.Id,
                InteractionDate = DateTime.UtcNow.AddDays(-i),
                IsActive = true,
                UpdatedBy = _curatorUser.Id
            };
            _context.Interactions.Add(interaction);
        }
        await _context.SaveChangesAsync();

        // Act
        var dashboard = await _service.GetAdminDashboardAsync();

        // Assert
        dashboard.TotalInteractions.Should().Be(3);
    }

    [Fact]
    public async Task GetAdminDashboardAsync_ShouldCountActiveBlocks()
    {
        // Arrange
        var activeBlock = new Block { Name = "Active", Code = "ACT", Status = BlockStatus.Active };
        var archivedBlock = new Block { Name = "Archived", Code = "ARCH", Status = BlockStatus.Archived };
        _context.Blocks.AddRange(activeBlock, archivedBlock);
        await _context.SaveChangesAsync();

        // Act
        var dashboard = await _service.GetAdminDashboardAsync();

        // Assert
        dashboard.TotalBlocks.Should().Be(2); // Includes testBlock + activeBlock
    }

    [Fact]
    public async Task GetAdminDashboardAsync_ShouldCountAllUsers()
    {
        // Arrange
        var user1 = new User { Login = "user1", PasswordHash = "hash", Role = UserRole.Curator, IsActive = true };
        var user2 = new User { Login = "user2", PasswordHash = "hash", Role = UserRole.Curator, IsActive = true };
        _context.Users.AddRange(user1, user2);
        await _context.SaveChangesAsync();

        // Act
        var dashboard = await _service.GetAdminDashboardAsync();

        // Assert
        dashboard.TotalUsers.Should().Be(4); // adminUser + curatorUser + user1 + user2
    }

    [Fact]
    public async Task GetAdminDashboardAsync_ShouldGroupContactsByBlock()
    {
        // Arrange
        var block1 = new Block { Name = "Block 1", Code = "BLK1", Status = BlockStatus.Active };
        var block2 = new Block { Name = "Block 2", Code = "BLK2", Status = BlockStatus.Active };
        _context.Blocks.AddRange(block1, block2);
        await _context.SaveChangesAsync();

        _context.Contacts.Add(CreateTestContact("Contact 1", block1.Id));
        _context.Contacts.Add(CreateTestContact("Contact 2", block1.Id));
        _context.Contacts.Add(CreateTestContact("Contact 3", block2.Id));
        await _context.SaveChangesAsync();

        // Act
        var dashboard = await _service.GetAdminDashboardAsync();

        // Assert
        dashboard.ContactsByBlock.Should().ContainKey("Block 1");
        dashboard.ContactsByBlock["Block 1"].Should().Be(2);
        dashboard.ContactsByBlock.Should().ContainKey("Block 2");
        dashboard.ContactsByBlock["Block 2"].Should().Be(1);
    }

    [Fact]
    public async Task GetAdminDashboardAsync_ShouldShowTopCuratorsByActivity()
    {
        // Arrange
        var contact = CreateTestContact("Test Contact", _testBlock.Id);
        _context.Contacts.Add(contact);
        await _context.SaveChangesAsync();

        // Create interactions by different curators
        for (int i = 0; i < 5; i++)
        {
            var interaction = new Interaction
            {
                ContactId = contact.Id,
                CuratorId = _adminUser.Id,
                InteractionDate = DateTime.UtcNow.AddDays(-i),
                IsActive = true,
                UpdatedBy = _adminUser.Id
            };
            _context.Interactions.Add(interaction);
        }

        for (int i = 0; i < 3; i++)
        {
            var interaction = new Interaction
            {
                ContactId = contact.Id,
                CuratorId = _curatorUser.Id,
                InteractionDate = DateTime.UtcNow.AddDays(-i),
                IsActive = true,
                UpdatedBy = _curatorUser.Id
            };
            _context.Interactions.Add(interaction);
        }
        await _context.SaveChangesAsync();

        // Act
        var dashboard = await _service.GetAdminDashboardAsync();

        // Assert
        dashboard.TopCuratorsByActivity.Should().ContainKey("admin");
        dashboard.TopCuratorsByActivity["admin"].Should().Be(5);
        dashboard.TopCuratorsByActivity.Should().ContainKey("curator");
        dashboard.TopCuratorsByActivity["curator"].Should().Be(3);
    }

    [Fact]
    public async Task GetAdminDashboardAsync_ShouldShowStatusChangeDynamics()
    {
        // Arrange
        var contact = CreateTestContact("Test Contact", _testBlock.Id);
        _context.Contacts.Add(contact);
        await _context.SaveChangesAsync();

        var statusChange1 = new InfluenceStatusHistory
        {
            ContactId = contact.Id,
            PreviousStatus = "1",
            NewStatus = "2",
            ChangedByUserId = _adminUser.Id,
            ChangedAt = DateTime.UtcNow.AddMonths(-1)
        };

        var statusChange2 = new InfluenceStatusHistory
        {
            ContactId = contact.Id,
            PreviousStatus = "2",
            NewStatus = "3",
            ChangedByUserId = _adminUser.Id,
            ChangedAt = DateTime.UtcNow.AddDays(-5)
        };

        _context.InfluenceStatusHistories.AddRange(statusChange1, statusChange2);
        await _context.SaveChangesAsync();

        // Act
        var dashboard = await _service.GetAdminDashboardAsync();

        // Assert
        dashboard.StatusChangeDynamics.Should().ContainKey("1→2");
        dashboard.StatusChangeDynamics.Should().ContainKey("2→3");
    }

    [Fact]
    public async Task GetAdminDashboardAsync_ShouldShowRecentAuditLogs()
    {
        // Arrange
        for (int i = 0; i < 25; i++)
        {
            var auditLog = new AuditLog
            {
                UserId = _adminUser.Id,
                Action = AuditActionType.Create,
                EntityType = "Contact",
                EntityId = i.ToString(),
                Timestamp = DateTime.UtcNow.AddMinutes(-i)
            };
            _context.AuditLogs.Add(auditLog);
        }
        await _context.SaveChangesAsync();

        // Act
        var dashboard = await _service.GetAdminDashboardAsync();

        // Assert
        dashboard.RecentAuditLogs.Should().HaveCount(20); // Limited to 20
        dashboard.RecentAuditLogs.First().ActionType.Should().Be("Create");
    }

    #endregion

    #region GetStatisticsAsync Tests

    [Fact]
    public async Task GetStatisticsAsync_ShouldCountInteractionsInDateRange()
    {
        // Arrange
        var contact = CreateTestContact("Test Contact", _testBlock.Id);
        _context.Contacts.Add(contact);
        await _context.SaveChangesAsync();

        var interaction1 = new Interaction
        {
            ContactId = contact.Id,
            CuratorId = _curatorUser.Id,
            InteractionDate = DateTime.UtcNow.AddDays(-5),
            IsActive = true,
            UpdatedBy = _curatorUser.Id
        };

        var interaction2 = new Interaction
        {
            ContactId = contact.Id,
            CuratorId = _curatorUser.Id,
            InteractionDate = DateTime.UtcNow.AddDays(-10),
            IsActive = true,
            UpdatedBy = _curatorUser.Id
        };

        var outsideRangeInteraction = new Interaction
        {
            ContactId = contact.Id,
            CuratorId = _curatorUser.Id,
            InteractionDate = DateTime.UtcNow.AddMonths(-2),
            IsActive = true,
            UpdatedBy = _curatorUser.Id
        };

        _context.Interactions.AddRange(interaction1, interaction2, outsideRangeInteraction);
        await _context.SaveChangesAsync();

        // Act
        var stats = await _service.GetStatisticsAsync(
            _adminUser.Id,
            isAdmin: true,
            fromDate: DateTime.UtcNow.AddMonths(-1),
            toDate: DateTime.UtcNow);

        // Assert
        stats.TotalInteractions.Should().Be(2);
    }

    [Fact]
    public async Task GetStatisticsAsync_ShouldCountUniqueContacts()
    {
        // Arrange
        var contact1 = CreateTestContact("Contact 1", _testBlock.Id);
        var contact2 = CreateTestContact("Contact 2", _testBlock.Id);
        _context.Contacts.AddRange(contact1, contact2);
        await _context.SaveChangesAsync();

        // Multiple interactions with same contact
        for (int i = 0; i < 3; i++)
        {
            var interaction = new Interaction
            {
                ContactId = contact1.Id,
                CuratorId = _curatorUser.Id,
                InteractionDate = DateTime.UtcNow.AddDays(-i),
                IsActive = true,
                UpdatedBy = _curatorUser.Id
            };
            _context.Interactions.Add(interaction);
        }

        // One interaction with different contact
        var interaction2 = new Interaction
        {
            ContactId = contact2.Id,
            CuratorId = _curatorUser.Id,
            InteractionDate = DateTime.UtcNow,
            IsActive = true,
            UpdatedBy = _curatorUser.Id
        };
        _context.Interactions.Add(interaction2);
        await _context.SaveChangesAsync();

        // Act
        var stats = await _service.GetStatisticsAsync(_adminUser.Id, isAdmin: true);

        // Assert
        stats.UniqueContacts.Should().Be(2);
    }

    [Fact]
    public async Task GetStatisticsAsync_ShouldGroupByInteractionType()
    {
        // Arrange
        var contact = CreateTestContact("Test Contact", _testBlock.Id);
        _context.Contacts.Add(contact);
        await _context.SaveChangesAsync();

        var interaction1 = new Interaction
        {
            ContactId = contact.Id,
            CuratorId = _curatorUser.Id,
            InteractionDate = DateTime.UtcNow,
            InteractionTypeId = 1,
            IsActive = true,
            UpdatedBy = _curatorUser.Id
        };

        var interaction2 = new Interaction
        {
            ContactId = contact.Id,
            CuratorId = _curatorUser.Id,
            InteractionDate = DateTime.UtcNow,
            InteractionTypeId = 1,
            IsActive = true,
            UpdatedBy = _curatorUser.Id
        };

        var interaction3 = new Interaction
        {
            ContactId = contact.Id,
            CuratorId = _curatorUser.Id,
            InteractionDate = DateTime.UtcNow,
            InteractionTypeId = 2,
            IsActive = true,
            UpdatedBy = _curatorUser.Id
        };

        _context.Interactions.AddRange(interaction1, interaction2, interaction3);
        await _context.SaveChangesAsync();

        // Act
        var stats = await _service.GetStatisticsAsync(_adminUser.Id, isAdmin: true);

        // Assert
        stats.ByType.Should().ContainKey("1");
        stats.ByType["1"].Should().Be(2);
        stats.ByType.Should().ContainKey("2");
        stats.ByType["2"].Should().Be(1);
    }

    [Fact]
    public async Task GetStatisticsAsync_AsCurator_ShouldFilterByAssignedBlocks()
    {
        // Arrange
        var assignedContact = CreateTestContact("Assigned Contact", _testBlock.Id);

        var otherBlock = new Block { Name = "Other Block", Code = "OTHER", Status = BlockStatus.Active };
        _context.Blocks.Add(otherBlock);
        await _context.SaveChangesAsync();

        var otherContact = CreateTestContact("Other Contact", otherBlock.Id);

        _context.Contacts.AddRange(assignedContact, otherContact);
        await _context.SaveChangesAsync();

        var assignedInteraction = new Interaction
        {
            ContactId = assignedContact.Id,
            CuratorId = _curatorUser.Id,
            InteractionDate = DateTime.UtcNow,
            IsActive = true,
            UpdatedBy = _curatorUser.Id
        };

        var otherInteraction = new Interaction
        {
            ContactId = otherContact.Id,
            CuratorId = _adminUser.Id,
            InteractionDate = DateTime.UtcNow,
            IsActive = true,
            UpdatedBy = _adminUser.Id
        };

        _context.Interactions.AddRange(assignedInteraction, otherInteraction);
        await _context.SaveChangesAsync();

        // Act
        var stats = await _service.GetStatisticsAsync(_curatorUser.Id, isAdmin: false);

        // Assert
        stats.TotalInteractions.Should().Be(1);
    }

    #endregion

    #region Helper Methods

    private Contact CreateTestContact(string name, int blockId)
    {
        return new Contact
        {
            ContactId = $"TEST-{Guid.NewGuid().ToString().Substring(0, 3)}",
            BlockId = blockId,
            FullNameEncrypted = _encryptionService.Encrypt(name),
            ResponsibleCuratorId = _curatorUser.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = _curatorUser.Id
        };
    }

    #endregion

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
