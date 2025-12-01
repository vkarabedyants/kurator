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

namespace Kurator.Tests.Controllers;

/// <summary>
/// Тесты для DashboardController - метрики дашборда для администраторов и кураторов
/// </summary>
public class DashboardControllerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<DashboardController> _logger;
    private readonly DashboardController _controller;
    private User _adminUser = null!;
    private User _curatorUser = null!;
    private User _otherCuratorUser = null!;
    private Block _block1 = null!;
    private Block _block2 = null!;

    public DashboardControllerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);

        // Настройка шифрования для тестов
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                {"Encryption:Key", "test-encryption-key-for-dashboard-12345"}
            }!)
            .Build();
        _encryptionService = new EncryptionService(configuration);

        _logger = LoggerFactory.Create(builder => builder.AddConsole())
            .CreateLogger<DashboardController>();
        _controller = new DashboardController(_context, _encryptionService, _logger);

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
        _block1 = new Block
        {
            Name = "Block 1",
            Code = "BLK1",
            Status = BlockStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        _block2 = new Block
        {
            Name = "Block 2",
            Code = "BLK2",
            Status = BlockStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        _context.Blocks.AddRange(_block1, _block2);
        _context.SaveChanges();

        // Назначение кураторов на блоки
        var blockCurator1 = new BlockCurator
        {
            BlockId = _block1.Id,
            UserId = _curatorUser.Id,
            CuratorType = CuratorType.Primary,
            AssignedAt = DateTime.UtcNow,
            AssignedBy = _adminUser.Id
        };

        var blockCurator2 = new BlockCurator
        {
            BlockId = _block2.Id,
            UserId = _otherCuratorUser.Id,
            CuratorType = CuratorType.Primary,
            AssignedAt = DateTime.UtcNow,
            AssignedBy = _adminUser.Id
        };

        _context.BlockCurators.AddRange(blockCurator1, blockCurator2);
        _context.SaveChanges();

        // Создание тестовых контактов и взаимодействий
        CreateTestContactsAndInteractions();
    }

    private void CreateTestContactsAndInteractions()
    {
        // Контакты в Block 1
        for (int i = 1; i <= 5; i++)
        {
            var contact = new Contact
            {
                ContactId = $"BLK1-{i:D3}",
                BlockId = _block1.Id,
                FullNameEncrypted = _encryptionService.Encrypt($"Contact Block1 {i}"),
                InfluenceStatusId = i % 3 + 1,
                InfluenceTypeId = i % 2 + 1,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                UpdatedBy = _curatorUser.Id,
                NextTouchDate = DateTime.UtcNow.AddDays(i % 3 == 0 ? -5 : 5) // Некоторые просрочены
            };

            _context.Contacts.Add(contact);
            _context.SaveChanges();

            // Добавляем взаимодействия для каждого контакта
            var interaction = new Interaction
            {
                ContactId = contact.Id,
                CuratorId = _curatorUser.Id,
                InteractionDate = DateTime.UtcNow.AddDays(-7),
                InteractionTypeId = i % 2 + 1,
                ResultId = i % 2 + 1,
                CommentEncrypted = _encryptionService.Encrypt($"Interaction for {contact.ContactId}"),
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-7),
                UpdatedBy = _curatorUser.Id
            };

            _context.Interactions.Add(interaction);

            // Обновляем LastInteractionDate
            contact.LastInteractionDate = interaction.InteractionDate;
        }

        // Контакты в Block 2
        for (int i = 1; i <= 3; i++)
        {
            var contact = new Contact
            {
                ContactId = $"BLK2-{i:D3}",
                BlockId = _block2.Id,
                FullNameEncrypted = _encryptionService.Encrypt($"Contact Block2 {i}"),
                InfluenceStatusId = i % 2 + 1,
                InfluenceTypeId = 1,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-20),
                UpdatedBy = _otherCuratorUser.Id,
                NextTouchDate = DateTime.UtcNow.AddDays(10)
            };

            _context.Contacts.Add(contact);
        }

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

    #region GetCuratorDashboard Tests

    [Fact]
    public async Task GetCuratorDashboard_AsAdmin_ShouldReturnEmptyDashboard()
    {
        // Arrange
        SetupUser(_adminUser);

        // Act
        var result = await _controller.GetCuratorDashboard();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var dashboard = okResult.Value.Should().BeOfType<CuratorDashboardDto>().Subject;

        // Admin не назначен ни на один блок как куратор, но это нормально
        dashboard.Should().NotBeNull();
    }

    [Fact]
    public async Task GetCuratorDashboard_AsCurator_ShouldReturnOnlyAccessibleMetrics()
    {
        // Arrange
        SetupUser(_curatorUser); // curator1 имеет доступ только к Block 1

        // Act
        var result = await _controller.GetCuratorDashboard();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var dashboard = okResult.Value.Should().BeOfType<CuratorDashboardDto>().Subject;

        // Должен видеть только контакты из Block 1 (5 контактов)
        dashboard.TotalContacts.Should().Be(5);

        // Должен видеть только взаимодействия из Block 1
        dashboard.InteractionsLastMonth.Should().BeGreaterThan(0);

        // Проверяем просроченные контакты
        dashboard.OverdueContacts.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetCuratorDashboard_ShouldCalculateAverageInteractionInterval()
    {
        // Arrange
        SetupUser(_curatorUser);

        // Act
        var result = await _controller.GetCuratorDashboard();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var dashboard = okResult.Value.Should().BeOfType<CuratorDashboardDto>().Subject;

        // Средний интервал должен быть рассчитан (примерно 7 дней)
        dashboard.AverageInteractionInterval.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetCuratorDashboard_ShouldReturnRecentInteractions()
    {
        // Arrange
        SetupUser(_curatorUser);

        // Act
        var result = await _controller.GetCuratorDashboard();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var dashboard = okResult.Value.Should().BeOfType<CuratorDashboardDto>().Subject;

        dashboard.RecentInteractions.Should().NotBeEmpty();
        dashboard.RecentInteractions.Should().HaveCountLessOrEqualTo(5);

        // Проверяем дешифровку имен
        dashboard.RecentInteractions.All(i => !string.IsNullOrEmpty(i.ContactName)).Should().BeTrue();
    }

    [Fact]
    public async Task GetCuratorDashboard_ShouldReturnContactsRequiringAttention()
    {
        // Arrange
        SetupUser(_curatorUser);

        // Act
        var result = await _controller.GetCuratorDashboard();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var dashboard = okResult.Value.Should().BeOfType<CuratorDashboardDto>().Subject;

        // Должны быть просроченные контакты
        if (dashboard.OverdueContacts > 0)
        {
            dashboard.ContactsRequiringAttention.Should().NotBeEmpty();

            // Все просроченные контакты должны иметь положительное количество дней просрочки
            dashboard.ContactsRequiringAttention.All(c => c.DaysOverdue > 0).Should().BeTrue();

            // Имена должны быть дешифрованы
            dashboard.ContactsRequiringAttention.All(c => !string.IsNullOrEmpty(c.FullName)).Should().BeTrue();
        }
    }

    [Fact]
    public async Task GetCuratorDashboard_ShouldReturnContactsByInfluenceStatus()
    {
        // Arrange
        SetupUser(_curatorUser);

        // Act
        var result = await _controller.GetCuratorDashboard();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var dashboard = okResult.Value.Should().BeOfType<CuratorDashboardDto>().Subject;

        dashboard.ContactsByInfluenceStatus.Should().NotBeEmpty();
        dashboard.ContactsByInfluenceStatus.Values.Sum().Should().Be(dashboard.TotalContacts);
    }

    [Fact]
    public async Task GetCuratorDashboard_ShouldReturnInteractionsByType()
    {
        // Arrange
        SetupUser(_curatorUser);

        // Act
        var result = await _controller.GetCuratorDashboard();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var dashboard = okResult.Value.Should().BeOfType<CuratorDashboardDto>().Subject;

        dashboard.InteractionsByType.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetCuratorDashboard_CuratorWithoutBlocks_ShouldReturnEmptyDashboard()
    {
        // Arrange
        var newCurator = new User
        {
            Login = "curator_no_blocks",
            PasswordHash = "hash",
            Role = UserRole.Curator,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(newCurator);
        _context.SaveChanges();

        SetupUser(newCurator);

        // Act
        var result = await _controller.GetCuratorDashboard();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var dashboard = okResult.Value.Should().BeOfType<CuratorDashboardDto>().Subject;

        dashboard.TotalContacts.Should().Be(0);
        dashboard.InteractionsLastMonth.Should().Be(0);
        dashboard.OverdueContacts.Should().Be(0);
        dashboard.RecentInteractions.Should().BeEmpty();
        dashboard.ContactsRequiringAttention.Should().BeEmpty();
    }

    #endregion

    #region GetAdminDashboard Tests

    [Fact]
    public async Task GetAdminDashboard_ShouldReturnAllMetrics()
    {
        // Arrange
        SetupUser(_adminUser);

        // Act
        var result = await _controller.GetAdminDashboard();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var dashboard = okResult.Value.Should().BeOfType<AdminDashboardDto>().Subject;

        // Проверяем общие метрики
        dashboard.TotalContacts.Should().Be(8); // 5 из Block 1 + 3 из Block 2
        dashboard.TotalInteractions.Should().BeGreaterThan(0);
        dashboard.TotalBlocks.Should().Be(2);
        dashboard.TotalUsers.Should().Be(3); // admin + 2 curators

        // Проверяем метрики за последний месяц
        dashboard.InteractionsLastMonth.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetAdminDashboard_ShouldReturnContactsByBlock()
    {
        // Arrange
        SetupUser(_adminUser);

        // Act
        var result = await _controller.GetAdminDashboard();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var dashboard = okResult.Value.Should().BeOfType<AdminDashboardDto>().Subject;

        dashboard.ContactsByBlock.Should().NotBeEmpty();
        dashboard.ContactsByBlock.Should().ContainKey("Block 1");
        dashboard.ContactsByBlock.Should().ContainKey("Block 2");
        dashboard.ContactsByBlock["Block 1"].Should().Be(5);
        dashboard.ContactsByBlock["Block 2"].Should().Be(3);
    }

    [Fact]
    public async Task GetAdminDashboard_ShouldReturnContactsByInfluenceStatus()
    {
        // Arrange
        SetupUser(_adminUser);

        // Act
        var result = await _controller.GetAdminDashboard();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var dashboard = okResult.Value.Should().BeOfType<AdminDashboardDto>().Subject;

        dashboard.ContactsByInfluenceStatus.Should().NotBeEmpty();
        dashboard.ContactsByInfluenceStatus.Values.Sum().Should().Be(dashboard.TotalContacts);
    }

    [Fact]
    public async Task GetAdminDashboard_ShouldReturnContactsByInfluenceType()
    {
        // Arrange
        SetupUser(_adminUser);

        // Act
        var result = await _controller.GetAdminDashboard();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var dashboard = okResult.Value.Should().BeOfType<AdminDashboardDto>().Subject;

        dashboard.ContactsByInfluenceType.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetAdminDashboard_ShouldReturnTopCuratorsByActivity()
    {
        // Arrange
        SetupUser(_adminUser);

        // Act
        var result = await _controller.GetAdminDashboard();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var dashboard = okResult.Value.Should().BeOfType<AdminDashboardDto>().Subject;

        dashboard.TopCuratorsByActivity.Should().NotBeEmpty();
        dashboard.TopCuratorsByActivity.Should().ContainKey("curator1");
        dashboard.TopCuratorsByActivity.Count.Should().BeLessThanOrEqualTo(5);
    }

    [Fact]
    public async Task GetAdminDashboard_ShouldReturnRecentAuditLogs()
    {
        // Arrange
        SetupUser(_adminUser);

        // Создаем несколько audit logs
        for (int i = 0; i < 5; i++)
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
        var result = await _controller.GetAdminDashboard();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var dashboard = okResult.Value.Should().BeOfType<AdminDashboardDto>().Subject;

        dashboard.RecentAuditLogs.Should().NotBeEmpty();
        dashboard.RecentAuditLogs.Count.Should().BeLessThanOrEqualTo(20);

        // Проверяем сортировку по времени (от новых к старым)
        var timestamps = dashboard.RecentAuditLogs.Select(a => a.Timestamp).ToList();
        timestamps.Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task GetAdminDashboard_ShouldReturnStatusChangeDynamics()
    {
        // Arrange
        SetupUser(_adminUser);

        // Создаем историю изменений статусов
        var statusHistory1 = new InfluenceStatusHistory
        {
            ContactId = _context.Contacts.First().Id,
            PreviousStatus = "1",
            NewStatus = "2",
            ChangedByUserId = _curatorUser.Id,
            ChangedAt = DateTime.UtcNow.AddDays(-10)
        };

        var statusHistory2 = new InfluenceStatusHistory
        {
            ContactId = _context.Contacts.Skip(1).First().Id,
            PreviousStatus = "1",
            NewStatus = "2",
            ChangedByUserId = _curatorUser.Id,
            ChangedAt = DateTime.UtcNow.AddDays(-5)
        };

        _context.InfluenceStatusHistories.AddRange(statusHistory1, statusHistory2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetAdminDashboard();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var dashboard = okResult.Value.Should().BeOfType<AdminDashboardDto>().Subject;

        dashboard.StatusChangeDynamics.Should().NotBeEmpty();
        dashboard.StatusChangeDynamics.Should().ContainKey("1→2");
    }

    #endregion

    #region GetStatistics Tests

    [Fact]
    public async Task GetStatistics_AsAdmin_ShouldReturnAllStatistics()
    {
        // Arrange
        SetupUser(_adminUser);
        var fromDate = DateTime.UtcNow.AddMonths(-1);
        var toDate = DateTime.UtcNow;

        // Act
        var result = await _controller.GetStatistics(fromDate, toDate);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value;

        response.Should().NotBeNull();

        // Проверяем наличие ключевых полей
        var totalInteractions = response!.GetType().GetProperty("totalInteractions")!.GetValue(response);
        var uniqueContacts = response.GetType().GetProperty("uniqueContacts")!.GetValue(response);

        totalInteractions.Should().NotBeNull();
        uniqueContacts.Should().NotBeNull();
    }

    [Fact]
    public async Task GetStatistics_AsCurator_ShouldReturnOnlyAccessibleStatistics()
    {
        // Arrange
        SetupUser(_curatorUser);
        var fromDate = DateTime.UtcNow.AddMonths(-1);
        var toDate = DateTime.UtcNow;

        // Act
        var result = await _controller.GetStatistics(fromDate, toDate);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();

        // Curator должен видеть только статистику по своим блокам
    }

    [Fact]
    public async Task GetStatistics_WithBlockFilter_ShouldFilterByBlock()
    {
        // Arrange
        SetupUser(_adminUser);
        var fromDate = DateTime.UtcNow.AddMonths(-1);
        var toDate = DateTime.UtcNow;

        // Act
        var result = await _controller.GetStatistics(fromDate, toDate, blockId: _block1.Id);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetStatistics_WithDefaultDates_ShouldUseLastMonth()
    {
        // Arrange
        SetupUser(_adminUser);

        // Act
        var result = await _controller.GetStatistics();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value;

        response.Should().NotBeNull();

        var period = response!.GetType().GetProperty("period")!.GetValue(response);
        period.Should().NotBeNull();
    }

    [Fact]
    public async Task GetStatistics_ShouldGroupByType()
    {
        // Arrange
        SetupUser(_adminUser);
        var fromDate = DateTime.UtcNow.AddMonths(-1);
        var toDate = DateTime.UtcNow;

        // Act
        var result = await _controller.GetStatistics(fromDate, toDate);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value;

        var byType = response!.GetType().GetProperty("byType")!.GetValue(response) as Dictionary<string, int>;
        byType.Should().NotBeNull();
    }

    [Fact]
    public async Task GetStatistics_ShouldGroupByResult()
    {
        // Arrange
        SetupUser(_adminUser);
        var fromDate = DateTime.UtcNow.AddMonths(-1);
        var toDate = DateTime.UtcNow;

        // Act
        var result = await _controller.GetStatistics(fromDate, toDate);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value;

        var byResult = response!.GetType().GetProperty("byResult")!.GetValue(response) as Dictionary<string, int>;
        byResult.Should().NotBeNull();
    }

    #endregion

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
