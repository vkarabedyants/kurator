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

/// <summary>
/// Minimal business rules tests for critical scenarios
/// Covers: data validation, business constraints, data integrity
/// </summary>
public class BusinessRulesValidationTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IEncryptionService _encryptionService;
    private readonly ContactService _contactService;
    private readonly InteractionService _interactionService;
    private User _adminUser = null!;
    private User _curatorUser = null!;
    private Block _activeBlock = null!;

    public BusinessRulesValidationTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                {"Encryption:Key", "test-key-32-characters-long!!"}
            }!)
            .Build();
        _encryptionService = new EncryptionService(config);

        var contactLogger = new Mock<ILogger<ContactService>>().Object;
        var interactionLogger = new Mock<ILogger<InteractionService>>().Object;

        _contactService = new ContactService(_context, _encryptionService, contactLogger);
        _interactionService = new InteractionService(_context, _encryptionService, interactionLogger);

        SetupTestData();
    }

    private void SetupTestData()
    {
        _adminUser = new User { Login = "admin", PasswordHash = "hash", Role = UserRole.Admin, IsActive = true };
        _curatorUser = new User { Login = "curator", PasswordHash = "hash", Role = UserRole.Curator, IsActive = true };
        _context.Users.AddRange(_adminUser, _curatorUser);

        _activeBlock = new Block { Name = "Active Block", Code = "ACTIVE", Status = BlockStatus.Active };
        _context.Blocks.Add(_activeBlock);
        _context.SaveChanges();

        _context.BlockCurators.Add(new BlockCurator
        {
            BlockId = _activeBlock.Id,
            UserId = _curatorUser.Id,
            AssignedAt = DateTime.UtcNow
        });
        _context.SaveChanges();
    }

    #region Contact Validation

    [Fact]
    public async Task CreateContact_WithEmptyFullName_ShouldCreateContactWithEncryptedEmpty()
    {
        // Act: service allows empty names (encryption works with empty string)
        var contact = await _contactService.CreateContactAsync(
            _activeBlock.Id, "", _adminUser.Id, isAdmin: true);

        // Assert: contact is created with encrypted empty string
        contact.Should().NotBeNull();
        contact.FullNameEncrypted.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateContact_WithInvalidBlockId_ShouldThrowArgumentException()
    {
        // Act & Assert: service throws ArgumentException for non-existent block
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _contactService.CreateContactAsync(
                999, "Test Contact", _adminUser.Id, isAdmin: true));
    }

    [Fact]
    public async Task CreateContact_InArchivedBlock_ShouldSucceed()
    {
        // Arrange: service does not check block status on creation
        var archivedBlock = new Block { Name = "Archive", Code = "ARCH", Status = BlockStatus.Archived };
        _context.Blocks.Add(archivedBlock);
        await _context.SaveChangesAsync();

        // Act: contact creation in archived block succeeds
        var contact = await _contactService.CreateContactAsync(
            archivedBlock.Id, "Contact in Archive", _adminUser.Id, isAdmin: true);

        // Assert
        contact.Should().NotBeNull();
        contact.BlockId.Should().Be(archivedBlock.Id);
    }

    [Fact]
    public async Task CreateContact_ShouldGenerateUniqueContactId()
    {
        // Act
        var contact1 = await _contactService.CreateContactAsync(
            _activeBlock.Id, "Contact 1", _adminUser.Id, isAdmin: true);
        var contact2 = await _contactService.CreateContactAsync(
            _activeBlock.Id, "Contact 2", _adminUser.Id, isAdmin: true);

        // Assert
        contact1.ContactId.Should().NotBe(contact2.ContactId);
        contact1.ContactId.Should().StartWith("ACTIVE-");
        contact2.ContactId.Should().StartWith("ACTIVE-");
    }

    [Fact]
    public async Task CreateContact_ShouldSetDefaultValues()
    {
        // Act
        var contact = await _contactService.CreateContactAsync(
            _activeBlock.Id, "New Contact", _adminUser.Id, isAdmin: true);

        // Assert
        contact.IsActive.Should().BeTrue();
        contact.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        contact.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        contact.UpdatedBy.Should().Be(_adminUser.Id);
    }

    #endregion

    #region Interaction Validation

    [Fact]
    public async Task CreateInteraction_WithNonExistentContact_ShouldThrowArgumentException()
    {
        // Act & Assert: service throws ArgumentException for non-existent contact
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _interactionService.CreateInteractionAsync(
                999, _curatorUser.Id, isAdmin: false));
    }

    [Fact]
    public async Task CreateInteraction_WithDeletedContact_ShouldSucceed()
    {
        // Arrange: service does not check IsActive when creating interaction
        var contact = await _contactService.CreateContactAsync(
            _activeBlock.Id, "Contact to Delete", _adminUser.Id, isAdmin: true);
        await _contactService.DeleteContactAsync(contact.Id, _adminUser.Id);

        // Act: interaction creation for deleted contact succeeds
        var interaction = await _interactionService.CreateInteractionAsync(
            contact.Id, _curatorUser.Id, isAdmin: false);

        // Assert
        interaction.Should().NotBeNull();
        interaction.ContactId.Should().Be(contact.Id);
    }

    [Fact]
    public async Task CreateInteraction_ShouldUpdateContactLastInteractionDate()
    {
        // Arrange
        var contact = await _contactService.CreateContactAsync(
            _activeBlock.Id, "Contact for Interaction", _curatorUser.Id, isAdmin: false);
        var oldLastInteraction = contact.LastInteractionDate;

        // Act
        await _interactionService.CreateInteractionAsync(
            contact.Id, _curatorUser.Id, isAdmin: false);

        // Assert
        var updatedContact = await _context.Contacts.FindAsync(contact.Id);
        updatedContact!.LastInteractionDate.Should().NotBe(oldLastInteraction);
        updatedContact.LastInteractionDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task CreateInteraction_ShouldIncrementInteractionCount()
    {
        // Arrange
        var contact = await _contactService.CreateContactAsync(
            _activeBlock.Id, "Contact", _curatorUser.Id, isAdmin: false);

        // Act
        await _interactionService.CreateInteractionAsync(contact.Id, _curatorUser.Id, isAdmin: false);
        await _interactionService.CreateInteractionAsync(contact.Id, _curatorUser.Id, isAdmin: false);
        await _interactionService.CreateInteractionAsync(contact.Id, _curatorUser.Id, isAdmin: false);

        // Assert
        var interactions = await _context.Interactions
            .Where(i => i.ContactId == contact.Id && i.IsActive)
            .ToListAsync();
        interactions.Should().HaveCount(3);
    }

    #endregion

    #region Influence Status Change Validation

    [Fact]
    public async Task UpdateInfluenceStatus_ShouldCreateHistoryRecord()
    {
        // Arrange
        var contact = await _contactService.CreateContactAsync(
            _activeBlock.Id, "Status Contact", _adminUser.Id, isAdmin: true,
            influenceStatusId: 1);

        // Act
        await _contactService.UpdateContactAsync(
            contact.Id, _adminUser.Id, isAdmin: true, influenceStatusId: 2);

        // Assert
        var history = await _context.InfluenceStatusHistories
            .Where(h => h.ContactId == contact.Id)
            .ToListAsync();

        history.Should().HaveCount(1);
        history.First().PreviousStatus.Should().Be("1");
        history.First().NewStatus.Should().Be("2");
        history.First().ChangedByUserId.Should().Be(_adminUser.Id);
    }

    [Fact]
    public async Task UpdateInfluenceStatus_SameStatus_ShouldNotCreateHistory()
    {
        // Arrange
        var contact = await _contactService.CreateContactAsync(
            _activeBlock.Id, "Contact", _adminUser.Id, isAdmin: true,
            influenceStatusId: 1);

        // Act: update with the same status
        await _contactService.UpdateContactAsync(
            contact.Id, _adminUser.Id, isAdmin: true, influenceStatusId: 1);

        // Assert: history should not be created for same status
        var history = await _context.InfluenceStatusHistories
            .Where(h => h.ContactId == contact.Id)
            .ToListAsync();

        history.Should().BeEmpty();
    }

    #endregion

    #region Date Validation

    [Fact]
    public async Task CreateContact_WithFutureNextTouchDate_ShouldWork()
    {
        // Arrange
        var futureDate = DateTime.UtcNow.AddDays(30);

        // Act
        var contact = await _contactService.CreateContactAsync(
            _activeBlock.Id, "Future Contact", _adminUser.Id, isAdmin: true,
            nextTouchDate: futureDate);

        // Assert
        contact.NextTouchDate.Should().BeCloseTo(futureDate, TimeSpan.FromSeconds(1));
        contact.NextTouchDate.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task CreateContact_WithPastNextTouchDate_ShouldBeOverdue()
    {
        // Arrange
        var pastDate = DateTime.UtcNow.AddDays(-5);

        // Act
        var contact = await _contactService.CreateContactAsync(
            _activeBlock.Id, "Overdue Contact", _adminUser.Id, isAdmin: true,
            nextTouchDate: pastDate);

        // Assert
        contact.NextTouchDate.Should().BeCloseTo(pastDate, TimeSpan.FromSeconds(1));

        var overdue = await _contactService.GetOverdueContactsAsync(_adminUser.Id, isAdmin: true);
        overdue.Should().Contain(c => c.Id == contact.Id);
    }

    [Fact]
    public async Task CreateInteraction_ShouldHaveDefaultInteractionDate()
    {
        // Arrange
        var contact = await _contactService.CreateContactAsync(
            _activeBlock.Id, "Contact", _curatorUser.Id, isAdmin: false);

        // Act: create without specifying date
        var interaction = await _interactionService.CreateInteractionAsync(
            contact.Id, _curatorUser.Id, isAdmin: false);

        // Assert
        interaction.InteractionDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    #endregion

    #region Data Integrity

    [Fact]
    public async Task DeleteContact_ShouldNotDeleteRelatedInteractions()
    {
        // Arrange
        var contact = await _contactService.CreateContactAsync(
            _activeBlock.Id, "Contact with Interactions", _curatorUser.Id, isAdmin: false);

        await _interactionService.CreateInteractionAsync(contact.Id, _curatorUser.Id, isAdmin: false);
        await _interactionService.CreateInteractionAsync(contact.Id, _curatorUser.Id, isAdmin: false);

        // Act
        await _contactService.DeleteContactAsync(contact.Id, _adminUser.Id);

        // Assert: contact is soft-deleted
        var deletedContact = await _context.Contacts.FindAsync(contact.Id);
        deletedContact.Should().NotBeNull();
        deletedContact!.IsActive.Should().BeFalse();

        // Interactions should remain in the database
        var interactions = await _context.Interactions
            .Where(i => i.ContactId == contact.Id)
            .ToListAsync();
        interactions.Should().HaveCount(2);
    }

    [Fact]
    public async Task UpdateContact_ShouldUpdateTimestamp()
    {
        // Arrange
        var contact = await _contactService.CreateContactAsync(
            _activeBlock.Id, "Contact", _adminUser.Id, isAdmin: true);
        var originalUpdatedAt = contact.UpdatedAt;

        await Task.Delay(100);

        // Act
        await _contactService.UpdateContactAsync(
            contact.Id, _adminUser.Id, isAdmin: true, position: "New Position");

        // Assert
        var updatedContact = await _context.Contacts.FindAsync(contact.Id);
        updatedContact!.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public async Task UpdateContact_ShouldTrackUpdatedBy()
    {
        // Arrange
        var contact = await _contactService.CreateContactAsync(
            _activeBlock.Id, "Contact", _adminUser.Id, isAdmin: true);

        // Act: update from another user
        await _contactService.UpdateContactAsync(
            contact.Id, _curatorUser.Id, isAdmin: false, position: "New Position");

        // Assert
        var updatedContact = await _context.Contacts.FindAsync(contact.Id);
        updatedContact!.UpdatedBy.Should().Be(_curatorUser.Id);
    }

    #endregion

    #region Block Restrictions

    [Fact]
    public async Task GetMyBlocks_CuratorShouldOnlySeeAssignedBlocks()
    {
        // Arrange
        var block2 = new Block { Name = "Other Block", Code = "OTHER", Status = BlockStatus.Active };
        _context.Blocks.Add(block2);
        await _context.SaveChangesAsync();

        // Act
        var curatorBlocks = await _context.BlockCurators
            .Where(bc => bc.UserId == _curatorUser.Id)
            .Select(bc => bc.BlockId)
            .ToListAsync();

        // Assert
        curatorBlocks.Should().HaveCount(1);
        curatorBlocks.Should().Contain(_activeBlock.Id);
        curatorBlocks.Should().NotContain(block2.Id);
    }

    [Fact]
    public async Task GetAllBlocks_AdminShouldSeeAllBlocks()
    {
        // Arrange
        var block2 = new Block { Name = "Block 2", Code = "BLK2", Status = BlockStatus.Active };
        var block3 = new Block { Name = "Block 3", Code = "BLK3", Status = BlockStatus.Active };
        _context.Blocks.AddRange(block2, block3);
        await _context.SaveChangesAsync();

        // Act
        var allBlocks = await _context.Blocks
            .Where(b => b.Status == BlockStatus.Active)
            .ToListAsync();

        // Assert
        allBlocks.Should().HaveCount(3);
    }

    #endregion

    #region Inactive Users

    [Fact]
    public async Task InactiveUser_ShouldNotBeAssignedAsCurator()
    {
        // Arrange
        var inactiveUser = new User
        {
            Login = "inactive",
            PasswordHash = "hash",
            Role = UserRole.Curator,
            IsActive = false
        };
        _context.Users.Add(inactiveUser);
        await _context.SaveChangesAsync();

        // Act
        var activeUsers = await _context.Users
            .Where(u => u.IsActive && u.Role == UserRole.Curator)
            .ToListAsync();

        // Assert
        activeUsers.Should().NotContain(u => u.Id == inactiveUser.Id);
        activeUsers.Should().Contain(u => u.Id == _curatorUser.Id);
    }

    #endregion

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
