using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Kurator.Api.Controllers;
using Kurator.Core.Entities;
using Kurator.Core.Enums;
using Kurator.Infrastructure.Data;
using System.Security.Claims;

namespace Kurator.Tests.Controllers;

/// <summary>
/// Тесты для WatchlistController - управление вотчлистом для аналитиков угроз
/// </summary>
public class WatchlistControllerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<WatchlistController> _logger;
    private readonly WatchlistController _controller;
    private User _adminUser = null!;
    private User _threatAnalystUser = null!;
    private User _curatorUser = null!;

    public WatchlistControllerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);

        _logger = LoggerFactory.Create(builder => builder.AddConsole())
            .CreateLogger<WatchlistController>();
        _controller = new WatchlistController(_context, _logger);

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

        _threatAnalystUser = new User
        {
            Login = "analyst1",
            PasswordHash = "hash",
            Role = UserRole.ThreatAnalyst,
            CreatedAt = DateTime.UtcNow
        };

        _curatorUser = new User
        {
            Login = "curator1",
            PasswordHash = "hash",
            Role = UserRole.Curator,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.AddRange(_adminUser, _threatAnalystUser, _curatorUser);
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

    #region GetAll Tests

    [Fact]
    public async Task GetAll_AsThreatAnalyst_ShouldReturnAllWatchlistItems()
    {
        // Arrange
        SetupUser(_threatAnalystUser);

        var watchlistItem1 = new Watchlist
        {
            FullName = "Test Person 1",
            RiskLevel = RiskLevel.High,
            MonitoringFrequency = MonitoringFrequency.Weekly,
            WatchOwnerId = _threatAnalystUser.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedBy = _threatAnalystUser.Id
        };

        var watchlistItem2 = new Watchlist
        {
            FullName = "Test Person 2",
            RiskLevel = RiskLevel.Medium,
            MonitoringFrequency = MonitoringFrequency.Monthly,
            WatchOwnerId = _threatAnalystUser.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedBy = _threatAnalystUser.Id
        };

        _context.Watchlists.AddRange(watchlistItem1, watchlistItem2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetAll();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value;
        var data = response!.GetType().GetProperty("data")!.GetValue(response) as IEnumerable<WatchlistDto>;

        data.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAll_ShouldFilterByRiskLevel()
    {
        // Arrange
        SetupUser(_adminUser);

        var highRiskItem = new Watchlist
        {
            FullName = "High Risk Person",
            RiskLevel = RiskLevel.High,
            MonitoringFrequency = MonitoringFrequency.Weekly,
            WatchOwnerId = _adminUser.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedBy = _adminUser.Id
        };

        var lowRiskItem = new Watchlist
        {
            FullName = "Low Risk Person",
            RiskLevel = RiskLevel.Low,
            MonitoringFrequency = MonitoringFrequency.Monthly,
            WatchOwnerId = _adminUser.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedBy = _adminUser.Id
        };

        _context.Watchlists.AddRange(highRiskItem, lowRiskItem);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetAll(riskLevel: RiskLevel.High);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value;
        var data = response!.GetType().GetProperty("data")!.GetValue(response) as IEnumerable<WatchlistDto>;

        data.Should().HaveCount(1);
        data!.First().FullName.Should().Be("High Risk Person");
    }

    [Fact]
    public async Task GetAll_ShouldFilterByMonitoringFrequency()
    {
        // Arrange
        SetupUser(_threatAnalystUser);

        var weeklyItem = new Watchlist
        {
            FullName = "Weekly Monitoring",
            RiskLevel = RiskLevel.Critical,
            MonitoringFrequency = MonitoringFrequency.Weekly,
            WatchOwnerId = _threatAnalystUser.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedBy = _threatAnalystUser.Id
        };

        var monthlyItem = new Watchlist
        {
            FullName = "Monthly Monitoring",
            RiskLevel = RiskLevel.High,
            MonitoringFrequency = MonitoringFrequency.Monthly,
            WatchOwnerId = _threatAnalystUser.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedBy = _threatAnalystUser.Id
        };

        _context.Watchlists.AddRange(weeklyItem, monthlyItem);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetAll(monitoringFrequency: MonitoringFrequency.Weekly);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value;
        var data = response!.GetType().GetProperty("data")!.GetValue(response) as IEnumerable<WatchlistDto>;

        data.Should().HaveCount(1);
        data!.First().MonitoringFrequency.Should().Be("Weekly");
    }

    [Fact]
    public async Task GetAll_ShouldFilterItemsRequiringCheck()
    {
        // Arrange
        SetupUser(_adminUser);

        var overdueItem = new Watchlist
        {
            FullName = "Overdue Check",
            RiskLevel = RiskLevel.High,
            MonitoringFrequency = MonitoringFrequency.Weekly,
            NextCheckDate = DateTime.UtcNow.AddDays(-5),
            WatchOwnerId = _adminUser.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedBy = _adminUser.Id
        };

        var futureItem = new Watchlist
        {
            FullName = "Future Check",
            RiskLevel = RiskLevel.Medium,
            MonitoringFrequency = MonitoringFrequency.Monthly,
            NextCheckDate = DateTime.UtcNow.AddDays(10),
            WatchOwnerId = _adminUser.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedBy = _adminUser.Id
        };

        _context.Watchlists.AddRange(overdueItem, futureItem);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetAll(requiresCheck: true);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value;
        var data = response!.GetType().GetProperty("data")!.GetValue(response) as IEnumerable<WatchlistDto>;

        data.Should().HaveCount(1);
        data!.First().FullName.Should().Be("Overdue Check");
        data.First().RequiresCheck.Should().BeTrue();
    }

    [Fact]
    public async Task GetAll_ShouldPaginateResults()
    {
        // Arrange
        SetupUser(_adminUser);

        for (int i = 1; i <= 15; i++)
        {
            var item = new Watchlist
            {
                FullName = $"Person {i}",
                RiskLevel = RiskLevel.Medium,
                MonitoringFrequency = MonitoringFrequency.Weekly,
                WatchOwnerId = _adminUser.Id,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedBy = _adminUser.Id
            };
            _context.Watchlists.Add(item);
        }
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetAll(page: 1, pageSize: 10);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value;
        var data = response!.GetType().GetProperty("data")!.GetValue(response) as IEnumerable<WatchlistDto>;
        var total = (int)response.GetType().GetProperty("total")!.GetValue(response)!;

        data.Should().HaveCount(10);
        total.Should().Be(15);
    }

    [Fact]
    public async Task GetAll_ShouldNotReturnInactiveItems()
    {
        // Arrange
        SetupUser(_adminUser);

        var activeItem = new Watchlist
        {
            FullName = "Active Person",
            RiskLevel = RiskLevel.High,
            MonitoringFrequency = MonitoringFrequency.Weekly,
            WatchOwnerId = _adminUser.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedBy = _adminUser.Id
        };

        var inactiveItem = new Watchlist
        {
            FullName = "Inactive Person",
            RiskLevel = RiskLevel.High,
            MonitoringFrequency = MonitoringFrequency.Weekly,
            WatchOwnerId = _adminUser.Id,
            IsActive = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedBy = _adminUser.Id
        };

        _context.Watchlists.AddRange(activeItem, inactiveItem);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetAll();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value;
        var data = response!.GetType().GetProperty("data")!.GetValue(response) as IEnumerable<WatchlistDto>;

        data.Should().HaveCount(1);
        data!.First().FullName.Should().Be("Active Person");
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_WithValidId_ShouldReturnWatchlistItem()
    {
        // Arrange
        SetupUser(_threatAnalystUser);

        var watchlistItem = new Watchlist
        {
            FullName = "Test Person",
            RoleStatus = "Director",
            RiskLevel = RiskLevel.High,
            MonitoringFrequency = MonitoringFrequency.Weekly,
            ThreatSource = "Political conflict",
            DynamicsDescription = "Increasing risk",
            WatchOwnerId = _threatAnalystUser.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedBy = _threatAnalystUser.Id
        };

        _context.Watchlists.Add(watchlistItem);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetById(watchlistItem.Id);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = okResult.Value.Should().BeOfType<WatchlistDto>().Subject;

        dto.Id.Should().Be(watchlistItem.Id);
        dto.FullName.Should().Be("Test Person");
        dto.RoleStatus.Should().Be("Director");
        dto.RiskLevel.Should().Be("High");
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

    #endregion

    #region Create Tests

    [Fact]
    public async Task Create_WithValidData_ShouldCreateWatchlistItem()
    {
        // Arrange
        SetupUser(_threatAnalystUser);

        var request = new CreateWatchlistRequest(
            FullName: "New Threat Person",
            RoleStatus: "Manager",
            RiskSphereId: 1,
            ThreatSource: "Economic sanctions",
            ConflictDate: DateTime.UtcNow.AddMonths(-2),
            RiskLevel: RiskLevel.Critical,
            MonitoringFrequency: MonitoringFrequency.Weekly,
            LastCheckDate: DateTime.UtcNow,
            NextCheckDate: DateTime.UtcNow.AddDays(1),
            DynamicsDescription: "High priority monitoring",
            WatchOwnerId: null,
            AttachmentsJson: null
        );

        // Act
        var result = await _controller.Create(request);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();

        var watchlistItem = await _context.Watchlists
            .FirstOrDefaultAsync(w => w.FullName == "New Threat Person");

        watchlistItem.Should().NotBeNull();
        watchlistItem!.RiskLevel.Should().Be(RiskLevel.Critical);
        watchlistItem.MonitoringFrequency.Should().Be(MonitoringFrequency.Weekly);
        watchlistItem.WatchOwnerId.Should().Be(_threatAnalystUser.Id); // Должен быть установлен текущий пользователь
        watchlistItem.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Create_WithSpecifiedWatchOwner_ShouldUseSpecifiedOwner()
    {
        // Arrange
        SetupUser(_adminUser);

        var request = new CreateWatchlistRequest(
            FullName: "Assigned Person",
            RoleStatus: null,
            RiskSphereId: null,
            ThreatSource: null,
            ConflictDate: null,
            RiskLevel: RiskLevel.Medium,
            MonitoringFrequency: MonitoringFrequency.Weekly,
            LastCheckDate: null,
            NextCheckDate: null,
            DynamicsDescription: null,
            WatchOwnerId: _threatAnalystUser.Id,
            AttachmentsJson: null
        );

        // Act
        await _controller.Create(request);

        // Assert
        var watchlistItem = await _context.Watchlists
            .FirstOrDefaultAsync(w => w.FullName == "Assigned Person");

        watchlistItem!.WatchOwnerId.Should().Be(_threatAnalystUser.Id);
    }

    [Fact]
    public async Task Create_WithAttachments_ShouldStoreAttachmentsJson()
    {
        // Arrange
        SetupUser(_threatAnalystUser);

        var attachmentsJson = "[\"doc1.pdf\",\"doc2.pdf\"]";
        var request = new CreateWatchlistRequest(
            FullName: "Person With Docs",
            RoleStatus: null,
            RiskSphereId: null,
            ThreatSource: null,
            ConflictDate: null,
            RiskLevel: RiskLevel.High,
            MonitoringFrequency: MonitoringFrequency.Weekly,
            LastCheckDate: null,
            NextCheckDate: null,
            DynamicsDescription: null,
            WatchOwnerId: null,
            AttachmentsJson: attachmentsJson
        );

        // Act
        await _controller.Create(request);

        // Assert
        var watchlistItem = await _context.Watchlists
            .FirstOrDefaultAsync(w => w.FullName == "Person With Docs");

        watchlistItem!.AttachmentsJson.Should().Be(attachmentsJson);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_WithValidData_ShouldUpdateWatchlistItem()
    {
        // Arrange
        SetupUser(_threatAnalystUser);

        var watchlistItem = new Watchlist
        {
            FullName = "Person To Update",
            RiskLevel = RiskLevel.Low,
            MonitoringFrequency = MonitoringFrequency.Monthly,
            WatchOwnerId = _threatAnalystUser.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedBy = _threatAnalystUser.Id
        };

        _context.Watchlists.Add(watchlistItem);
        await _context.SaveChangesAsync();

        var updateRequest = new UpdateWatchlistRequest(
            RoleStatus: "New Role",
            RiskSphereId: 2,
            ThreatSource: "New threat",
            ConflictDate: DateTime.UtcNow,
            RiskLevel: RiskLevel.High,
            MonitoringFrequency: MonitoringFrequency.Weekly,
            LastCheckDate: DateTime.UtcNow,
            NextCheckDate: DateTime.UtcNow.AddDays(7),
            DynamicsDescription: "Escalated",
            WatchOwnerId: _threatAnalystUser.Id,
            AttachmentsJson: null
        );

        // Act
        var result = await _controller.Update(watchlistItem.Id, updateRequest);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        var updated = await _context.Watchlists.FindAsync(watchlistItem.Id);
        updated!.RiskLevel.Should().Be(RiskLevel.High);
        updated.MonitoringFrequency.Should().Be(MonitoringFrequency.Weekly);
        updated.RoleStatus.Should().Be("New Role");
        updated.DynamicsDescription.Should().Be("Escalated");
    }

    [Fact]
    public async Task Update_WithNonExistentId_ShouldReturnNotFound()
    {
        // Arrange
        SetupUser(_adminUser);

        var updateRequest = new UpdateWatchlistRequest(
            RoleStatus: null,
            RiskSphereId: null,
            ThreatSource: null,
            ConflictDate: null,
            RiskLevel: RiskLevel.Medium,
            MonitoringFrequency: MonitoringFrequency.Weekly,
            LastCheckDate: null,
            NextCheckDate: null,
            DynamicsDescription: null,
            WatchOwnerId: null,
            AttachmentsJson: null
        );

        // Act
        var result = await _controller.Update(99999, updateRequest);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    #endregion

    #region RecordCheck Tests

    [Fact]
    public async Task RecordCheck_ShouldUpdateCheckDates()
    {
        // Arrange
        SetupUser(_threatAnalystUser);

        var watchlistItem = new Watchlist
        {
            FullName = "Person To Check",
            RiskLevel = RiskLevel.High,
            MonitoringFrequency = MonitoringFrequency.Weekly,
            LastCheckDate = DateTime.UtcNow.AddDays(-7),
            NextCheckDate = DateTime.UtcNow.AddDays(-1),
            WatchOwnerId = _threatAnalystUser.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedBy = _threatAnalystUser.Id
        };

        _context.Watchlists.Add(watchlistItem);
        await _context.SaveChangesAsync();

        var checkRequest = new RecordCheckRequest(
            NextCheckDate: DateTime.UtcNow.AddDays(7),
            DynamicsUpdate: null,
            NewRiskLevel: null
        );

        // Act
        var result = await _controller.RecordCheck(watchlistItem.Id, checkRequest);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        var updated = await _context.Watchlists.FindAsync(watchlistItem.Id);
        updated!.LastCheckDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        updated.NextCheckDate.Should().Be(checkRequest.NextCheckDate);
    }

    [Fact]
    public async Task RecordCheck_WithDynamicsUpdate_ShouldUpdateDynamics()
    {
        // Arrange
        SetupUser(_threatAnalystUser);

        var watchlistItem = new Watchlist
        {
            FullName = "Person With Dynamics",
            RiskLevel = RiskLevel.Medium,
            MonitoringFrequency = MonitoringFrequency.Weekly,
            DynamicsDescription = "Old dynamics",
            WatchOwnerId = _threatAnalystUser.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedBy = _threatAnalystUser.Id
        };

        _context.Watchlists.Add(watchlistItem);
        await _context.SaveChangesAsync();

        var checkRequest = new RecordCheckRequest(
            NextCheckDate: DateTime.UtcNow.AddDays(7),
            DynamicsUpdate: "Risk increasing significantly",
            NewRiskLevel: RiskLevel.High
        );

        // Act
        await _controller.RecordCheck(watchlistItem.Id, checkRequest);

        // Assert
        var updated = await _context.Watchlists.FindAsync(watchlistItem.Id);
        updated!.DynamicsDescription.Should().Be("Risk increasing significantly");
        updated.RiskLevel.Should().Be(RiskLevel.High);
    }

    [Fact]
    public async Task RecordCheck_WithNonExistentId_ShouldReturnNotFound()
    {
        // Arrange
        SetupUser(_adminUser);

        var checkRequest = new RecordCheckRequest(
            NextCheckDate: DateTime.UtcNow.AddDays(7),
            DynamicsUpdate: null,
            NewRiskLevel: null
        );

        // Act
        var result = await _controller.RecordCheck(99999, checkRequest);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_AsAdmin_ShouldSoftDeleteWatchlistItem()
    {
        // Arrange
        SetupUser(_adminUser);

        var watchlistItem = new Watchlist
        {
            FullName = "Person To Delete",
            RiskLevel = RiskLevel.Low,
            MonitoringFrequency = MonitoringFrequency.Monthly,
            WatchOwnerId = _threatAnalystUser.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedBy = _threatAnalystUser.Id
        };

        _context.Watchlists.Add(watchlistItem);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.Delete(watchlistItem.Id);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        var deleted = await _context.Watchlists.FindAsync(watchlistItem.Id);
        deleted.Should().NotBeNull(); // Still exists in database
        deleted!.IsActive.Should().BeFalse(); // But marked inactive
    }

    [Fact]
    public async Task Delete_WithNonExistentId_ShouldReturnNotFound()
    {
        // Arrange
        SetupUser(_adminUser);

        // Act
        var result = await _controller.Delete(99999);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    #endregion

    #region GetRequiringCheck Tests

    [Fact]
    public async Task GetRequiringCheck_ShouldReturnOnlyOverdueItems()
    {
        // Arrange
        SetupUser(_threatAnalystUser);

        var overdueItem = new Watchlist
        {
            FullName = "Overdue Person",
            RiskLevel = RiskLevel.Critical,
            MonitoringFrequency = MonitoringFrequency.Weekly,
            NextCheckDate = DateTime.UtcNow.AddDays(-3),
            WatchOwnerId = _threatAnalystUser.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedBy = _threatAnalystUser.Id
        };

        var futureItem = new Watchlist
        {
            FullName = "Future Check Person",
            RiskLevel = RiskLevel.Medium,
            MonitoringFrequency = MonitoringFrequency.Weekly,
            NextCheckDate = DateTime.UtcNow.AddDays(5),
            WatchOwnerId = _threatAnalystUser.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedBy = _threatAnalystUser.Id
        };

        _context.Watchlists.AddRange(overdueItem, futureItem);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetRequiringCheck();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var items = okResult.Value.Should().BeAssignableTo<IEnumerable<WatchlistDto>>().Subject;

        items.Should().HaveCount(1);
        items.First().FullName.Should().Be("Overdue Person");
        items.First().RequiresCheck.Should().BeTrue();
    }

    [Fact]
    public async Task GetRequiringCheck_ShouldOrderByDateAndRiskLevel()
    {
        // Arrange
        SetupUser(_adminUser);

        var item1 = new Watchlist
        {
            FullName = "Most Overdue",
            RiskLevel = RiskLevel.Medium,
            MonitoringFrequency = MonitoringFrequency.Weekly,
            NextCheckDate = DateTime.UtcNow.AddDays(-10),
            WatchOwnerId = _adminUser.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedBy = _adminUser.Id
        };

        var item2 = new Watchlist
        {
            FullName = "Recent Overdue Critical",
            RiskLevel = RiskLevel.Critical,
            MonitoringFrequency = MonitoringFrequency.Weekly,
            NextCheckDate = DateTime.UtcNow.AddDays(-1),
            WatchOwnerId = _adminUser.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedBy = _adminUser.Id
        };

        _context.Watchlists.AddRange(item1, item2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetRequiringCheck();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var items = okResult.Value.Should().BeAssignableTo<IEnumerable<WatchlistDto>>().Subject.ToList();

        items.Should().HaveCount(2);
        items.First().FullName.Should().Be("Most Overdue"); // Ordered by NextCheckDate first
    }

    #endregion

    #region GetStatistics Tests

    [Fact]
    public async Task GetStatistics_ShouldReturnCorrectCounts()
    {
        // Arrange
        SetupUser(_adminUser);

        var items = new[]
        {
            new Watchlist
            {
                FullName = "Person 1",
                RiskLevel = RiskLevel.Critical,
                MonitoringFrequency = MonitoringFrequency.Weekly,
                NextCheckDate = DateTime.UtcNow.AddDays(-1),
                WatchOwnerId = _adminUser.Id,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedBy = _adminUser.Id
            },
            new Watchlist
            {
                FullName = "Person 2",
                RiskLevel = RiskLevel.High,
                MonitoringFrequency = MonitoringFrequency.Weekly,
                NextCheckDate = DateTime.UtcNow.AddDays(5),
                WatchOwnerId = _adminUser.Id,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedBy = _adminUser.Id
            },
            new Watchlist
            {
                FullName = "Person 3",
                RiskLevel = RiskLevel.Critical,
                MonitoringFrequency = MonitoringFrequency.Weekly,
                NextCheckDate = DateTime.UtcNow.AddDays(1),
                WatchOwnerId = _adminUser.Id,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedBy = _adminUser.Id
            }
        };

        _context.Watchlists.AddRange(items);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetStatistics();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value;

        var total = (int)response!.GetType().GetProperty("total")!.GetValue(response)!;
        var requiresCheck = (int)response.GetType().GetProperty("requiresCheck")!.GetValue(response)!;

        total.Should().Be(3);
        requiresCheck.Should().Be(1); // Only one item is overdue
    }

    [Fact]
    public async Task GetStatistics_ShouldGroupByRiskLevel()
    {
        // Arrange
        SetupUser(_adminUser);

        for (int i = 0; i < 5; i++)
        {
            _context.Watchlists.Add(new Watchlist
            {
                FullName = $"Person {i}",
                RiskLevel = i % 2 == 0 ? RiskLevel.High : RiskLevel.Medium,
                MonitoringFrequency = MonitoringFrequency.Weekly,
                WatchOwnerId = _adminUser.Id,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedBy = _adminUser.Id
            });
        }
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetStatistics();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value;

        var byRiskLevel = response!.GetType().GetProperty("byRiskLevel")!.GetValue(response) as Dictionary<string, int>;
        byRiskLevel.Should().NotBeNull();
        byRiskLevel!.Should().ContainKey("High");
        byRiskLevel.Should().ContainKey("Medium");
    }

    [Fact]
    public async Task GetStatistics_ShouldGroupByMonitoringFrequency()
    {
        // Arrange
        SetupUser(_threatAnalystUser);

        var items = new[]
        {
            new Watchlist
            {
                FullName = "Weekly 1",
                RiskLevel = RiskLevel.Critical,
                MonitoringFrequency = MonitoringFrequency.Weekly,
                WatchOwnerId = _threatAnalystUser.Id,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedBy = _threatAnalystUser.Id
            },
            new Watchlist
            {
                FullName = "Weekly 2",
                RiskLevel = RiskLevel.High,
                MonitoringFrequency = MonitoringFrequency.Weekly,
                WatchOwnerId = _threatAnalystUser.Id,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedBy = _threatAnalystUser.Id
            },
            new Watchlist
            {
                FullName = "Monthly 1",
                RiskLevel = RiskLevel.Medium,
                MonitoringFrequency = MonitoringFrequency.Monthly,
                WatchOwnerId = _threatAnalystUser.Id,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedBy = _threatAnalystUser.Id
            }
        };

        _context.Watchlists.AddRange(items);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetStatistics();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value;

        var byFrequency = response!.GetType().GetProperty("byMonitoringFrequency")!.GetValue(response) as Dictionary<string, int>;
        byFrequency.Should().NotBeNull();
        byFrequency!["Weekly"].Should().Be(2);
        byFrequency["Monthly"].Should().Be(1);
    }

    #endregion

    #region GetHistory Tests

    [Fact]
    public async Task GetHistory_WithValidId_ShouldReturnHistoryRecords()
    {
        // Arrange
        SetupUser(_adminUser);

        var watchlistItem = new Watchlist
        {
            FullName = "Person With History",
            RiskLevel = RiskLevel.Low,
            MonitoringFrequency = MonitoringFrequency.Monthly,
            WatchOwnerId = _adminUser.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedBy = _adminUser.Id
        };

        _context.Watchlists.Add(watchlistItem);
        await _context.SaveChangesAsync();

        // Add history records
        var history1 = new WatchlistHistory
        {
            WatchlistId = watchlistItem.Id,
            OldRiskLevel = RiskLevel.Low,
            NewRiskLevel = RiskLevel.Medium,
            ChangedBy = _adminUser.Id,
            ChangedAt = DateTime.UtcNow.AddDays(-2),
            Comment = "First change"
        };

        var history2 = new WatchlistHistory
        {
            WatchlistId = watchlistItem.Id,
            OldRiskLevel = RiskLevel.Medium,
            NewRiskLevel = RiskLevel.High,
            ChangedBy = _adminUser.Id,
            ChangedAt = DateTime.UtcNow.AddDays(-1),
            Comment = "Second change"
        };

        _context.Set<WatchlistHistory>().AddRange(history1, history2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetHistory(watchlistItem.Id);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var historyItems = okResult.Value.Should().BeAssignableTo<IEnumerable<WatchlistHistoryDto>>().Subject.ToList();

        historyItems.Should().HaveCount(2);
        historyItems.First().Comment.Should().Be("Second change"); // Ordered by ChangedAt desc
        historyItems.Last().Comment.Should().Be("First change");
    }

    [Fact]
    public async Task GetHistory_WithNonExistentId_ShouldReturnNotFound()
    {
        // Arrange
        SetupUser(_adminUser);

        // Act
        var result = await _controller.GetHistory(99999);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetHistory_ShouldIncludeUserLogin()
    {
        // Arrange
        SetupUser(_adminUser);

        var watchlistItem = new Watchlist
        {
            FullName = "Person With User History",
            RiskLevel = RiskLevel.Low,
            MonitoringFrequency = MonitoringFrequency.Monthly,
            WatchOwnerId = _adminUser.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedBy = _adminUser.Id
        };

        _context.Watchlists.Add(watchlistItem);
        await _context.SaveChangesAsync();

        var history = new WatchlistHistory
        {
            WatchlistId = watchlistItem.Id,
            OldRiskLevel = RiskLevel.Low,
            NewRiskLevel = RiskLevel.Critical,
            ChangedBy = _adminUser.Id,
            ChangedAt = DateTime.UtcNow,
            Comment = "Escalated to critical"
        };

        _context.Set<WatchlistHistory>().Add(history);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetHistory(watchlistItem.Id);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var historyItems = okResult.Value.Should().BeAssignableTo<IEnumerable<WatchlistHistoryDto>>().Subject.ToList();

        historyItems.Should().HaveCount(1);
        historyItems.First().ChangedByLogin.Should().Be(_adminUser.Login);
    }

    #endregion

    #region RiskLevel Change Tracking Tests

    [Fact]
    public async Task Update_WithRiskLevelChange_ShouldCreateHistoryRecord()
    {
        // Arrange
        SetupUser(_threatAnalystUser);

        var watchlistItem = new Watchlist
        {
            FullName = "Person For Risk Change",
            RiskLevel = RiskLevel.Low,
            MonitoringFrequency = MonitoringFrequency.Monthly,
            WatchOwnerId = _threatAnalystUser.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedBy = _threatAnalystUser.Id
        };

        _context.Watchlists.Add(watchlistItem);
        await _context.SaveChangesAsync();

        var updateRequest = new UpdateWatchlistRequest(
            RoleStatus: null,
            RiskSphereId: null,
            ThreatSource: null,
            ConflictDate: null,
            RiskLevel: RiskLevel.Critical, // Changed from Low to Critical
            MonitoringFrequency: MonitoringFrequency.Weekly,
            LastCheckDate: null,
            NextCheckDate: null,
            DynamicsDescription: null,
            WatchOwnerId: _threatAnalystUser.Id,
            AttachmentsJson: null
        );

        // Act
        await _controller.Update(watchlistItem.Id, updateRequest);

        // Assert
        var history = await _context.Set<WatchlistHistory>()
            .Where(h => h.WatchlistId == watchlistItem.Id)
            .ToListAsync();

        history.Should().HaveCount(1);
        history.First().OldRiskLevel.Should().Be(RiskLevel.Low);
        history.First().NewRiskLevel.Should().Be(RiskLevel.Critical);
        history.First().ChangedBy.Should().Be(_threatAnalystUser.Id);
    }

    [Fact]
    public async Task Update_WithoutRiskLevelChange_ShouldNotCreateHistoryRecord()
    {
        // Arrange
        SetupUser(_threatAnalystUser);

        var watchlistItem = new Watchlist
        {
            FullName = "Person No Risk Change",
            RiskLevel = RiskLevel.Medium,
            MonitoringFrequency = MonitoringFrequency.Monthly,
            WatchOwnerId = _threatAnalystUser.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedBy = _threatAnalystUser.Id
        };

        _context.Watchlists.Add(watchlistItem);
        await _context.SaveChangesAsync();

        var updateRequest = new UpdateWatchlistRequest(
            RoleStatus: "New role",
            RiskSphereId: null,
            ThreatSource: null,
            ConflictDate: null,
            RiskLevel: RiskLevel.Medium, // Same as before
            MonitoringFrequency: MonitoringFrequency.Weekly,
            LastCheckDate: null,
            NextCheckDate: null,
            DynamicsDescription: "Updated",
            WatchOwnerId: _threatAnalystUser.Id,
            AttachmentsJson: null
        );

        // Act
        await _controller.Update(watchlistItem.Id, updateRequest);

        // Assert
        var history = await _context.Set<WatchlistHistory>()
            .Where(h => h.WatchlistId == watchlistItem.Id)
            .ToListAsync();

        history.Should().BeEmpty();
    }

    [Fact]
    public async Task RecordCheck_WithRiskLevelChange_ShouldCreateHistoryRecord()
    {
        // Arrange
        SetupUser(_threatAnalystUser);

        var watchlistItem = new Watchlist
        {
            FullName = "Person For Check Risk Change",
            RiskLevel = RiskLevel.High,
            MonitoringFrequency = MonitoringFrequency.Weekly,
            WatchOwnerId = _threatAnalystUser.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedBy = _threatAnalystUser.Id
        };

        _context.Watchlists.Add(watchlistItem);
        await _context.SaveChangesAsync();

        var checkRequest = new RecordCheckRequest(
            NextCheckDate: DateTime.UtcNow.AddDays(7),
            DynamicsUpdate: "Situation improved",
            NewRiskLevel: RiskLevel.Low // Changed from High to Low
        );

        // Act
        await _controller.RecordCheck(watchlistItem.Id, checkRequest);

        // Assert
        var history = await _context.Set<WatchlistHistory>()
            .Where(h => h.WatchlistId == watchlistItem.Id)
            .ToListAsync();

        history.Should().HaveCount(1);
        history.First().OldRiskLevel.Should().Be(RiskLevel.High);
        history.First().NewRiskLevel.Should().Be(RiskLevel.Low);
        history.First().Comment.Should().Contain("Situation improved");
    }

    [Fact]
    public async Task RecordCheck_WithSameRiskLevel_ShouldNotCreateHistoryRecord()
    {
        // Arrange
        SetupUser(_threatAnalystUser);

        var watchlistItem = new Watchlist
        {
            FullName = "Person Same Risk Level",
            RiskLevel = RiskLevel.Medium,
            MonitoringFrequency = MonitoringFrequency.Weekly,
            WatchOwnerId = _threatAnalystUser.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedBy = _threatAnalystUser.Id
        };

        _context.Watchlists.Add(watchlistItem);
        await _context.SaveChangesAsync();

        var checkRequest = new RecordCheckRequest(
            NextCheckDate: DateTime.UtcNow.AddDays(7),
            DynamicsUpdate: "Checked, no change",
            NewRiskLevel: RiskLevel.Medium // Same as before
        );

        // Act
        await _controller.RecordCheck(watchlistItem.Id, checkRequest);

        // Assert
        var history = await _context.Set<WatchlistHistory>()
            .Where(h => h.WatchlistId == watchlistItem.Id)
            .ToListAsync();

        history.Should().BeEmpty();
    }

    #endregion

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
