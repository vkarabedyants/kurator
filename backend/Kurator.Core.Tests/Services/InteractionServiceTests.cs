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

public class InteractionServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<InteractionService> _logger;
    private readonly InteractionService _service;
    private readonly User _adminUser;
    private readonly User _curatorUser;
    private readonly Block _testBlock;
    private readonly Contact _testContact;

    public InteractionServiceTests()
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
        _logger = new Mock<ILogger<InteractionService>>().Object;

        // Create service
        _service = new InteractionService(_context, _encryptionService, _logger);

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

        _testContact = new Contact
        {
            ContactId = "TEST-001",
            BlockId = _testBlock.Id,
            FullNameEncrypted = _encryptionService.Encrypt("Test Contact"),
            ResponsibleCuratorId = _curatorUser.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = _curatorUser.Id
        };
        _context.Contacts.Add(_testContact);

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

    #region GetInteractionsAsync Tests

    [Fact]
    public async Task GetInteractionsAsync_AsAdmin_ShouldReturnAllInteractions()
    {
        // Arrange
        var interaction1 = CreateTestInteraction(_testContact.Id, _curatorUser.Id);
        var interaction2 = CreateTestInteraction(_testContact.Id, _curatorUser.Id);
        _context.Interactions.AddRange(interaction1, interaction2);
        await _context.SaveChangesAsync();

        // Act
        var (interactions, total) = await _service.GetInteractionsAsync(_adminUser.Id, isAdmin: true);

        // Assert
        interactions.Should().HaveCount(2);
        total.Should().Be(2);
    }

    [Fact]
    public async Task GetInteractionsAsync_AsCurator_ShouldReturnOnlyAssignedBlockInteractions()
    {
        // Arrange
        var assignedInteraction = CreateTestInteraction(_testContact.Id, _curatorUser.Id);

        var otherBlock = new Block { Name = "Other Block", Code = "OTHER", Status = BlockStatus.Active };
        _context.Blocks.Add(otherBlock);
        await _context.SaveChangesAsync();

        var otherContact = new Contact
        {
            ContactId = "OTHER-001",
            BlockId = otherBlock.Id,
            FullNameEncrypted = _encryptionService.Encrypt("Other Contact"),
            ResponsibleCuratorId = _adminUser.Id,
            IsActive = true,
            UpdatedBy = _adminUser.Id
        };
        _context.Contacts.Add(otherContact);
        await _context.SaveChangesAsync();

        var otherInteraction = CreateTestInteraction(otherContact.Id, _adminUser.Id);

        _context.Interactions.AddRange(assignedInteraction, otherInteraction);
        await _context.SaveChangesAsync();

        // Act
        var (interactions, total) = await _service.GetInteractionsAsync(_curatorUser.Id, isAdmin: false);

        // Assert
        interactions.Should().HaveCount(1);
        interactions.First().ContactId.Should().Be(_testContact.Id);
    }

    [Fact]
    public async Task GetInteractionsAsync_WithContactIdFilter_ShouldReturnFilteredInteractions()
    {
        // Arrange
        var interaction1 = CreateTestInteraction(_testContact.Id, _curatorUser.Id);

        var contact2 = new Contact
        {
            ContactId = "TEST-002",
            BlockId = _testBlock.Id,
            FullNameEncrypted = _encryptionService.Encrypt("Contact 2"),
            ResponsibleCuratorId = _curatorUser.Id,
            IsActive = true,
            UpdatedBy = _curatorUser.Id
        };
        _context.Contacts.Add(contact2);
        await _context.SaveChangesAsync();

        var interaction2 = CreateTestInteraction(contact2.Id, _curatorUser.Id);

        _context.Interactions.AddRange(interaction1, interaction2);
        await _context.SaveChangesAsync();

        // Act
        var (interactions, total) = await _service.GetInteractionsAsync(
            _adminUser.Id, isAdmin: true, contactId: _testContact.Id);

        // Assert
        interactions.Should().HaveCount(1);
        interactions.First().ContactId.Should().Be(_testContact.Id);
    }

    [Fact]
    public async Task GetInteractionsAsync_WithDateRangeFilter_ShouldReturnFilteredInteractions()
    {
        // Arrange
        var oldInteraction = CreateTestInteraction(_testContact.Id, _curatorUser.Id);
        oldInteraction.InteractionDate = DateTime.UtcNow.AddMonths(-2);

        var recentInteraction = CreateTestInteraction(_testContact.Id, _curatorUser.Id);
        recentInteraction.InteractionDate = DateTime.UtcNow.AddDays(-5);

        _context.Interactions.AddRange(oldInteraction, recentInteraction);
        await _context.SaveChangesAsync();

        // Act
        var (interactions, total) = await _service.GetInteractionsAsync(
            _adminUser.Id,
            isAdmin: true,
            fromDate: DateTime.UtcNow.AddMonths(-1));

        // Assert
        interactions.Should().HaveCount(1);
        interactions.First().Id.Should().Be(recentInteraction.Id);
    }

    [Fact]
    public async Task GetInteractionsAsync_ShouldExcludeInactiveInteractions()
    {
        // Arrange
        var activeInteraction = CreateTestInteraction(_testContact.Id, _curatorUser.Id);
        var inactiveInteraction = CreateTestInteraction(_testContact.Id, _curatorUser.Id);
        inactiveInteraction.IsActive = false;

        _context.Interactions.AddRange(activeInteraction, inactiveInteraction);
        await _context.SaveChangesAsync();

        // Act
        var (interactions, total) = await _service.GetInteractionsAsync(_adminUser.Id, isAdmin: true);

        // Assert
        interactions.Should().HaveCount(1);
        interactions.First().IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetInteractionsAsync_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        for (int i = 0; i < 25; i++)
        {
            var interaction = CreateTestInteraction(_testContact.Id, _curatorUser.Id);
            _context.Interactions.Add(interaction);
        }
        await _context.SaveChangesAsync();

        // Act
        var (interactions, total) = await _service.GetInteractionsAsync(
            _adminUser.Id, isAdmin: true, page: 2, pageSize: 10);

        // Assert
        interactions.Should().HaveCount(10);
        total.Should().Be(25);
    }

    #endregion

    #region CreateInteractionAsync Tests

    [Fact]
    public async Task CreateInteractionAsync_ShouldCreateInteraction()
    {
        // Arrange
        var comment = "Test interaction comment";

        // Act
        var interaction = await _service.CreateInteractionAsync(
            contactId: _testContact.Id,
            userId: _curatorUser.Id,
            isAdmin: false,
            comment: comment);

        // Assert
        interaction.Should().NotBeNull();
        interaction.ContactId.Should().Be(_testContact.Id);
        interaction.CuratorId.Should().Be(_curatorUser.Id);
        interaction.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task CreateInteractionAsync_ShouldEncryptComment()
    {
        // Arrange
        var comment = "Confidential comment";

        // Act
        var interaction = await _service.CreateInteractionAsync(
            contactId: _testContact.Id,
            userId: _curatorUser.Id,
            isAdmin: false,
            comment: comment);

        // Assert
        interaction.CommentEncrypted.Should().NotBeNullOrEmpty();
        var decrypted = _encryptionService.Decrypt(interaction.CommentEncrypted!);
        decrypted.Should().Be(comment);
    }

    [Fact]
    public async Task CreateInteractionAsync_ShouldUpdateContactLastInteractionDate()
    {
        // Arrange
        var interactionDate = DateTime.UtcNow.AddDays(-1);

        // Act
        await _service.CreateInteractionAsync(
            contactId: _testContact.Id,
            userId: _curatorUser.Id,
            isAdmin: false,
            interactionDate: interactionDate);

        // Assert
        var contact = await _context.Contacts.FindAsync(_testContact.Id);
        contact!.LastInteractionDate.Should().BeCloseTo(interactionDate, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task CreateInteractionAsync_WithNextTouchDate_ShouldUpdateContactNextTouchDate()
    {
        // Arrange
        var nextTouchDate = DateTime.UtcNow.AddDays(30);

        // Act
        await _service.CreateInteractionAsync(
            contactId: _testContact.Id,
            userId: _curatorUser.Id,
            isAdmin: false,
            nextTouchDate: nextTouchDate);

        // Assert
        var contact = await _context.Contacts.FindAsync(_testContact.Id);
        contact!.NextTouchDate.Should().BeCloseTo(nextTouchDate, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task CreateInteractionAsync_ShouldCreateAuditLog()
    {
        // Act
        var interaction = await _service.CreateInteractionAsync(
            contactId: _testContact.Id,
            userId: _curatorUser.Id,
            isAdmin: false);

        // Assert
        var auditLog = await _context.AuditLogs
            .FirstOrDefaultAsync(a => a.EntityType == "Interaction" && a.EntityId == interaction.Id.ToString());

        auditLog.Should().NotBeNull();
        auditLog!.Action.Should().Be(AuditActionType.Create);
        auditLog.UserId.Should().Be(_curatorUser.Id);
    }

    [Fact]
    public async Task CreateInteractionAsync_WithStatusChange_ShouldUpdateContactStatus()
    {
        // Arrange
        _testContact.InfluenceStatusId = 1;
        await _context.SaveChangesAsync();

        var statusChangeJson = "{\"newStatus\":\"2\"}";

        // Act
        await _service.CreateInteractionAsync(
            contactId: _testContact.Id,
            userId: _curatorUser.Id,
            isAdmin: false,
            statusChangeJson: statusChangeJson);

        // Assert
        var contact = await _context.Contacts.FindAsync(_testContact.Id);
        contact!.InfluenceStatusId.Should().Be(2);
    }

    [Fact]
    public async Task CreateInteractionAsync_WithStatusChange_ShouldCreateStatusHistory()
    {
        // Arrange
        _testContact.InfluenceStatusId = 1;
        await _context.SaveChangesAsync();

        var statusChangeJson = "{\"newStatus\":\"2\"}";

        // Act
        await _service.CreateInteractionAsync(
            contactId: _testContact.Id,
            userId: _curatorUser.Id,
            isAdmin: false,
            statusChangeJson: statusChangeJson);

        // Assert
        var statusHistory = await _context.InfluenceStatusHistories
            .FirstOrDefaultAsync(h => h.ContactId == _testContact.Id);

        statusHistory.Should().NotBeNull();
        statusHistory!.PreviousStatus.Should().Be("1");
        statusHistory.NewStatus.Should().Be("2");
    }

    [Fact]
    public async Task CreateInteractionAsync_AsCurator_WithoutAccess_ShouldThrowUnauthorizedException()
    {
        // Arrange
        var otherBlock = new Block { Name = "Other Block", Code = "OTHER", Status = BlockStatus.Active };
        _context.Blocks.Add(otherBlock);
        await _context.SaveChangesAsync();

        var otherContact = new Contact
        {
            ContactId = "OTHER-001",
            BlockId = otherBlock.Id,
            FullNameEncrypted = _encryptionService.Encrypt("Other Contact"),
            ResponsibleCuratorId = _adminUser.Id,
            IsActive = true,
            UpdatedBy = _adminUser.Id
        };
        _context.Contacts.Add(otherContact);
        await _context.SaveChangesAsync();

        // Act
        Func<Task> act = async () => await _service.CreateInteractionAsync(
            contactId: otherContact.Id,
            userId: _curatorUser.Id,
            isAdmin: false);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task CreateInteractionAsync_WithInvalidContactId_ShouldThrowArgumentException()
    {
        // Act
        Func<Task> act = async () => await _service.CreateInteractionAsync(
            contactId: 99999,
            userId: _curatorUser.Id,
            isAdmin: false);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Contact not found*");
    }

    #endregion

    #region UpdateInteractionAsync Tests

    [Fact]
    public async Task UpdateInteractionAsync_ShouldUpdateInteraction()
    {
        // Arrange
        var interaction = CreateTestInteraction(_testContact.Id, _curatorUser.Id);
        _context.Interactions.Add(interaction);
        await _context.SaveChangesAsync();

        var newComment = "Updated comment";

        // Act
        await _service.UpdateInteractionAsync(
            id: interaction.Id,
            userId: _curatorUser.Id,
            isAdmin: false,
            comment: newComment);

        // Assert
        var updated = await _context.Interactions.FindAsync(interaction.Id);
        var decryptedComment = _encryptionService.Decrypt(updated!.CommentEncrypted!);
        decryptedComment.Should().Be(newComment);
    }

    [Fact]
    public async Task UpdateInteractionAsync_WithNextTouchDate_ShouldUpdateContactNextTouchDate()
    {
        // Arrange
        var interaction = CreateTestInteraction(_testContact.Id, _curatorUser.Id);
        _context.Interactions.Add(interaction);
        await _context.SaveChangesAsync();

        var newNextTouchDate = DateTime.UtcNow.AddDays(60);

        // Act
        await _service.UpdateInteractionAsync(
            id: interaction.Id,
            userId: _curatorUser.Id,
            isAdmin: false,
            nextTouchDate: newNextTouchDate);

        // Assert
        var contact = await _context.Contacts.FindAsync(_testContact.Id);
        contact!.NextTouchDate.Should().BeCloseTo(newNextTouchDate, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task UpdateInteractionAsync_ShouldCreateAuditLog()
    {
        // Arrange
        var interaction = CreateTestInteraction(_testContact.Id, _curatorUser.Id);
        _context.Interactions.Add(interaction);
        await _context.SaveChangesAsync();

        // Act
        await _service.UpdateInteractionAsync(
            id: interaction.Id,
            userId: _curatorUser.Id,
            isAdmin: false,
            comment: "Updated");

        // Assert
        var auditLog = await _context.AuditLogs
            .Where(a => a.EntityType == "Interaction" && a.EntityId == interaction.Id.ToString())
            .OrderByDescending(a => a.Timestamp)
            .FirstOrDefaultAsync();

        auditLog.Should().NotBeNull();
        auditLog!.Action.Should().Be(AuditActionType.Update);
    }

    [Fact]
    public async Task UpdateInteractionAsync_WithInvalidId_ShouldThrowArgumentException()
    {
        // Act
        Func<Task> act = async () => await _service.UpdateInteractionAsync(
            id: 99999,
            userId: _curatorUser.Id,
            isAdmin: false,
            comment: "Test");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Interaction not found*");
    }

    #endregion

    #region DeactivateInteractionAsync Tests

    [Fact]
    public async Task DeactivateInteractionAsync_ShouldSoftDelete()
    {
        // Arrange
        var interaction = CreateTestInteraction(_testContact.Id, _curatorUser.Id);
        _context.Interactions.Add(interaction);
        await _context.SaveChangesAsync();

        // Act
        await _service.DeactivateInteractionAsync(interaction.Id, _adminUser.Id);

        // Assert
        var deactivated = await _context.Interactions.FindAsync(interaction.Id);
        deactivated.Should().NotBeNull();
        deactivated!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeactivateInteractionAsync_ShouldCreateAuditLog()
    {
        // Arrange
        var interaction = CreateTestInteraction(_testContact.Id, _curatorUser.Id);
        _context.Interactions.Add(interaction);
        await _context.SaveChangesAsync();

        // Act
        await _service.DeactivateInteractionAsync(interaction.Id, _adminUser.Id);

        // Assert
        var auditLog = await _context.AuditLogs
            .FirstOrDefaultAsync(a => a.EntityType == "Interaction" && a.EntityId == interaction.Id.ToString());

        auditLog.Should().NotBeNull();
        auditLog!.Action.Should().Be(AuditActionType.Delete);
    }

    #endregion

    #region GetRecentInteractionsAsync Tests

    [Fact]
    public async Task GetRecentInteractionsAsync_ShouldReturnLimitedResults()
    {
        // Arrange
        for (int i = 0; i < 10; i++)
        {
            var interaction = CreateTestInteraction(_testContact.Id, _curatorUser.Id);
            _context.Interactions.Add(interaction);
        }
        await _context.SaveChangesAsync();

        // Act
        var recent = await _service.GetRecentInteractionsAsync(_adminUser.Id, isAdmin: true, count: 5);

        // Assert
        recent.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetRecentInteractionsAsync_ShouldOrderByInteractionDateDescending()
    {
        // Arrange
        var interaction1 = CreateTestInteraction(_testContact.Id, _curatorUser.Id);
        interaction1.InteractionDate = DateTime.UtcNow.AddDays(-10);

        var interaction2 = CreateTestInteraction(_testContact.Id, _curatorUser.Id);
        interaction2.InteractionDate = DateTime.UtcNow.AddDays(-5);

        _context.Interactions.AddRange(interaction1, interaction2);
        await _context.SaveChangesAsync();

        // Act
        var recent = (await _service.GetRecentInteractionsAsync(_adminUser.Id, isAdmin: true, count: 5)).ToList();

        // Assert
        recent[0].Id.Should().Be(interaction2.Id); // Most recent first
        recent[1].Id.Should().Be(interaction1.Id);
    }

    #endregion

    #region RecordStatusChangeAsync Tests

    [Fact]
    public async Task RecordStatusChangeAsync_ShouldUpdateContactStatus()
    {
        // Arrange
        _testContact.InfluenceStatusId = 1;
        await _context.SaveChangesAsync();

        var statusChangeJson = "{\"newStatus\":\"3\"}";

        // Act
        await _service.RecordStatusChangeAsync(_testContact.Id, _curatorUser.Id, statusChangeJson);
        await _context.SaveChangesAsync();

        // Assert
        var contact = await _context.Contacts.FindAsync(_testContact.Id);
        contact!.InfluenceStatusId.Should().Be(3);
    }

    [Fact]
    public async Task RecordStatusChangeAsync_ShouldCreateStatusHistory()
    {
        // Arrange
        _testContact.InfluenceStatusId = 1;
        await _context.SaveChangesAsync();

        var statusChangeJson = "{\"newStatus\":\"3\"}";

        // Act
        await _service.RecordStatusChangeAsync(_testContact.Id, _curatorUser.Id, statusChangeJson);
        await _context.SaveChangesAsync();

        // Assert
        var statusHistory = await _context.InfluenceStatusHistories
            .Where(h => h.ContactId == _testContact.Id)
            .OrderByDescending(h => h.ChangedAt)
            .FirstOrDefaultAsync();

        statusHistory.Should().NotBeNull();
        statusHistory!.PreviousStatus.Should().Be("1");
        statusHistory.NewStatus.Should().Be("3");
    }

    [Fact]
    public async Task RecordStatusChangeAsync_WithInvalidJson_ShouldNotThrow()
    {
        // Arrange
        var invalidJson = "{ invalid json }";

        // Act
        Func<Task> act = async () =>
        {
            await _service.RecordStatusChangeAsync(_testContact.Id, _curatorUser.Id, invalidJson);
            await _context.SaveChangesAsync();
        };

        // Assert
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region Helper Methods

    private Interaction CreateTestInteraction(int contactId, int curatorId)
    {
        return new Interaction
        {
            ContactId = contactId,
            CuratorId = curatorId,
            InteractionDate = DateTime.UtcNow,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = curatorId
        };
    }

    #endregion

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
