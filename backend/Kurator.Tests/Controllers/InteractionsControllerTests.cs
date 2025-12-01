using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Kurator.Api.Controllers;
using Kurator.Core.Entities;
using Kurator.Core.Enums;
using Kurator.Core.Interfaces;
using Kurator.Infrastructure.Data;
using Kurator.Infrastructure.Services;
using System.Security.Claims;
using System.Text.Json;

namespace Kurator.Tests.Controllers;

/// <summary>
/// Тесты для InteractionsController - создание, обновление, удаление взаимодействий
/// </summary>
public class InteractionsControllerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<InteractionsController> _logger;
    private readonly InteractionsController _controller;
    private User _adminUser = null!;
    private User _curatorUser = null!;
    private User _otherCuratorUser = null!;
    private Block _testBlock = null!;
    private Block _otherBlock = null!;
    private Contact _testContact = null!;
    private Contact _otherContact = null!;

    public InteractionsControllerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);

        // Настройка шифрования для тестов
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                {"Encryption:Key", "test-encryption-key-for-interactions-12345"}
            }!)
            .Build();
        _encryptionService = new EncryptionService(configuration);

        _logger = LoggerFactory.Create(builder => builder.AddConsole())
            .CreateLogger<InteractionsController>();
        _controller = new InteractionsController(_context, _encryptionService, _logger);

        // Создание тестовых данных
        SetupTestData();
    }

    private void SetupTestData()
    {
        // Создание пользователей
        _adminUser = new User
        {
            Login = "admin",
            PasswordHash = "hash",
            Role = UserRole.Admin,
            CreatedAt = DateTime.UtcNow
        };

        _curatorUser = new User
        {
            Login = "curator1",
            PasswordHash = "hash",
            Role = UserRole.Curator,
            CreatedAt = DateTime.UtcNow
        };

        _otherCuratorUser = new User
        {
            Login = "curator2",
            PasswordHash = "hash",
            Role = UserRole.Curator,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.AddRange(_adminUser, _curatorUser, _otherCuratorUser);
        _context.SaveChanges();

        // Создание блоков
        _testBlock = new Block
        {
            Name = "Test Block",
            Code = "TEST",
            Status = BlockStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        _otherBlock = new Block
        {
            Name = "Other Block",
            Code = "OTHER",
            Status = BlockStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        _context.Blocks.AddRange(_testBlock, _otherBlock);
        _context.SaveChanges();

        // Назначение кураторов на блоки
        var blockCurator1 = new BlockCurator
        {
            BlockId = _testBlock.Id,
            UserId = _curatorUser.Id,
            CuratorType = CuratorType.Primary,
            AssignedAt = DateTime.UtcNow,
            AssignedBy = _adminUser.Id
        };

        var blockCurator2 = new BlockCurator
        {
            BlockId = _otherBlock.Id,
            UserId = _otherCuratorUser.Id,
            CuratorType = CuratorType.Primary,
            AssignedAt = DateTime.UtcNow,
            AssignedBy = _adminUser.Id
        };

        _context.BlockCurators.AddRange(blockCurator1, blockCurator2);
        _context.SaveChanges();

        // Создание контактов
        _testContact = new Contact
        {
            ContactId = "TEST-001",
            BlockId = _testBlock.Id,
            FullNameEncrypted = _encryptionService.Encrypt("Test Contact"),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedBy = _adminUser.Id
        };

        _otherContact = new Contact
        {
            ContactId = "OTHER-001",
            BlockId = _otherBlock.Id,
            FullNameEncrypted = _encryptionService.Encrypt("Other Contact"),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedBy = _adminUser.Id
        };

        _context.Contacts.AddRange(_testContact, _otherContact);
        _context.SaveChanges();
    }

    private void SetupUser(User user)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Login),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = claimsPrincipal
            }
        };
    }

    #region Create Tests

    [Fact]
    public async Task Create_WithValidData_ShouldCreateInteractionAndEncryptComment()
    {
        // Arrange
        SetupUser(_curatorUser);
        var request = new CreateInteractionRequest(
            ContactId: _testContact.Id,
            InteractionDate: DateTime.UtcNow,
            InteractionTypeId: 1,
            ResultId: 1,
            Comment: "Test interaction comment",
            StatusChangeJson: null,
            AttachmentsJson: null,
            NextTouchDate: DateTime.UtcNow.AddDays(7)
        );

        // Act
        var result = await _controller.Create(request);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();

        var interaction = await _context.Interactions
            .FirstOrDefaultAsync(i => i.ContactId == _testContact.Id);

        interaction.Should().NotBeNull();
        interaction!.CuratorId.Should().Be(_curatorUser.Id);
        interaction.CommentEncrypted.Should().NotBeNull();
        interaction.IsActive.Should().BeTrue();

        // Verify encryption
        var decryptedComment = _encryptionService.Decrypt(interaction.CommentEncrypted!);
        decryptedComment.Should().Be("Test interaction comment");

        // Verify NextTouchDate updated on contact
        var updatedContact = await _context.Contacts.FindAsync(_testContact.Id);
        updatedContact!.NextTouchDate.Should().Be(request.NextTouchDate);
        updatedContact.LastInteractionDate.Should().NotBeNull();
    }

    [Fact]
    public async Task Create_WithStatusChange_ShouldUpdateContactStatusAndCreateHistory()
    {
        // Arrange
        SetupUser(_curatorUser);

        // Set initial status
        _testContact.InfluenceStatusId = 1;
        await _context.SaveChangesAsync();

        var statusChangeJson = JsonSerializer.Serialize(new { newStatus = "2" });
        var request = new CreateInteractionRequest(
            ContactId: _testContact.Id,
            InteractionDate: DateTime.UtcNow,
            InteractionTypeId: 1,
            ResultId: 1,
            Comment: "Status change interaction",
            StatusChangeJson: statusChangeJson,
            AttachmentsJson: null,
            NextTouchDate: null
        );

        // Act
        var result = await _controller.Create(request);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();

        // Verify contact status updated
        var updatedContact = await _context.Contacts.FindAsync(_testContact.Id);
        updatedContact!.InfluenceStatusId.Should().Be(2);

        // Verify status history created
        var statusHistory = await _context.InfluenceStatusHistories
            .FirstOrDefaultAsync(s => s.ContactId == _testContact.Id);

        statusHistory.Should().NotBeNull();
        statusHistory!.PreviousStatus.Should().Be("1");
        statusHistory.NewStatus.Should().Be("2");
        statusHistory.ChangedByUserId.Should().Be(_curatorUser.Id);

        // Verify audit log created
        var auditLog = await _context.AuditLogs
            .FirstOrDefaultAsync(a => a.EntityType == "Contact" &&
                                     a.Action == AuditActionType.StatusChange);
        auditLog.Should().NotBeNull();
    }

    [Fact]
    public async Task Create_WithAttachments_ShouldStoreAsJson()
    {
        // Arrange
        SetupUser(_adminUser);
        var attachmentsJson = JsonSerializer.Serialize(new[]
        {
            "path/to/file1.pdf",
            "path/to/file2.docx"
        });

        var request = new CreateInteractionRequest(
            ContactId: _testContact.Id,
            InteractionDate: DateTime.UtcNow,
            InteractionTypeId: 1,
            ResultId: 1,
            Comment: "Test with attachments",
            StatusChangeJson: null,
            AttachmentsJson: attachmentsJson,
            NextTouchDate: null
        );

        // Act
        var result = await _controller.Create(request);

        // Assert
        var interaction = await _context.Interactions
            .FirstOrDefaultAsync(i => i.ContactId == _testContact.Id);

        interaction!.AttachmentsJson.Should().NotBeNull();
        interaction.AttachmentsJson.Should().Contain("file1.pdf");
        interaction.AttachmentsJson.Should().Contain("file2.docx");
    }

    [Fact]
    public async Task Create_AsCurator_WithoutAccessToBlock_ShouldReturnForbid()
    {
        // Arrange
        SetupUser(_curatorUser); // curator1 имеет доступ только к TEST блоку
        var request = new CreateInteractionRequest(
            ContactId: _otherContact.Id, // Контакт в OTHER блоке
            InteractionDate: DateTime.UtcNow,
            InteractionTypeId: 1,
            ResultId: 1,
            Comment: "Test",
            StatusChangeJson: null,
            AttachmentsJson: null,
            NextTouchDate: null
        );

        // Act
        var result = await _controller.Create(request);

        // Assert
        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task Create_AsAdmin_WithAnyBlock_ShouldSucceed()
    {
        // Arrange
        SetupUser(_adminUser);
        var request = new CreateInteractionRequest(
            ContactId: _otherContact.Id,
            InteractionDate: DateTime.UtcNow,
            InteractionTypeId: 1,
            ResultId: 1,
            Comment: "Admin can access any block",
            StatusChangeJson: null,
            AttachmentsJson: null,
            NextTouchDate: null
        );

        // Act
        var result = await _controller.Create(request);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task Create_WithNonExistentContact_ShouldReturnBadRequest()
    {
        // Arrange
        SetupUser(_adminUser);
        var request = new CreateInteractionRequest(
            ContactId: 99999,
            InteractionDate: DateTime.UtcNow,
            InteractionTypeId: 1,
            ResultId: 1,
            Comment: "Test",
            StatusChangeJson: null,
            AttachmentsJson: null,
            NextTouchDate: null
        );

        // Act
        var result = await _controller.Create(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Create_ShouldCreateAuditLog()
    {
        // Arrange
        SetupUser(_curatorUser);
        var request = new CreateInteractionRequest(
            ContactId: _testContact.Id,
            InteractionDate: DateTime.UtcNow,
            InteractionTypeId: 1,
            ResultId: 1,
            Comment: "Test audit log",
            StatusChangeJson: null,
            AttachmentsJson: null,
            NextTouchDate: null
        );

        // Act
        await _controller.Create(request);

        // Assert
        var auditLog = await _context.AuditLogs
            .FirstOrDefaultAsync(a => a.EntityType == "Interaction" &&
                                     a.Action == AuditActionType.Create);

        auditLog.Should().NotBeNull();
        auditLog!.UserId.Should().Be(_curatorUser.Id);
        auditLog.NewValuesJson.Should().Contain(_testContact.ContactId);
    }

    #endregion

    #region GetAll Tests

    [Fact]
    public async Task GetAll_AsAdmin_ShouldReturnAllInteractions()
    {
        // Arrange
        SetupUser(_adminUser);

        // Создаем взаимодействия в разных блоках
        var interaction1 = new Interaction
        {
            ContactId = _testContact.Id,
            CuratorId = _curatorUser.Id,
            InteractionDate = DateTime.UtcNow,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedBy = _curatorUser.Id
        };

        var interaction2 = new Interaction
        {
            ContactId = _otherContact.Id,
            CuratorId = _otherCuratorUser.Id,
            InteractionDate = DateTime.UtcNow,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedBy = _otherCuratorUser.Id
        };

        _context.Interactions.AddRange(interaction1, interaction2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetAll();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value;
        var data = response!.GetType().GetProperty("data")!.GetValue(response) as IEnumerable<InteractionDto>;

        data.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAll_AsCurator_ShouldReturnOnlyAccessibleInteractions()
    {
        // Arrange
        SetupUser(_curatorUser); // curator1 имеет доступ только к TEST блоку

        var interaction1 = new Interaction
        {
            ContactId = _testContact.Id,
            CuratorId = _curatorUser.Id,
            InteractionDate = DateTime.UtcNow,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedBy = _curatorUser.Id
        };

        var interaction2 = new Interaction
        {
            ContactId = _otherContact.Id,
            CuratorId = _otherCuratorUser.Id,
            InteractionDate = DateTime.UtcNow,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedBy = _otherCuratorUser.Id
        };

        _context.Interactions.AddRange(interaction1, interaction2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetAll();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value;
        var data = response!.GetType().GetProperty("data")!.GetValue(response) as IEnumerable<InteractionDto>;

        data.Should().HaveCount(1);
        data!.First().ContactId.Should().Be(_testContact.Id);
    }

    [Fact]
    public async Task GetAll_ShouldDecryptComments()
    {
        // Arrange
        SetupUser(_adminUser);
        var secretComment = "This is a secret comment";

        var interaction = new Interaction
        {
            ContactId = _testContact.Id,
            CuratorId = _curatorUser.Id,
            InteractionDate = DateTime.UtcNow,
            CommentEncrypted = _encryptionService.Encrypt(secretComment),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedBy = _curatorUser.Id
        };

        _context.Interactions.Add(interaction);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetAll();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value;
        var data = response!.GetType().GetProperty("data")!.GetValue(response) as IEnumerable<InteractionDto>;

        var interactionDto = data!.First();
        interactionDto.Comment.Should().Be(secretComment);
    }

    [Fact]
    public async Task GetAll_WithFilters_ShouldApplyCorrectly()
    {
        // Arrange
        SetupUser(_adminUser);
        var pastDate = DateTime.UtcNow.AddDays(-10);
        var futureDate = DateTime.UtcNow.AddDays(-5);

        var interaction1 = new Interaction
        {
            ContactId = _testContact.Id,
            CuratorId = _curatorUser.Id,
            InteractionDate = pastDate,
            InteractionTypeId = 1,
            ResultId = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedBy = _curatorUser.Id
        };

        var interaction2 = new Interaction
        {
            ContactId = _testContact.Id,
            CuratorId = _curatorUser.Id,
            InteractionDate = futureDate,
            InteractionTypeId = 2,
            ResultId = 2,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedBy = _curatorUser.Id
        };

        _context.Interactions.AddRange(interaction1, interaction2);
        await _context.SaveChangesAsync();

        // Act - Filter by InteractionTypeId
        var result = await _controller.GetAll(interactionTypeId: 1);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value;
        var data = response!.GetType().GetProperty("data")!.GetValue(response) as IEnumerable<InteractionDto>;

        data.Should().HaveCount(1);
        data!.First().InteractionTypeId.Should().Be(1);
    }

    [Fact]
    public async Task GetAll_ShouldPaginateResults()
    {
        // Arrange
        SetupUser(_adminUser);

        // Создаем 15 взаимодействий
        for (int i = 0; i < 15; i++)
        {
            var interaction = new Interaction
            {
                ContactId = _testContact.Id,
                CuratorId = _curatorUser.Id,
                InteractionDate = DateTime.UtcNow.AddDays(-i),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedBy = _curatorUser.Id
            };
            _context.Interactions.Add(interaction);
        }
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetAll(page: 1, pageSize: 10);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value;
        var data = response!.GetType().GetProperty("data")!.GetValue(response) as IEnumerable<InteractionDto>;
        var total = (int)response.GetType().GetProperty("total")!.GetValue(response)!;

        data.Should().HaveCount(10);
        total.Should().Be(15);
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_WithValidId_ShouldReturnInteraction()
    {
        // Arrange
        SetupUser(_adminUser);
        var interaction = new Interaction
        {
            ContactId = _testContact.Id,
            CuratorId = _curatorUser.Id,
            InteractionDate = DateTime.UtcNow,
            CommentEncrypted = _encryptionService.Encrypt("Test comment"),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedBy = _curatorUser.Id
        };

        _context.Interactions.Add(interaction);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetById(interaction.Id);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = okResult.Value.Should().BeOfType<InteractionDto>().Subject;

        dto.Id.Should().Be(interaction.Id);
        dto.Comment.Should().Be("Test comment");
        dto.ContactName.Should().Be("Test Contact");
    }

    [Fact]
    public async Task GetById_WithNonExistentId_ShouldReturnNotFound()
    {
        // Arrange
        SetupUser(_adminUser);

        // Act
        var result = await _controller.GetById(99999);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetById_AsCurator_WithoutAccess_ShouldReturnForbid()
    {
        // Arrange
        SetupUser(_curatorUser); // curator1 имеет доступ только к TEST блоку

        var interaction = new Interaction
        {
            ContactId = _otherContact.Id, // Контакт в OTHER блоке
            CuratorId = _otherCuratorUser.Id,
            InteractionDate = DateTime.UtcNow,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedBy = _otherCuratorUser.Id
        };

        _context.Interactions.Add(interaction);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetById(interaction.Id);

        // Assert
        result.Should().BeOfType<ForbidResult>();
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_WithValidData_ShouldUpdateInteraction()
    {
        // Arrange
        SetupUser(_curatorUser);
        var interaction = new Interaction
        {
            ContactId = _testContact.Id,
            CuratorId = _curatorUser.Id,
            InteractionDate = DateTime.UtcNow.AddDays(-1),
            CommentEncrypted = _encryptionService.Encrypt("Old comment"),
            ResultId = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedBy = _curatorUser.Id
        };

        _context.Interactions.Add(interaction);
        await _context.SaveChangesAsync();

        var updateRequest = new UpdateInteractionRequest(
            InteractionDate: DateTime.UtcNow,
            InteractionTypeId: 2,
            ResultId: 2,
            Comment: "Updated comment",
            StatusChangeJson: null,
            NextTouchDate: DateTime.UtcNow.AddDays(14)
        );

        // Act
        var result = await _controller.Update(interaction.Id, updateRequest);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        var updated = await _context.Interactions.FindAsync(interaction.Id);
        updated!.ResultId.Should().Be(2);
        updated.InteractionTypeId.Should().Be(2);

        var decryptedComment = _encryptionService.Decrypt(updated.CommentEncrypted!);
        decryptedComment.Should().Be("Updated comment");
    }

    [Fact]
    public async Task Update_ShouldUpdateContactNextTouchDate()
    {
        // Arrange
        SetupUser(_curatorUser);
        var interaction = new Interaction
        {
            ContactId = _testContact.Id,
            CuratorId = _curatorUser.Id,
            InteractionDate = DateTime.UtcNow,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedBy = _curatorUser.Id
        };

        _context.Interactions.Add(interaction);
        await _context.SaveChangesAsync();

        var newNextTouchDate = DateTime.UtcNow.AddDays(30);
        var updateRequest = new UpdateInteractionRequest(
            InteractionDate: null,
            InteractionTypeId: 1,
            ResultId: 1,
            Comment: null,
            StatusChangeJson: null,
            NextTouchDate: newNextTouchDate
        );

        // Act
        await _controller.Update(interaction.Id, updateRequest);

        // Assert
        var updatedContact = await _context.Contacts.FindAsync(_testContact.Id);
        updatedContact!.NextTouchDate.Should().Be(newNextTouchDate);
    }

    [Fact]
    public async Task Update_AsCurator_WithoutAccess_ShouldReturnForbid()
    {
        // Arrange
        SetupUser(_curatorUser);

        var interaction = new Interaction
        {
            ContactId = _otherContact.Id,
            CuratorId = _otherCuratorUser.Id,
            InteractionDate = DateTime.UtcNow,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedBy = _otherCuratorUser.Id
        };

        _context.Interactions.Add(interaction);
        await _context.SaveChangesAsync();

        var updateRequest = new UpdateInteractionRequest(
            InteractionDate: null,
            InteractionTypeId: 1,
            ResultId: 1,
            Comment: "Try to update",
            StatusChangeJson: null,
            NextTouchDate: null
        );

        // Act
        var result = await _controller.Update(interaction.Id, updateRequest);

        // Assert
        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task Update_WithNonExistentId_ShouldReturnNotFound()
    {
        // Arrange
        SetupUser(_adminUser);
        var updateRequest = new UpdateInteractionRequest(
            InteractionDate: null,
            InteractionTypeId: 1,
            ResultId: 1,
            Comment: null,
            StatusChangeJson: null,
            NextTouchDate: null
        );

        // Act
        var result = await _controller.Update(99999, updateRequest);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    #endregion

    #region Deactivate Tests

    [Fact]
    public async Task Deactivate_ShouldSoftDelete()
    {
        // Arrange
        SetupUser(_adminUser);
        var interaction = new Interaction
        {
            ContactId = _testContact.Id,
            CuratorId = _curatorUser.Id,
            InteractionDate = DateTime.UtcNow,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedBy = _curatorUser.Id
        };

        _context.Interactions.Add(interaction);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.Deactivate(interaction.Id);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        var deleted = await _context.Interactions.FindAsync(interaction.Id);
        deleted.Should().NotBeNull(); // Still exists in database
        deleted!.IsActive.Should().BeFalse(); // But marked inactive
    }

    [Fact]
    public async Task Deactivate_ShouldCreateAuditLog()
    {
        // Arrange
        SetupUser(_adminUser);
        var interaction = new Interaction
        {
            ContactId = _testContact.Id,
            CuratorId = _curatorUser.Id,
            InteractionDate = DateTime.UtcNow,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedBy = _curatorUser.Id
        };

        _context.Interactions.Add(interaction);
        await _context.SaveChangesAsync();

        // Act
        await _controller.Deactivate(interaction.Id);

        // Assert
        var auditLog = await _context.AuditLogs
            .FirstOrDefaultAsync(a => a.EntityType == "Interaction" &&
                                     a.Action == AuditActionType.Delete &&
                                     a.EntityId == interaction.Id.ToString());

        auditLog.Should().NotBeNull();
        auditLog!.UserId.Should().Be(_adminUser.Id);
    }

    [Fact]
    public async Task Deactivate_WithNonExistentId_ShouldReturnNotFound()
    {
        // Arrange
        SetupUser(_adminUser);

        // Act
        var result = await _controller.Deactivate(99999);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    #endregion

    #region GetRecentInteractions Tests

    [Fact]
    public async Task GetRecentInteractions_ShouldReturnMostRecent()
    {
        // Arrange
        SetupUser(_adminUser);

        // Создаем 10 взаимодействий с разными датами
        for (int i = 0; i < 10; i++)
        {
            var interaction = new Interaction
            {
                ContactId = _testContact.Id,
                CuratorId = _curatorUser.Id,
                InteractionDate = DateTime.UtcNow.AddDays(-i),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedBy = _curatorUser.Id
            };
            _context.Interactions.Add(interaction);
        }
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetRecentInteractions(count: 5);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var interactions = okResult.Value.Should().BeAssignableTo<IEnumerable<InteractionDto>>().Subject;

        interactions.Should().HaveCount(5);

        // Verify they are ordered by date descending
        var dates = interactions.Select(i => i.InteractionDate).ToList();
        dates.Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task GetRecentInteractions_AsCurator_ShouldReturnOnlyAccessible()
    {
        // Arrange
        SetupUser(_curatorUser);

        // Взаимодействие в доступном блоке
        var accessibleInteraction = new Interaction
        {
            ContactId = _testContact.Id,
            CuratorId = _curatorUser.Id,
            InteractionDate = DateTime.UtcNow,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedBy = _curatorUser.Id
        };

        // Взаимодействие в недоступном блоке
        var inaccessibleInteraction = new Interaction
        {
            ContactId = _otherContact.Id,
            CuratorId = _otherCuratorUser.Id,
            InteractionDate = DateTime.UtcNow.AddHours(1), // Более поздняя дата
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedBy = _otherCuratorUser.Id
        };

        _context.Interactions.AddRange(accessibleInteraction, inaccessibleInteraction);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetRecentInteractions(count: 10);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var interactions = okResult.Value.Should().BeAssignableTo<IEnumerable<InteractionDto>>().Subject;

        interactions.Should().HaveCount(1);
        interactions.First().ContactId.Should().Be(_testContact.Id);
    }

    #endregion

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
