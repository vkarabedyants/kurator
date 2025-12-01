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

public class ContactServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<ContactService> _logger;
    private readonly ContactService _service;
    private readonly User _adminUser;
    private readonly User _curatorUser;
    private readonly Block _testBlock;

    public ContactServiceTests()
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
        _logger = new Mock<ILogger<ContactService>>().Object;

        // Create service
        _service = new ContactService(_context, _encryptionService, _logger);

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

    #region GetContactsAsync Tests

    [Fact]
    public async Task GetContactsAsync_AsAdmin_ShouldReturnAllContacts()
    {
        // Arrange
        var contact1 = CreateTestContact("Contact 1", _testBlock.Id);
        var contact2 = CreateTestContact("Contact 2", _testBlock.Id);
        _context.Contacts.AddRange(contact1, contact2);
        await _context.SaveChangesAsync();

        // Act
        var (contacts, total) = await _service.GetContactsAsync(_adminUser.Id, isAdmin: true);

        // Assert
        contacts.Should().HaveCount(2);
        total.Should().Be(2);
    }

    [Fact]
    public async Task GetContactsAsync_AsCurator_ShouldReturnOnlyAssignedBlockContacts()
    {
        // Arrange
        var assignedBlockContact = CreateTestContact("Assigned Contact", _testBlock.Id);

        var otherBlock = new Block { Name = "Other Block", Code = "OTHER", Status = BlockStatus.Active };
        _context.Blocks.Add(otherBlock);
        await _context.SaveChangesAsync();

        var otherBlockContact = CreateTestContact("Other Contact", otherBlock.Id);

        _context.Contacts.AddRange(assignedBlockContact, otherBlockContact);
        await _context.SaveChangesAsync();

        // Act
        var (contacts, total) = await _service.GetContactsAsync(_curatorUser.Id, isAdmin: false);

        // Assert
        contacts.Should().HaveCount(1);
        total.Should().Be(1);
        contacts.First().Id.Should().Be(assignedBlockContact.Id);
    }

    [Fact]
    public async Task GetContactsAsync_WithBlockIdFilter_ShouldReturnFilteredContacts()
    {
        // Arrange
        var block1 = new Block { Name = "Block 1", Code = "BLK1", Status = BlockStatus.Active };
        var block2 = new Block { Name = "Block 2", Code = "BLK2", Status = BlockStatus.Active };
        _context.Blocks.AddRange(block1, block2);
        await _context.SaveChangesAsync();

        var contact1 = CreateTestContact("Contact 1", block1.Id);
        var contact2 = CreateTestContact("Contact 2", block2.Id);
        _context.Contacts.AddRange(contact1, contact2);
        await _context.SaveChangesAsync();

        // Act
        var (contacts, total) = await _service.GetContactsAsync(
            _adminUser.Id, isAdmin: true, blockId: block1.Id);

        // Assert
        contacts.Should().HaveCount(1);
        total.Should().Be(1);
        contacts.First().BlockId.Should().Be(block1.Id);
    }

    [Fact]
    public async Task GetContactsAsync_WithSearchFilter_ShouldReturnMatchingContacts()
    {
        // Arrange
        var contact1 = CreateTestContact("John Doe", _testBlock.Id);
        contact1.Position = "Director";
        var contact2 = CreateTestContact("Jane Smith", _testBlock.Id);
        contact2.Position = "Manager";
        _context.Contacts.AddRange(contact1, contact2);
        await _context.SaveChangesAsync();

        // Act
        var (contacts, total) = await _service.GetContactsAsync(
            _adminUser.Id, isAdmin: true, search: "Director");

        // Assert
        contacts.Should().HaveCount(1);
        contacts.First().Position.Should().Be("Director");
    }

    [Fact]
    public async Task GetContactsAsync_WithInfluenceStatusFilter_ShouldReturnFilteredContacts()
    {
        // Arrange
        var contact1 = CreateTestContact("Contact 1", _testBlock.Id);
        contact1.InfluenceStatusId = 1;
        var contact2 = CreateTestContact("Contact 2", _testBlock.Id);
        contact2.InfluenceStatusId = 2;
        _context.Contacts.AddRange(contact1, contact2);
        await _context.SaveChangesAsync();

        // Act
        var (contacts, total) = await _service.GetContactsAsync(
            _adminUser.Id, isAdmin: true, influenceStatusId: 1);

        // Assert
        contacts.Should().HaveCount(1);
        contacts.First().InfluenceStatusId.Should().Be(1);
    }

    [Fact]
    public async Task GetContactsAsync_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        for (int i = 1; i <= 25; i++)
        {
            var contact = CreateTestContact($"Contact {i}", _testBlock.Id);
            _context.Contacts.Add(contact);
        }
        await _context.SaveChangesAsync();

        // Act
        var (contacts, total) = await _service.GetContactsAsync(
            _adminUser.Id, isAdmin: true, page: 2, pageSize: 10);

        // Assert
        contacts.Should().HaveCount(10);
        total.Should().Be(25);
    }

    [Fact]
    public async Task GetContactsAsync_ShouldExcludeInactiveContacts()
    {
        // Arrange
        var activeContact = CreateTestContact("Active Contact", _testBlock.Id);
        var inactiveContact = CreateTestContact("Inactive Contact", _testBlock.Id);
        inactiveContact.IsActive = false;
        _context.Contacts.AddRange(activeContact, inactiveContact);
        await _context.SaveChangesAsync();

        // Act
        var (contacts, total) = await _service.GetContactsAsync(_adminUser.Id, isAdmin: true);

        // Assert
        contacts.Should().HaveCount(1);
        contacts.First().IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetContactsAsync_ShouldExcludeArchivedBlockContacts()
    {
        // Arrange
        var archivedBlock = new Block { Name = "Archived Block", Code = "ARCH", Status = BlockStatus.Archived };
        _context.Blocks.Add(archivedBlock);
        await _context.SaveChangesAsync();

        var activeBlockContact = CreateTestContact("Active Block Contact", _testBlock.Id);
        var archivedBlockContact = CreateTestContact("Archived Block Contact", archivedBlock.Id);
        _context.Contacts.AddRange(activeBlockContact, archivedBlockContact);
        await _context.SaveChangesAsync();

        // Act
        var (contacts, total) = await _service.GetContactsAsync(_adminUser.Id, isAdmin: true);

        // Assert
        contacts.Should().HaveCount(1);
        contacts.First().BlockId.Should().Be(_testBlock.Id);
    }

    #endregion

    #region GetContactByIdAsync Tests

    [Fact]
    public async Task GetContactByIdAsync_AsAdmin_ShouldReturnContact()
    {
        // Arrange
        var contact = CreateTestContact("Test Contact", _testBlock.Id);
        _context.Contacts.Add(contact);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetContactByIdAsync(contact.Id, _adminUser.Id, isAdmin: true);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(contact.Id);
    }

    [Fact]
    public async Task GetContactByIdAsync_AsCurator_WithAccess_ShouldReturnContact()
    {
        // Arrange
        var contact = CreateTestContact("Test Contact", _testBlock.Id);
        _context.Contacts.Add(contact);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetContactByIdAsync(contact.Id, _curatorUser.Id, isAdmin: false);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(contact.Id);
    }

    [Fact]
    public async Task GetContactByIdAsync_AsCurator_WithoutAccess_ShouldReturnNull()
    {
        // Arrange
        var otherBlock = new Block { Name = "Other Block", Code = "OTHER", Status = BlockStatus.Active };
        _context.Blocks.Add(otherBlock);
        await _context.SaveChangesAsync();

        var contact = CreateTestContact("Test Contact", otherBlock.Id);
        _context.Contacts.Add(contact);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetContactByIdAsync(contact.Id, _curatorUser.Id, isAdmin: false);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetContactByIdAsync_WithInvalidId_ShouldReturnNull()
    {
        // Act
        var result = await _service.GetContactByIdAsync(99999, _adminUser.Id, isAdmin: true);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetContactByIdAsync_ShouldIncludeInteractions()
    {
        // Arrange
        var contact = CreateTestContact("Test Contact", _testBlock.Id);
        _context.Contacts.Add(contact);
        await _context.SaveChangesAsync();

        var interaction = new Interaction
        {
            ContactId = contact.Id,
            CuratorId = _adminUser.Id,
            InteractionDate = DateTime.UtcNow,
            IsActive = true,
            UpdatedBy = _adminUser.Id
        };
        _context.Interactions.Add(interaction);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetContactByIdAsync(contact.Id, _adminUser.Id, isAdmin: true);

        // Assert
        result.Should().NotBeNull();
        result!.Interactions.Should().HaveCount(1);
    }

    #endregion

    #region CreateContactAsync Tests

    [Fact]
    public async Task CreateContactAsync_AsAdmin_ShouldCreateContact()
    {
        // Arrange
        var fullName = "New Contact";

        // Act
        var contact = await _service.CreateContactAsync(
            blockId: _testBlock.Id,
            fullName: fullName,
            userId: _adminUser.Id,
            isAdmin: true);

        // Assert
        contact.Should().NotBeNull();
        contact.BlockId.Should().Be(_testBlock.Id);
        contact.ResponsibleCuratorId.Should().Be(_adminUser.Id);
        contact.IsActive.Should().BeTrue();

        // Verify encryption
        var decryptedName = _encryptionService.Decrypt(contact.FullNameEncrypted);
        decryptedName.Should().Be(fullName);
    }

    [Fact]
    public async Task CreateContactAsync_ShouldGenerateContactId()
    {
        // Arrange
        var fullName = "New Contact";

        // Act
        var contact = await _service.CreateContactAsync(
            blockId: _testBlock.Id,
            fullName: fullName,
            userId: _adminUser.Id,
            isAdmin: true);

        // Assert
        contact.ContactId.Should().NotBeNullOrEmpty();
        contact.ContactId.Should().StartWith(_testBlock.Code + "-");
        contact.ContactId.Should().MatchRegex(@"^TEST-\d{3}$");
    }

    [Fact]
    public async Task CreateContactAsync_ShouldIncrementContactIdSequence()
    {
        // Arrange
        var contact1 = await _service.CreateContactAsync(
            blockId: _testBlock.Id,
            fullName: "Contact 1",
            userId: _adminUser.Id,
            isAdmin: true);

        // Act
        var contact2 = await _service.CreateContactAsync(
            blockId: _testBlock.Id,
            fullName: "Contact 2",
            userId: _adminUser.Id,
            isAdmin: true);

        // Assert
        contact1.ContactId.Should().Be("TEST-001");
        contact2.ContactId.Should().Be("TEST-002");
    }

    [Fact]
    public async Task CreateContactAsync_WithNotes_ShouldEncryptNotes()
    {
        // Arrange
        var notes = "Confidential notes";

        // Act
        var contact = await _service.CreateContactAsync(
            blockId: _testBlock.Id,
            fullName: "Test Contact",
            userId: _adminUser.Id,
            isAdmin: true,
            notes: notes);

        // Assert
        contact.NotesEncrypted.Should().NotBeNullOrEmpty();
        var decryptedNotes = _encryptionService.Decrypt(contact.NotesEncrypted!);
        decryptedNotes.Should().Be(notes);
    }

    [Fact]
    public async Task CreateContactAsync_WithAllOptionalFields_ShouldSetAllFields()
    {
        // Arrange & Act
        var contact = await _service.CreateContactAsync(
            blockId: _testBlock.Id,
            fullName: "Test Contact",
            userId: _adminUser.Id,
            isAdmin: true,
            organizationId: 1,
            position: "Director",
            influenceStatusId: 1,
            influenceTypeId: 2,
            usefulnessDescription: "Very useful",
            communicationChannelId: 3,
            contactSourceId: 4,
            nextTouchDate: DateTime.UtcNow.AddDays(30),
            notes: "Important contact");

        // Assert
        contact.OrganizationId.Should().Be(1);
        contact.Position.Should().Be("Director");
        contact.InfluenceStatusId.Should().Be(1);
        contact.InfluenceTypeId.Should().Be(2);
        contact.UsefulnessDescription.Should().Be("Very useful");
        contact.CommunicationChannelId.Should().Be(3);
        contact.ContactSourceId.Should().Be(4);
        contact.NextTouchDate.Should().NotBeNull();
        contact.NotesEncrypted.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CreateContactAsync_ShouldCreateAuditLog()
    {
        // Arrange & Act
        var contact = await _service.CreateContactAsync(
            blockId: _testBlock.Id,
            fullName: "Test Contact",
            userId: _adminUser.Id,
            isAdmin: true);

        // Assert
        var auditLog = await _context.AuditLogs
            .FirstOrDefaultAsync(a => a.EntityType == "Contact" && a.EntityId == contact.Id.ToString());

        auditLog.Should().NotBeNull();
        auditLog!.Action.Should().Be(AuditActionType.Create);
        auditLog.UserId.Should().Be(_adminUser.Id);
    }

    [Fact]
    public async Task CreateContactAsync_AsCurator_WithAccess_ShouldSucceed()
    {
        // Act
        var contact = await _service.CreateContactAsync(
            blockId: _testBlock.Id,
            fullName: "Test Contact",
            userId: _curatorUser.Id,
            isAdmin: false);

        // Assert
        contact.Should().NotBeNull();
        contact.BlockId.Should().Be(_testBlock.Id);
    }

    [Fact]
    public async Task CreateContactAsync_AsCurator_WithoutAccess_ShouldThrowUnauthorizedException()
    {
        // Arrange
        var otherBlock = new Block { Name = "Other Block", Code = "OTHER", Status = BlockStatus.Active };
        _context.Blocks.Add(otherBlock);
        await _context.SaveChangesAsync();

        // Act
        Func<Task> act = async () => await _service.CreateContactAsync(
            blockId: otherBlock.Id,
            fullName: "Test Contact",
            userId: _curatorUser.Id,
            isAdmin: false);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task CreateContactAsync_WithInvalidBlockId_ShouldThrowArgumentException()
    {
        // Act
        Func<Task> act = async () => await _service.CreateContactAsync(
            blockId: 99999,
            fullName: "Test Contact",
            userId: _adminUser.Id,
            isAdmin: true);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Block not found*");
    }

    #endregion

    #region UpdateContactAsync Tests

    [Fact]
    public async Task UpdateContactAsync_AsAdmin_ShouldUpdateContact()
    {
        // Arrange
        var contact = CreateTestContact("Original Contact", _testBlock.Id);
        _context.Contacts.Add(contact);
        await _context.SaveChangesAsync();

        // Act
        await _service.UpdateContactAsync(
            id: contact.Id,
            userId: _adminUser.Id,
            isAdmin: true,
            position: "New Position",
            influenceStatusId: 2);

        // Assert
        var updated = await _context.Contacts.FindAsync(contact.Id);
        updated!.Position.Should().Be("New Position");
        updated.InfluenceStatusId.Should().Be(2);
        updated.UpdatedBy.Should().Be(_adminUser.Id);
    }

    [Fact]
    public async Task UpdateContactAsync_WithStatusChange_ShouldCreateStatusHistory()
    {
        // Arrange
        var contact = CreateTestContact("Test Contact", _testBlock.Id);
        contact.InfluenceStatusId = 1;
        _context.Contacts.Add(contact);
        await _context.SaveChangesAsync();

        // Act
        await _service.UpdateContactAsync(
            id: contact.Id,
            userId: _adminUser.Id,
            isAdmin: true,
            influenceStatusId: 2);

        // Assert
        var statusHistory = await _context.InfluenceStatusHistories
            .FirstOrDefaultAsync(h => h.ContactId == contact.Id);

        statusHistory.Should().NotBeNull();
        statusHistory!.PreviousStatus.Should().Be("1");
        statusHistory.NewStatus.Should().Be("2");
        statusHistory.ChangedByUserId.Should().Be(_adminUser.Id);
    }

    [Fact]
    public async Task UpdateContactAsync_WithStatusChange_ShouldCreateAuditLogWithStatusChange()
    {
        // Arrange
        var contact = CreateTestContact("Test Contact", _testBlock.Id);
        contact.InfluenceStatusId = 1;
        _context.Contacts.Add(contact);
        await _context.SaveChangesAsync();

        // Act
        await _service.UpdateContactAsync(
            id: contact.Id,
            userId: _adminUser.Id,
            isAdmin: true,
            influenceStatusId: 2);

        // Assert
        var auditLog = await _context.AuditLogs
            .Where(a => a.EntityType == "Contact" && a.EntityId == contact.Id.ToString())
            .OrderByDescending(a => a.Timestamp)
            .FirstOrDefaultAsync();

        auditLog.Should().NotBeNull();
        auditLog!.Action.Should().Be(AuditActionType.StatusChange);
    }

    [Fact]
    public async Task UpdateContactAsync_WithoutStatusChange_ShouldCreateRegularAuditLog()
    {
        // Arrange
        var contact = CreateTestContact("Test Contact", _testBlock.Id);
        contact.InfluenceStatusId = 1;
        _context.Contacts.Add(contact);
        await _context.SaveChangesAsync();

        // Act
        await _service.UpdateContactAsync(
            id: contact.Id,
            userId: _adminUser.Id,
            isAdmin: true,
            position: "New Position");

        // Assert
        var auditLog = await _context.AuditLogs
            .Where(a => a.EntityType == "Contact" && a.EntityId == contact.Id.ToString())
            .OrderByDescending(a => a.Timestamp)
            .FirstOrDefaultAsync();

        auditLog.Should().NotBeNull();
        auditLog!.Action.Should().Be(AuditActionType.Update);
    }

    [Fact]
    public async Task UpdateContactAsync_AsCurator_WithoutAccess_ShouldThrowUnauthorizedException()
    {
        // Arrange
        var otherBlock = new Block { Name = "Other Block", Code = "OTHER", Status = BlockStatus.Active };
        _context.Blocks.Add(otherBlock);
        await _context.SaveChangesAsync();

        var contact = CreateTestContact("Test Contact", otherBlock.Id);
        _context.Contacts.Add(contact);
        await _context.SaveChangesAsync();

        // Act
        Func<Task> act = async () => await _service.UpdateContactAsync(
            id: contact.Id,
            userId: _curatorUser.Id,
            isAdmin: false,
            position: "New Position");

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task UpdateContactAsync_WithInvalidId_ShouldThrowArgumentException()
    {
        // Act
        Func<Task> act = async () => await _service.UpdateContactAsync(
            id: 99999,
            userId: _adminUser.Id,
            isAdmin: true,
            position: "New Position");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Contact not found*");
    }

    #endregion

    #region DeleteContactAsync Tests

    [Fact]
    public async Task DeleteContactAsync_ShouldSoftDeleteContact()
    {
        // Arrange
        var contact = CreateTestContact("Test Contact", _testBlock.Id);
        _context.Contacts.Add(contact);
        await _context.SaveChangesAsync();

        // Act
        await _service.DeleteContactAsync(contact.Id, _adminUser.Id);

        // Assert
        var deleted = await _context.Contacts.FindAsync(contact.Id);
        deleted.Should().NotBeNull();
        deleted!.IsActive.Should().BeFalse();
        deleted.UpdatedBy.Should().Be(_adminUser.Id);
    }

    [Fact]
    public async Task DeleteContactAsync_ShouldCreateAuditLog()
    {
        // Arrange
        var contact = CreateTestContact("Test Contact", _testBlock.Id);
        _context.Contacts.Add(contact);
        await _context.SaveChangesAsync();

        // Act
        await _service.DeleteContactAsync(contact.Id, _adminUser.Id);

        // Assert
        var auditLog = await _context.AuditLogs
            .FirstOrDefaultAsync(a => a.EntityType == "Contact" && a.EntityId == contact.Id.ToString());

        auditLog.Should().NotBeNull();
        auditLog!.Action.Should().Be(AuditActionType.Delete);
        auditLog.UserId.Should().Be(_adminUser.Id);
    }

    [Fact]
    public async Task DeleteContactAsync_WithInvalidId_ShouldThrowArgumentException()
    {
        // Act
        Func<Task> act = async () => await _service.DeleteContactAsync(99999, _adminUser.Id);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Contact not found*");
    }

    #endregion

    #region GetOverdueContactsAsync Tests

    [Fact]
    public async Task GetOverdueContactsAsync_ShouldReturnOverdueContacts()
    {
        // Arrange
        var overdueContact = CreateTestContact("Overdue Contact", _testBlock.Id);
        overdueContact.NextTouchDate = DateTime.UtcNow.AddDays(-5);

        var futureContact = CreateTestContact("Future Contact", _testBlock.Id);
        futureContact.NextTouchDate = DateTime.UtcNow.AddDays(5);

        _context.Contacts.AddRange(overdueContact, futureContact);
        await _context.SaveChangesAsync();

        // Act
        var overdue = await _service.GetOverdueContactsAsync(_adminUser.Id, isAdmin: true);

        // Assert
        overdue.Should().HaveCount(1);
        overdue.First().Id.Should().Be(overdueContact.Id);
    }

    [Fact]
    public async Task GetOverdueContactsAsync_AsCurator_ShouldReturnOnlyAssignedBlockOverdueContacts()
    {
        // Arrange
        var assignedOverdue = CreateTestContact("Assigned Overdue", _testBlock.Id);
        assignedOverdue.NextTouchDate = DateTime.UtcNow.AddDays(-5);

        var otherBlock = new Block { Name = "Other Block", Code = "OTHER", Status = BlockStatus.Active };
        _context.Blocks.Add(otherBlock);
        await _context.SaveChangesAsync();

        var otherOverdue = CreateTestContact("Other Overdue", otherBlock.Id);
        otherOverdue.NextTouchDate = DateTime.UtcNow.AddDays(-5);

        _context.Contacts.AddRange(assignedOverdue, otherOverdue);
        await _context.SaveChangesAsync();

        // Act
        var overdue = await _service.GetOverdueContactsAsync(_curatorUser.Id, isAdmin: false);

        // Assert
        overdue.Should().HaveCount(1);
        overdue.First().Id.Should().Be(assignedOverdue.Id);
    }

    [Fact]
    public async Task GetOverdueContactsAsync_ShouldOrderByNextTouchDate()
    {
        // Arrange
        var contact1 = CreateTestContact("Contact 1", _testBlock.Id);
        contact1.NextTouchDate = DateTime.UtcNow.AddDays(-10);

        var contact2 = CreateTestContact("Contact 2", _testBlock.Id);
        contact2.NextTouchDate = DateTime.UtcNow.AddDays(-5);

        _context.Contacts.AddRange(contact1, contact2);
        await _context.SaveChangesAsync();

        // Act
        var overdue = (await _service.GetOverdueContactsAsync(_adminUser.Id, isAdmin: true)).ToList();

        // Assert
        overdue[0].Id.Should().Be(contact1.Id); // Most overdue first
        overdue[1].Id.Should().Be(contact2.Id);
    }

    #endregion

    #region HasAccessToContactAsync Tests

    [Fact]
    public async Task HasAccessToContactAsync_AsAdmin_ShouldAlwaysReturnTrue()
    {
        // Arrange
        var contact = CreateTestContact("Test Contact", _testBlock.Id);
        _context.Contacts.Add(contact);
        await _context.SaveChangesAsync();

        // Act
        var hasAccess = await _service.HasAccessToContactAsync(contact.Id, _adminUser.Id, isAdmin: true);

        // Assert
        hasAccess.Should().BeTrue();
    }

    [Fact]
    public async Task HasAccessToContactAsync_AsCurator_WithAccess_ShouldReturnTrue()
    {
        // Arrange
        var contact = CreateTestContact("Test Contact", _testBlock.Id);
        _context.Contacts.Add(contact);
        await _context.SaveChangesAsync();

        // Act
        var hasAccess = await _service.HasAccessToContactAsync(contact.Id, _curatorUser.Id, isAdmin: false);

        // Assert
        hasAccess.Should().BeTrue();
    }

    [Fact]
    public async Task HasAccessToContactAsync_AsCurator_WithoutAccess_ShouldReturnFalse()
    {
        // Arrange
        var otherBlock = new Block { Name = "Other Block", Code = "OTHER", Status = BlockStatus.Active };
        _context.Blocks.Add(otherBlock);
        await _context.SaveChangesAsync();

        var contact = CreateTestContact("Test Contact", otherBlock.Id);
        _context.Contacts.Add(contact);
        await _context.SaveChangesAsync();

        // Act
        var hasAccess = await _service.HasAccessToContactAsync(contact.Id, _curatorUser.Id, isAdmin: false);

        // Assert
        hasAccess.Should().BeFalse();
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
            ResponsibleCuratorId = _adminUser.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = _adminUser.Id
        };
    }

    #endregion

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
