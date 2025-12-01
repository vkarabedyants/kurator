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
/// Минимальные тесты безопасности для проверки ключевых сценариев доступа
/// Тесты покрывают: изоляцию данных между блоками, шифрование, контроль доступа
/// </summary>
public class SecurityValidationTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IEncryptionService _encryptionService;
    private readonly ContactService _contactService;
    private readonly InteractionService _interactionService;
    private User _adminUser = null!;
    private User _curatorUser1 = null!;
    private User _curatorUser2 = null!;
    private Block _block1 = null!;
    private Block _block2 = null!;

    public SecurityValidationTests()
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
        // Создание пользователей
        _adminUser = new User { Login = "admin", PasswordHash = "hash", Role = UserRole.Admin, IsActive = true };
        _curatorUser1 = new User { Login = "curator1", PasswordHash = "hash", Role = UserRole.Curator, IsActive = true };
        _curatorUser2 = new User { Login = "curator2", PasswordHash = "hash", Role = UserRole.Curator, IsActive = true };

        _context.Users.AddRange(_adminUser, _curatorUser1, _curatorUser2);

        // Создание блоков
        _block1 = new Block { Name = "Блок 1", Code = "BLK1", Status = BlockStatus.Active };
        _block2 = new Block { Name = "Блок 2", Code = "BLK2", Status = BlockStatus.Active };

        _context.Blocks.AddRange(_block1, _block2);
        _context.SaveChanges();

        // Назначение кураторов
        _context.BlockCurators.Add(new BlockCurator
        {
            BlockId = _block1.Id,
            UserId = _curatorUser1.Id,
            AssignedAt = DateTime.UtcNow
        });
        _context.BlockCurators.Add(new BlockCurator
        {
            BlockId = _block2.Id,
            UserId = _curatorUser2.Id,
            AssignedAt = DateTime.UtcNow
        });
        _context.SaveChanges();
    }

    #region Изоляция данных между блоками

    [Fact]
    public async Task Curator_ShouldNotSeeContactsFromOtherBlocks()
    {
        // Arrange: создаем контакты в обоих блоках
        var contact1 = await _contactService.CreateContactAsync(
            _block1.Id, "Контакт Блока 1", _curatorUser1.Id, isAdmin: false);
        var contact2 = await _contactService.CreateContactAsync(
            _block2.Id, "Контакт Блока 2", _adminUser.Id, isAdmin: true);

        // Act: curator1 получает список контактов
        var (contacts, total) = await _contactService.GetContactsAsync(_curatorUser1.Id, isAdmin: false);

        // Assert: curator1 видит только контакты своего блока
        contacts.Should().HaveCount(1);
        contacts.First().BlockId.Should().Be(_block1.Id);
        total.Should().Be(1);
    }

    [Fact]
    public async Task Curator_ShouldNotAccessContactFromOtherBlock()
    {
        // Arrange: создаем контакт в блоке 2
        var contact = await _contactService.CreateContactAsync(
            _block2.Id, "Секретный контакт", _adminUser.Id, isAdmin: true);

        // Act: curator1 (из блока 1) пытается получить контакт
        var result = await _contactService.GetContactByIdAsync(contact.Id, _curatorUser1.Id, isAdmin: false);

        // Assert: доступ запрещен
        result.Should().BeNull();
    }

    [Fact]
    public async Task Curator_ShouldNotCreateContactInOtherBlock()
    {
        // Act & Assert: curator1 пытается создать контакт в блоке 2
        Func<Task> act = async () => await _contactService.CreateContactAsync(
            _block2.Id, "Несанкционированный контакт", _curatorUser1.Id, isAdmin: false);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Curator_ShouldNotUpdateContactFromOtherBlock()
    {
        // Arrange: создаем контакт в блоке 2
        var contact = await _contactService.CreateContactAsync(
            _block2.Id, "Контакт блока 2", _adminUser.Id, isAdmin: true);

        // Act & Assert: curator1 пытается обновить контакт
        Func<Task> act = async () => await _contactService.UpdateContactAsync(
            contact.Id, _curatorUser1.Id, isAdmin: false, position: "Хакер");

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Admin_ShouldAccessAllBlocks()
    {
        // Arrange: создаем контакты в обоих блоках
        await _contactService.CreateContactAsync(_block1.Id, "Контакт 1", _adminUser.Id, isAdmin: true);
        await _contactService.CreateContactAsync(_block2.Id, "Контакт 2", _adminUser.Id, isAdmin: true);

        // Act: админ получает все контакты
        var (contacts, total) = await _contactService.GetContactsAsync(_adminUser.Id, isAdmin: true);

        // Assert: админ видит все контакты
        contacts.Should().HaveCount(2);
        total.Should().Be(2);
    }

    #endregion

    #region Шифрование данных

    [Fact]
    public async Task Contact_FullNameShouldBeEncrypted()
    {
        // Arrange
        var sensitiveData = "Иванов Иван Иванович";

        // Act
        var contact = await _contactService.CreateContactAsync(
            _block1.Id, sensitiveData, _adminUser.Id, isAdmin: true);

        // Assert: ФИО хранится в зашифрованном виде
        contact.FullNameEncrypted.Should().NotBe(sensitiveData);
        contact.FullNameEncrypted.Should().NotBeNullOrEmpty();

        // Проверяем что можно расшифровать
        var decrypted = _encryptionService.Decrypt(contact.FullNameEncrypted);
        decrypted.Should().Be(sensitiveData);
    }

    [Fact]
    public async Task Contact_NotesShouldBeEncrypted()
    {
        // Arrange
        var sensitiveNotes = "Конфиденциальные заметки о контакте";

        // Act
        var contact = await _contactService.CreateContactAsync(
            _block1.Id, "Тестовый контакт", _adminUser.Id, isAdmin: true, notes: sensitiveNotes);

        // Assert: заметки хранятся в зашифрованном виде
        contact.NotesEncrypted.Should().NotBe(sensitiveNotes);
        contact.NotesEncrypted.Should().NotBeNullOrEmpty();

        var decrypted = _encryptionService.Decrypt(contact.NotesEncrypted!);
        decrypted.Should().Be(sensitiveNotes);
    }

    [Fact]
    public async Task Interaction_CommentShouldBeEncrypted()
    {
        // Arrange
        var contact = await _contactService.CreateContactAsync(
            _block1.Id, "Тест", _curatorUser1.Id, isAdmin: false);
        var sensitiveComment = "Конфиденциальный комментарий о встрече";

        // Act
        var interaction = await _interactionService.CreateInteractionAsync(
            contact.Id, _curatorUser1.Id, isAdmin: false, comment: sensitiveComment);

        // Assert
        interaction.CommentEncrypted.Should().NotBe(sensitiveComment);
        var decrypted = _encryptionService.Decrypt(interaction.CommentEncrypted!);
        decrypted.Should().Be(sensitiveComment);
    }

    [Fact]
    public void Encryption_ShouldProduceSameCiphertexts()
    {
        // Arrange
        var plaintext = "Одинаковый текст";

        // Act
        var ciphertext1 = _encryptionService.Encrypt(plaintext);
        var ciphertext2 = _encryptionService.Encrypt(plaintext);

        // Assert: шифрование детерминистическое (без случайного IV)
        ciphertext1.Should().Be(ciphertext2);

        // Но оба расшифровываются корректно
        _encryptionService.Decrypt(ciphertext1).Should().Be(plaintext);
        _encryptionService.Decrypt(ciphertext2).Should().Be(plaintext);
    }

    #endregion

    #region Аудит операций

    [Fact]
    public async Task Contact_CreationShouldBeAudited()
    {
        // Act
        var contact = await _contactService.CreateContactAsync(
            _block1.Id, "Аудит контакт", _adminUser.Id, isAdmin: true);

        // Assert
        var auditLog = await _context.AuditLogs
            .FirstOrDefaultAsync(a => a.EntityType == "Contact" && a.EntityId == contact.Id.ToString());

        auditLog.Should().NotBeNull();
        auditLog!.Action.Should().Be(AuditActionType.Create);
        auditLog.UserId.Should().Be(_adminUser.Id);
        auditLog.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Contact_DeletionShouldBeAudited()
    {
        // Arrange
        var contact = await _contactService.CreateContactAsync(
            _block1.Id, "Удаляемый контакт", _adminUser.Id, isAdmin: true);

        // Act
        await _contactService.DeleteContactAsync(contact.Id, _adminUser.Id);

        // Assert
        var auditLog = await _context.AuditLogs
            .Where(a => a.EntityType == "Contact" && a.EntityId == contact.Id.ToString())
            .OrderByDescending(a => a.Timestamp)
            .FirstOrDefaultAsync();

        auditLog.Should().NotBeNull();
        auditLog!.Action.Should().Be(AuditActionType.Delete);
    }

    [Fact]
    public async Task Contact_StatusChangeShouldBeRecorded()
    {
        // Arrange
        var contact = await _contactService.CreateContactAsync(
            _block1.Id, "Статусный контакт", _adminUser.Id, isAdmin: true, influenceStatusId: 1);

        // Act: изменяем статус
        await _contactService.UpdateContactAsync(
            contact.Id, _adminUser.Id, isAdmin: true, influenceStatusId: 2);

        // Assert: запись в истории статусов
        var statusHistory = await _context.InfluenceStatusHistories
            .FirstOrDefaultAsync(h => h.ContactId == contact.Id);

        statusHistory.Should().NotBeNull();
        statusHistory!.PreviousStatus.Should().Be("1");
        statusHistory.NewStatus.Should().Be("2");
    }

    #endregion

    #region Мягкое удаление

    [Fact]
    public async Task Contact_DeletionShouldBeSoft()
    {
        // Arrange
        var contact = await _contactService.CreateContactAsync(
            _block1.Id, "Мягко удаляемый", _adminUser.Id, isAdmin: true);

        // Act
        await _contactService.DeleteContactAsync(contact.Id, _adminUser.Id);

        // Assert: контакт не удален физически
        var dbContact = await _context.Contacts.FindAsync(contact.Id);
        dbContact.Should().NotBeNull();
        dbContact!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Contact_DeletedShouldNotAppearInLists()
    {
        // Arrange
        var contact = await _contactService.CreateContactAsync(
            _block1.Id, "Скрытый контакт", _adminUser.Id, isAdmin: true);
        await _contactService.DeleteContactAsync(contact.Id, _adminUser.Id);

        // Act
        var (contacts, total) = await _contactService.GetContactsAsync(_adminUser.Id, isAdmin: true);

        // Assert
        contacts.Should().NotContain(c => c.Id == contact.Id);
    }

    [Fact]
    public async Task Interaction_DeactivationShouldBeSoft()
    {
        // Arrange
        var contact = await _contactService.CreateContactAsync(
            _block1.Id, "Тест", _curatorUser1.Id, isAdmin: false);
        var interaction = await _interactionService.CreateInteractionAsync(
            contact.Id, _curatorUser1.Id, isAdmin: false);

        // Act
        await _interactionService.DeactivateInteractionAsync(interaction.Id, _adminUser.Id);

        // Assert
        var dbInteraction = await _context.Interactions.FindAsync(interaction.Id);
        dbInteraction.Should().NotBeNull();
        dbInteraction!.IsActive.Should().BeFalse();
    }

    #endregion

    #region Просроченные контакты

    [Fact]
    public async Task OverdueContacts_ShouldBeCorrectlyIdentified()
    {
        // Arrange
        var overdueContact = await _contactService.CreateContactAsync(
            _block1.Id, "Просроченный", _curatorUser1.Id, isAdmin: false,
            nextTouchDate: DateTime.UtcNow.AddDays(-10));

        var futureContact = await _contactService.CreateContactAsync(
            _block1.Id, "Будущий", _curatorUser1.Id, isAdmin: false,
            nextTouchDate: DateTime.UtcNow.AddDays(10));

        // Act
        var overdue = await _contactService.GetOverdueContactsAsync(_curatorUser1.Id, isAdmin: false);

        // Assert
        overdue.Should().HaveCount(1);
        overdue.First().Id.Should().Be(overdueContact.Id);
    }

    [Fact]
    public async Task OverdueContacts_CuratorShouldOnlySeeOwnBlocks()
    {
        // Arrange
        await _contactService.CreateContactAsync(
            _block1.Id, "Мой просроченный", _curatorUser1.Id, isAdmin: false,
            nextTouchDate: DateTime.UtcNow.AddDays(-5));
        await _contactService.CreateContactAsync(
            _block2.Id, "Чужой просроченный", _adminUser.Id, isAdmin: true,
            nextTouchDate: DateTime.UtcNow.AddDays(-5));

        // Act
        var overdue = await _contactService.GetOverdueContactsAsync(_curatorUser1.Id, isAdmin: false);

        // Assert: curator1 видит только свои просроченные контакты
        overdue.Should().HaveCount(1);
        overdue.All(c => c.BlockId == _block1.Id).Should().BeTrue();
    }

    #endregion

    #region Архивированные блоки

    [Fact]
    public async Task ArchivedBlock_ContactsShouldNotAppearInLists()
    {
        // Arrange
        var archivedBlock = new Block { Name = "Архив", Code = "ARCH", Status = BlockStatus.Archived };
        _context.Blocks.Add(archivedBlock);
        await _context.SaveChangesAsync();

        var contact = new Contact
        {
            ContactId = "ARCH-001",
            BlockId = archivedBlock.Id,
            FullNameEncrypted = _encryptionService.Encrypt("Архивный контакт"),
            ResponsibleCuratorId = _adminUser.Id,
            IsActive = true,
            UpdatedBy = _adminUser.Id
        };
        _context.Contacts.Add(contact);
        await _context.SaveChangesAsync();

        // Act
        var (contacts, total) = await _contactService.GetContactsAsync(_adminUser.Id, isAdmin: true);

        // Assert
        contacts.Should().NotContain(c => c.BlockId == archivedBlock.Id);
    }

    #endregion

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
