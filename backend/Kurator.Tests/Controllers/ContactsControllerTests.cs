using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Kurator.Api.Controllers;
using Kurator.Core.Entities;
using Kurator.Core.Enums;
using Kurator.Core.Interfaces;
using Kurator.Infrastructure.Data;
using Kurator.Infrastructure.Services;
using System.Security.Claims;

namespace Kurator.Tests.Controllers;

/// <summary>
/// Tests for ContactsController with authorization and encryption
/// </summary>
public class ContactsControllerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<ContactsController> _logger;
    private readonly ContactsController _controller;
    private User _adminUser = null!;
    private User _curatorUser = null!;
    private Block _testBlock = null!;

    public ContactsControllerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);

        // Setup encryption for tests
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                {"Encryption:Key", "test-encryption-key-12345"}
            })
            .Build();
        _encryptionService = new EncryptionService(configuration);

        _logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<ContactsController>();
        _controller = new ContactsController(_context, _encryptionService, _logger);

        // Create test data
        SetupTestData();
    }

    private void SetupTestData()
    {
        // Create users
        _adminUser = new User { Login = "admin", PasswordHash = "hash", Role = UserRole.Admin };
        _curatorUser = new User { Login = "curator", PasswordHash = "hash", Role = UserRole.Curator };
        _context.Users.AddRange(_adminUser, _curatorUser);

        // Create block
        _testBlock = new Block { Name = "Test Block", Code = "TEST", Status = BlockStatus.Active };
        _context.Blocks.Add(_testBlock);

        _context.SaveChanges();

        // Assign curator to block
        var blockCurator = new BlockCurator
        {
            BlockId = _testBlock.Id,
            UserId = _curatorUser.Id,
            CuratorType = CuratorType.Primary,
            AssignedAt = DateTime.UtcNow,
            AssignedBy = _adminUser.Id
        };
        _context.BlockCurators.Add(blockCurator);

        _context.SaveChanges();
    }

    private void SetupUser(User user, bool isAdmin = false)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Login)
        };

        if (isAdmin)
        {
            claims.Add(new Claim(ClaimTypes.Role, "Admin"));
        }

        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
            {
                User = claimsPrincipal
            }
        };
    }

    [Fact]
    public async Task GetAll_AsAdmin_ShouldReturnAllContacts()
    {
        // Arrange
        SetupUser(_adminUser, true);

        var contact1 = new Contact
        {
            ContactId = "TEST-001",
            BlockId = _testBlock.Id,
            FullNameEncrypted = _encryptionService.Encrypt("John Doe"),
            ResponsibleCuratorId = _curatorUser.Id,
            IsActive = true
        };
        var contact2 = new Contact
        {
            ContactId = "TEST-002",
            BlockId = _testBlock.Id,
            FullNameEncrypted = _encryptionService.Encrypt("Jane Smith"),
            ResponsibleCuratorId = _curatorUser.Id,
            IsActive = true
        };
        _context.Contacts.AddRange(contact1, contact2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetAll();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
        var response = okResult.Value!;

        // Check response structure
        var dataProperty = response.GetType().GetProperty("data");
        dataProperty.Should().NotBeNull();
        var contacts = dataProperty!.GetValue(response).Should().BeAssignableTo<IEnumerable<ContactListDto>>().Subject;

        contacts.Should().HaveCount(2);
        var firstContact = contacts.First(c => c.ContactId == "TEST-001");
        firstContact.FullName.Should().Be("John Doe");
        firstContact.BlockCode.Should().Be("TEST");
        firstContact.ResponsibleCuratorLogin.Should().Be("curator");
    }

    [Fact]
    public async Task GetAll_AsCurator_ShouldReturnOnlyAccessibleContacts()
    {
        // Arrange
        SetupUser(_curatorUser, false);

        var contact1 = new Contact
        {
            ContactId = "TEST-001",
            BlockId = _testBlock.Id, // Curator has access to this block
            FullNameEncrypted = _encryptionService.Encrypt("John Doe"),
            ResponsibleCuratorId = _curatorUser.Id,
            IsActive = true
        };

        // Create another block without curator access
        var otherBlock = new Block { Name = "Other Block", Code = "OTHER", Status = BlockStatus.Active };
        _context.Blocks.Add(otherBlock);
        await _context.SaveChangesAsync();

        var contact2 = new Contact
        {
            ContactId = "OTHER-001",
            BlockId = otherBlock.Id, // Curator does NOT have access to this block
            FullNameEncrypted = _encryptionService.Encrypt("Jane Smith"),
            ResponsibleCuratorId = _adminUser.Id,
            IsActive = true
        };

        _context.Contacts.AddRange(contact1, contact2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetAll();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
        var response = okResult.Value!;
        var dataProperty = response.GetType().GetProperty("data");
        var contacts = dataProperty!.GetValue(response).Should().BeAssignableTo<IEnumerable<ContactListDto>>().Subject;

        contacts.Should().HaveCount(1);
        contacts.First().ContactId.Should().Be("TEST-001");
    }

    [Fact]
    public async Task GetAll_WithBlockFilter_ShouldFilterByBlock()
    {
        // Arrange
        SetupUser(_adminUser, true);

        var otherBlock = new Block { Name = "Other Block", Code = "OTHER", Status = BlockStatus.Active };
        _context.Blocks.Add(otherBlock);
        await _context.SaveChangesAsync();

        var contact1 = new Contact
        {
            ContactId = "TEST-001",
            BlockId = _testBlock.Id,
            FullNameEncrypted = _encryptionService.Encrypt("John Doe"),
            ResponsibleCuratorId = _curatorUser.Id,
            IsActive = true
        };
        var contact2 = new Contact
        {
            ContactId = "OTHER-001",
            BlockId = otherBlock.Id,
            FullNameEncrypted = _encryptionService.Encrypt("Jane Smith"),
            ResponsibleCuratorId = _curatorUser.Id,
            IsActive = true
        };
        _context.Contacts.AddRange(contact1, contact2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetAll(blockId: _testBlock.Id);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
        var response = okResult.Value!;
        var dataProperty = response.GetType().GetProperty("data");
        var contacts = dataProperty!.GetValue(response).Should().BeAssignableTo<IEnumerable<ContactListDto>>().Subject;

        contacts.Should().HaveCount(1);
        contacts.First().ContactId.Should().Be("TEST-001");
    }

    [Fact]
    public async Task GetAll_WithSearch_ShouldFilterByContactIdOrPosition()
    {
        // Arrange
        SetupUser(_adminUser, true);

        var contact1 = new Contact
        {
            ContactId = "TEST-001",
            BlockId = _testBlock.Id,
            FullNameEncrypted = _encryptionService.Encrypt("John Doe"),
            Position = "Manager",
            ResponsibleCuratorId = _curatorUser.Id,
            IsActive = true
        };
        var contact2 = new Contact
        {
            ContactId = "TEST-002",
            BlockId = _testBlock.Id,
            FullNameEncrypted = _encryptionService.Encrypt("Jane Smith"),
            Position = "Director",
            ResponsibleCuratorId = _curatorUser.Id,
            IsActive = true
        };
        _context.Contacts.AddRange(contact1, contact2);
        await _context.SaveChangesAsync();

        // Act - search by ContactId
        var result = await _controller.GetAll(search: "001");

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
        var response = okResult.Value!;
        var dataProperty = response.GetType().GetProperty("data");
        var contacts = dataProperty!.GetValue(response).Should().BeAssignableTo<IEnumerable<ContactListDto>>().Subject;

        contacts.Should().HaveCount(1);
        contacts.First().ContactId.Should().Be("TEST-001");
    }

    [Fact]
    public async Task GetById_AsAdmin_WithValidId_ShouldReturnContact()
    {
        // Arrange
        SetupUser(_adminUser, true);

        var contact = new Contact
        {
            ContactId = "TEST-001",
            BlockId = _testBlock.Id,
            FullNameEncrypted = _encryptionService.Encrypt("John Doe"),
            ResponsibleCuratorId = _curatorUser.Id,
            IsActive = true
        };
        _context.Contacts.Add(contact);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetById(contact.Id);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
        var contactDto = okResult.Value.Should().BeOfType<ContactDetailDto>().Subject;

        contactDto.Id.Should().Be(contact.Id);
        contactDto.ContactId.Should().Be("TEST-001");
        contactDto.FullName.Should().Be("John Doe");
        contactDto.BlockCode.Should().Be("TEST");
        contactDto.ResponsibleCuratorLogin.Should().Be("curator");
    }

    [Fact]
    public async Task GetById_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        SetupUser(_adminUser, true);

        // Act
        var result = await _controller.GetById(999);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetById_AsCurator_WithoutAccess_ShouldReturnForbid()
    {
        // Arrange
        SetupUser(_curatorUser, false);

        var otherBlock = new Block { Name = "Other Block", Code = "OTHER", Status = BlockStatus.Active };
        _context.Blocks.Add(otherBlock);
        await _context.SaveChangesAsync();

        var contact = new Contact
        {
            ContactId = "OTHER-001",
            BlockId = otherBlock.Id, // Curator does not have access to this block
            FullNameEncrypted = _encryptionService.Encrypt("John Doe"),
            ResponsibleCuratorId = _adminUser.Id,
            IsActive = true
        };
        _context.Contacts.Add(contact);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetById(contact.Id);

        // Assert - Controller returns Forbid for access denied, NotFound if not found
        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task Create_WithValidData_ShouldCreateContact()
    {
        // Arrange
        SetupUser(_adminUser, true);

        var request = new CreateContactRequest(
            BlockId: _testBlock.Id,
            FullName: "John Doe",
            OrganizationId: null,
            Position: "Manager",
            InfluenceStatusId: 1,
            InfluenceTypeId: 2,
            UsefulnessDescription: "Very useful contact",
            CommunicationChannelId: 1,
            ContactSourceId: 1,
            NextTouchDate: DateTime.UtcNow.AddDays(7),
            Notes: "Test notes"
        );

        // Act
        var result = await _controller.Create(request);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(_controller.GetById));

        var createdContact = await _context.Contacts.FirstOrDefaultAsync(c => c.ContactId.StartsWith("TEST-"));
        createdContact.Should().NotBeNull();
        _encryptionService.Decrypt(createdContact!.FullNameEncrypted).Should().Be("John Doe");
        createdContact.Position.Should().Be("Manager");
        createdContact.InfluenceStatusId.Should().Be(1);
        createdContact.InfluenceTypeId.Should().Be(2);
        createdContact.UsefulnessDescription.Should().Be("Very useful contact");
        _encryptionService.Decrypt(createdContact.NotesEncrypted!).Should().Be("Test notes");
        createdContact.UpdatedBy.Should().Be(_adminUser.Id);
    }

    [Fact]
    public async Task Create_WithInvalidBlockId_ShouldReturnBadRequest()
    {
        // Arrange
        SetupUser(_adminUser, true);

        var request = new CreateContactRequest(
            BlockId: 999, // Non-existent block
            FullName: "John Doe",
            OrganizationId: null,
            Position: "Manager",
            InfluenceStatusId: 1,
            InfluenceTypeId: 2,
            UsefulnessDescription: null,
            CommunicationChannelId: null,
            ContactSourceId: null,
            NextTouchDate: null,
            Notes: null
        );

        // Act
        var result = await _controller.Create(request);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task Update_WithValidData_ShouldUpdateContact()
    {
        // Arrange
        SetupUser(_adminUser, true);

        var contact = new Contact
        {
            ContactId = "TEST-001",
            BlockId = _testBlock.Id,
            FullNameEncrypted = _encryptionService.Encrypt("John Doe"),
            Position = "Manager",
            ResponsibleCuratorId = _curatorUser.Id,
            IsActive = true
        };
        _context.Contacts.Add(contact);
        await _context.SaveChangesAsync();

        var request = new UpdateContactRequest(
            OrganizationId: 1,
            Position: "Senior Manager",
            InfluenceStatusId: 2,
            InfluenceTypeId: 3,
            UsefulnessDescription: "Updated description",
            CommunicationChannelId: 2,
            ContactSourceId: 2,
            NextTouchDate: DateTime.UtcNow.AddDays(14),
            Notes: "Updated notes"
        );

        // Act
        var result = await _controller.Update(contact.Id, request);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        var updatedContact = await _context.Contacts.FindAsync(contact.Id);
        updatedContact!.Position.Should().Be("Senior Manager");
        updatedContact.InfluenceStatusId.Should().Be(2);
        updatedContact.InfluenceTypeId.Should().Be(3);
        updatedContact.UsefulnessDescription.Should().Be("Updated description");
        _encryptionService.Decrypt(updatedContact.NotesEncrypted!).Should().Be("Updated notes");
        updatedContact.UpdatedBy.Should().Be(_adminUser.Id);
    }

    [Fact]
    public async Task Update_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        SetupUser(_adminUser, true);

        var request = new UpdateContactRequest(
            OrganizationId: null,
            Position: "Manager",
            InfluenceStatusId: null,
            InfluenceTypeId: null,
            UsefulnessDescription: null,
            CommunicationChannelId: null,
            ContactSourceId: null,
            NextTouchDate: null,
            Notes: null
        );

        // Act
        var result = await _controller.Update(999, request);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Delete_WithValidId_ShouldDeactivateContact()
    {
        // Arrange
        SetupUser(_adminUser, true);

        var contact = new Contact
        {
            ContactId = "TEST-001",
            BlockId = _testBlock.Id,
            FullNameEncrypted = _encryptionService.Encrypt("John Doe"),
            ResponsibleCuratorId = _curatorUser.Id,
            IsActive = true
        };
        _context.Contacts.Add(contact);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.Delete(contact.Id);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        var deletedContact = await _context.Contacts.FindAsync(contact.Id);
        deletedContact!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Delete_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        SetupUser(_adminUser, true);

        // Act
        var result = await _controller.Delete(999);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetOverdueContacts_AsAdmin_ShouldReturnOverdueContacts()
    {
        // Arrange
        SetupUser(_adminUser, true);

        var overdueContact = new Contact
        {
            ContactId = "TEST-001",
            BlockId = _testBlock.Id,
            FullNameEncrypted = _encryptionService.Encrypt("John Doe"),
            ResponsibleCuratorId = _curatorUser.Id,
            NextTouchDate = DateTime.UtcNow.AddDays(-1), // Overdue
            IsActive = true
        };

        var futureContact = new Contact
        {
            ContactId = "TEST-002",
            BlockId = _testBlock.Id,
            FullNameEncrypted = _encryptionService.Encrypt("Jane Smith"),
            ResponsibleCuratorId = _curatorUser.Id,
            NextTouchDate = DateTime.UtcNow.AddDays(1), // Not overdue
            IsActive = true
        };

        _context.Contacts.AddRange(overdueContact, futureContact);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetOverdueContacts();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
        var contacts = okResult.Value.Should().BeAssignableTo<IEnumerable<ContactListDto>>().Subject;

        contacts.Should().HaveCount(1);
        contacts.First().ContactId.Should().Be("TEST-001");
        contacts.First().IsOverdue.Should().BeTrue();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
