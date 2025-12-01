using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Kurator.Core.Entities;
using Kurator.Core.Enums;
using Kurator.Infrastructure.Services;
using Kurator.Infrastructure.Data;

namespace Kurator.Core.Tests.Services;

public class WatchlistServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly WatchlistService _service;
    private readonly Mock<ILogger<WatchlistService>> _mockLogger;
    private readonly User _testUser;

    public WatchlistServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);

        _mockLogger = new Mock<ILogger<WatchlistService>>();
        _service = new WatchlistService(_context, _mockLogger.Object);

        // Setup test user
        _testUser = new User
        {
            Login = "threatanalyst",
            PasswordHash = "hash",
            Role = UserRole.ThreatAnalyst
        };
        _context.Users.Add(_testUser);
        _context.SaveChanges();
    }

    #region GetWatchlistItemsAsync Tests

    [Fact]
    public async Task GetWatchlistItemsAsync_WithNoFilters_ShouldReturnAllActiveItems()
    {
        // Arrange
        var activeItem1 = new Watchlist
        {
            FullName = "Active Person 1",
            RiskLevel = RiskLevel.High,
            MonitoringFrequency = MonitoringFrequency.Weekly,
            IsActive = true,
            UpdatedBy = _testUser.Id
        };
        var activeItem2 = new Watchlist
        {
            FullName = "Active Person 2",
            RiskLevel = RiskLevel.Medium,
            MonitoringFrequency = MonitoringFrequency.Monthly,
            IsActive = true,
            UpdatedBy = _testUser.Id
        };
        var inactiveItem = new Watchlist
        {
            FullName = "Inactive Person",
            RiskLevel = RiskLevel.Low,
            MonitoringFrequency = MonitoringFrequency.Quarterly,
            IsActive = false,
            UpdatedBy = _testUser.Id
        };

        _context.Watchlists.AddRange(activeItem1, activeItem2, inactiveItem);
        await _context.SaveChangesAsync();

        // Act
        var (items, total) = await _service.GetWatchlistItemsAsync();

        // Assert
        items.Should().HaveCount(2);
        total.Should().Be(2);
        items.Should().NotContain(w => w.FullName == "Inactive Person");
        items.All(w => w.IsActive).Should().BeTrue();
    }

    [Fact]
    public async Task GetWatchlistItemsAsync_WithRiskLevelFilter_ShouldReturnFilteredItems()
    {
        // Arrange
        var highRiskItem = new Watchlist
        {
            FullName = "High Risk Person",
            RiskLevel = RiskLevel.High,
            MonitoringFrequency = MonitoringFrequency.Weekly,
            IsActive = true,
            UpdatedBy = _testUser.Id
        };
        var mediumRiskItem = new Watchlist
        {
            FullName = "Medium Risk Person",
            RiskLevel = RiskLevel.Medium,
            MonitoringFrequency = MonitoringFrequency.Monthly,
            IsActive = true,
            UpdatedBy = _testUser.Id
        };

        _context.Watchlists.AddRange(highRiskItem, mediumRiskItem);
        await _context.SaveChangesAsync();

        // Act
        var (items, total) = await _service.GetWatchlistItemsAsync(riskLevel: RiskLevel.High);

        // Assert
        items.Should().HaveCount(1);
        total.Should().Be(1);
        items.First().RiskLevel.Should().Be(RiskLevel.High);
        items.First().FullName.Should().Be("High Risk Person");
    }

    [Fact]
    public async Task GetWatchlistItemsAsync_WithMonitoringFrequencyFilter_ShouldReturnFilteredItems()
    {
        // Arrange
        var weeklyItem = new Watchlist
        {
            FullName = "Weekly Check Person",
            RiskLevel = RiskLevel.High,
            MonitoringFrequency = MonitoringFrequency.Weekly,
            IsActive = true,
            UpdatedBy = _testUser.Id
        };
        var monthlyItem = new Watchlist
        {
            FullName = "Monthly Check Person",
            RiskLevel = RiskLevel.Medium,
            MonitoringFrequency = MonitoringFrequency.Monthly,
            IsActive = true,
            UpdatedBy = _testUser.Id
        };

        _context.Watchlists.AddRange(weeklyItem, monthlyItem);
        await _context.SaveChangesAsync();

        // Act
        var (items, total) = await _service.GetWatchlistItemsAsync(
            monitoringFrequency: MonitoringFrequency.Weekly);

        // Assert
        items.Should().HaveCount(1);
        total.Should().Be(1);
        items.First().MonitoringFrequency.Should().Be(MonitoringFrequency.Weekly);
    }

    [Fact]
    public async Task GetWatchlistItemsAsync_WithRequiresCheckFilter_ShouldReturnOverdueItems()
    {
        // Arrange
        var overdueItem = new Watchlist
        {
            FullName = "Overdue Person",
            RiskLevel = RiskLevel.High,
            MonitoringFrequency = MonitoringFrequency.Weekly,
            NextCheckDate = DateTime.UtcNow.AddDays(-5),
            IsActive = true,
            UpdatedBy = _testUser.Id
        };
        var futureItem = new Watchlist
        {
            FullName = "Future Check Person",
            RiskLevel = RiskLevel.Medium,
            MonitoringFrequency = MonitoringFrequency.Monthly,
            NextCheckDate = DateTime.UtcNow.AddDays(10),
            IsActive = true,
            UpdatedBy = _testUser.Id
        };

        _context.Watchlists.AddRange(overdueItem, futureItem);
        await _context.SaveChangesAsync();

        // Act
        var (items, total) = await _service.GetWatchlistItemsAsync(requiresCheck: true);

        // Assert
        items.Should().HaveCount(1);
        total.Should().Be(1);
        items.First().FullName.Should().Be("Overdue Person");
        items.First().NextCheckDate.Should().BeBefore(DateTime.UtcNow);
    }

    [Fact]
    public async Task GetWatchlistItemsAsync_WithWatchOwnerFilter_ShouldReturnOwnedItems()
    {
        // Arrange
        var owner = new User
        {
            Login = "owner1",
            PasswordHash = "hash",
            Role = UserRole.ThreatAnalyst
        };
        _context.Users.Add(owner);
        await _context.SaveChangesAsync();

        var ownedItem = new Watchlist
        {
            FullName = "Owned Person",
            RiskLevel = RiskLevel.High,
            MonitoringFrequency = MonitoringFrequency.Weekly,
            WatchOwnerId = owner.Id,
            IsActive = true,
            UpdatedBy = _testUser.Id
        };
        var unownedItem = new Watchlist
        {
            FullName = "Unowned Person",
            RiskLevel = RiskLevel.Medium,
            MonitoringFrequency = MonitoringFrequency.Monthly,
            WatchOwnerId = _testUser.Id,
            IsActive = true,
            UpdatedBy = _testUser.Id
        };

        _context.Watchlists.AddRange(ownedItem, unownedItem);
        await _context.SaveChangesAsync();

        // Act
        var (items, total) = await _service.GetWatchlistItemsAsync(watchOwnerId: owner.Id);

        // Assert
        items.Should().HaveCount(1);
        total.Should().Be(1);
        items.First().WatchOwnerId.Should().Be(owner.Id);
    }

    [Fact]
    public async Task GetWatchlistItemsAsync_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        for (int i = 1; i <= 25; i++)
        {
            _context.Watchlists.Add(new Watchlist
            {
                FullName = $"Person {i}",
                RiskLevel = RiskLevel.Medium,
                MonitoringFrequency = MonitoringFrequency.Monthly,
                IsActive = true,
                UpdatedBy = _testUser.Id
            });
        }
        await _context.SaveChangesAsync();

        // Act - Get page 2 with page size 10
        var (items, total) = await _service.GetWatchlistItemsAsync(page: 2, pageSize: 10);

        // Assert
        items.Should().HaveCount(10);
        total.Should().Be(25);
    }

    [Fact]
    public async Task GetWatchlistItemsAsync_ShouldOrderByRiskLevelThenNextCheckDate()
    {
        // Arrange
        var item1 = new Watchlist
        {
            FullName = "Person A",
            RiskLevel = RiskLevel.Medium,
            MonitoringFrequency = MonitoringFrequency.Monthly,
            NextCheckDate = DateTime.UtcNow.AddDays(5),
            IsActive = true,
            UpdatedBy = _testUser.Id
        };
        var item2 = new Watchlist
        {
            FullName = "Person B",
            RiskLevel = RiskLevel.High,
            MonitoringFrequency = MonitoringFrequency.Weekly,
            NextCheckDate = DateTime.UtcNow.AddDays(2),
            IsActive = true,
            UpdatedBy = _testUser.Id
        };
        var item3 = new Watchlist
        {
            FullName = "Person C",
            RiskLevel = RiskLevel.High,
            MonitoringFrequency = MonitoringFrequency.Weekly,
            NextCheckDate = DateTime.UtcNow.AddDays(1),
            IsActive = true,
            UpdatedBy = _testUser.Id
        };

        _context.Watchlists.AddRange(item1, item2, item3);
        await _context.SaveChangesAsync();

        // Act
        var (items, total) = await _service.GetWatchlistItemsAsync();

        // Assert
        var itemsList = items.ToList();
        itemsList.Should().HaveCount(3);
        // High risk items should come first
        itemsList[0].FullName.Should().Be("Person C"); // High risk, earliest next check
        itemsList[1].FullName.Should().Be("Person B"); // High risk, later next check
        itemsList[2].FullName.Should().Be("Person A"); // Medium risk
    }

    #endregion

    #region GetWatchlistItemByIdAsync Tests

    [Fact]
    public async Task GetWatchlistItemByIdAsync_WithValidId_ShouldReturnItem()
    {
        // Arrange
        var item = new Watchlist
        {
            FullName = "Test Person",
            RiskLevel = RiskLevel.High,
            MonitoringFrequency = MonitoringFrequency.Weekly,
            IsActive = true,
            UpdatedBy = _testUser.Id
        };
        _context.Watchlists.Add(item);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetWatchlistItemByIdAsync(item.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(item.Id);
        result.FullName.Should().Be("Test Person");
    }

    [Fact]
    public async Task GetWatchlistItemByIdAsync_WithInvalidId_ShouldReturnNull()
    {
        // Act
        var result = await _service.GetWatchlistItemByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetWatchlistItemByIdAsync_ShouldIncludeWatchOwner()
    {
        // Arrange
        var owner = new User
        {
            Login = "owner1",
            PasswordHash = "hash",
            Role = UserRole.ThreatAnalyst
        };
        _context.Users.Add(owner);
        await _context.SaveChangesAsync();

        var item = new Watchlist
        {
            FullName = "Test Person",
            RiskLevel = RiskLevel.High,
            MonitoringFrequency = MonitoringFrequency.Weekly,
            WatchOwnerId = owner.Id,
            IsActive = true,
            UpdatedBy = _testUser.Id
        };
        _context.Watchlists.Add(item);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetWatchlistItemByIdAsync(item.Id);

        // Assert
        result.Should().NotBeNull();
        result!.WatchOwner.Should().NotBeNull();
        result.WatchOwner!.Login.Should().Be("owner1");
    }

    #endregion

    #region CreateWatchlistItemAsync Tests

    [Fact]
    public async Task CreateWatchlistItemAsync_WithValidData_ShouldCreateItem()
    {
        // Act
        var result = await _service.CreateWatchlistItemAsync(
            fullName: "New Threat Person",
            userId: _testUser.Id,
            roleStatus: "Former Partner",
            threatSource: "Media Investigation",
            riskLevel: RiskLevel.High,
            monitoringFrequency: MonitoringFrequency.Weekly);

        // Assert
        result.Should().NotBeNull();
        result.FullName.Should().Be("New Threat Person");
        result.RoleStatus.Should().Be("Former Partner");
        result.ThreatSource.Should().Be("Media Investigation");
        result.RiskLevel.Should().Be(RiskLevel.High);
        result.MonitoringFrequency.Should().Be(MonitoringFrequency.Weekly);
        result.IsActive.Should().BeTrue();
        result.UpdatedBy.Should().Be(_testUser.Id);

        // Verify in database
        var dbItem = await _context.Watchlists.FindAsync(result.Id);
        dbItem.Should().NotBeNull();
        dbItem!.FullName.Should().Be("New Threat Person");
    }

    [Fact]
    public async Task CreateWatchlistItemAsync_WithEmptyFullName_ShouldThrowException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _service.CreateWatchlistItemAsync(
                fullName: "",
                userId: _testUser.Id));
    }

    [Fact]
    public async Task CreateWatchlistItemAsync_WithAllOptionalFields_ShouldCreateItem()
    {
        // Arrange
        var conflictDate = DateTime.UtcNow.AddMonths(-3);
        var nextCheckDate = DateTime.UtcNow.AddDays(7);

        // Act
        var result = await _service.CreateWatchlistItemAsync(
            fullName: "Complete Entry Person",
            userId: _testUser.Id,
            roleStatus: "Journalist",
            riskSphereId: 1,
            threatSource: "Investigation",
            conflictDate: conflictDate,
            riskLevel: RiskLevel.Critical,
            monitoringFrequency: MonitoringFrequency.Weekly,
            lastCheckDate: DateTime.UtcNow,
            nextCheckDate: nextCheckDate,
            dynamicsDescription: "Situation escalating",
            watchOwnerId: _testUser.Id,
            attachmentsJson: "[\"file1.pdf\", \"file2.docx\"]");

        // Assert
        result.Should().NotBeNull();
        result.RiskSphereId.Should().Be(1);
        result.ConflictDate.Should().Be(conflictDate);
        result.NextCheckDate.Should().Be(nextCheckDate);
        result.DynamicsDescription.Should().Be("Situation escalating");
        result.AttachmentsJson.Should().Contain("file1.pdf");
    }

    [Fact]
    public async Task CreateWatchlistItemAsync_WithoutWatchOwner_ShouldDefaultToCreatingUser()
    {
        // Act
        var result = await _service.CreateWatchlistItemAsync(
            fullName: "Test Person",
            userId: _testUser.Id,
            riskLevel: RiskLevel.Medium,
            monitoringFrequency: MonitoringFrequency.Monthly);

        // Assert
        result.WatchOwnerId.Should().Be(_testUser.Id);
    }

    [Fact]
    public async Task CreateWatchlistItemAsync_ShouldCreateAuditLog()
    {
        // Act
        var result = await _service.CreateWatchlistItemAsync(
            fullName: "Audit Test Person",
            userId: _testUser.Id,
            riskLevel: RiskLevel.High,
            monitoringFrequency: MonitoringFrequency.Weekly);

        // Assert
        var auditLog = await _context.AuditLogs
            .FirstOrDefaultAsync(a => a.EntityType == "Watchlist" && a.EntityId == result.Id.ToString());

        auditLog.Should().NotBeNull();
        auditLog!.Action.Should().Be(AuditActionType.Create);
        auditLog.UserId.Should().Be(_testUser.Id);
        auditLog.NewValuesJson.Should().Contain("Audit Test Person");
    }

    #endregion

    #region UpdateWatchlistItemAsync Tests

    [Fact]
    public async Task UpdateWatchlistItemAsync_WithValidData_ShouldUpdateItem()
    {
        // Arrange
        var item = new Watchlist
        {
            FullName = "Original Name",
            RoleStatus = "Original Role",
            RiskLevel = RiskLevel.Low,
            MonitoringFrequency = MonitoringFrequency.Quarterly,
            IsActive = true,
            UpdatedBy = _testUser.Id
        };
        _context.Watchlists.Add(item);
        await _context.SaveChangesAsync();

        // Act
        await _service.UpdateWatchlistItemAsync(
            id: item.Id,
            userId: _testUser.Id,
            roleStatus: "Updated Role",
            riskLevel: RiskLevel.High,
            monitoringFrequency: MonitoringFrequency.Weekly);

        // Assert
        var updated = await _context.Watchlists.FindAsync(item.Id);
        updated.Should().NotBeNull();
        updated!.RoleStatus.Should().Be("Updated Role");
        updated.RiskLevel.Should().Be(RiskLevel.High);
        updated.MonitoringFrequency.Should().Be(MonitoringFrequency.Weekly);
        updated.UpdatedBy.Should().Be(_testUser.Id);
        updated.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task UpdateWatchlistItemAsync_WithNonExistentId_ShouldThrowException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _service.UpdateWatchlistItemAsync(
                id: 999,
                userId: _testUser.Id,
                roleStatus: "Updated Role"));
    }

    [Fact]
    public async Task UpdateWatchlistItemAsync_ShouldCreateAuditLog()
    {
        // Arrange
        var item = new Watchlist
        {
            FullName = "Test Person",
            RiskLevel = RiskLevel.Low,
            MonitoringFrequency = MonitoringFrequency.Monthly,
            IsActive = true,
            UpdatedBy = _testUser.Id
        };
        _context.Watchlists.Add(item);
        await _context.SaveChangesAsync();

        // Act
        await _service.UpdateWatchlistItemAsync(
            id: item.Id,
            userId: _testUser.Id,
            riskLevel: RiskLevel.High);

        // Assert
        var auditLog = await _context.AuditLogs
            .Where(a => a.EntityType == "Watchlist" && a.EntityId == item.Id.ToString() && a.Action == AuditActionType.Update)
            .FirstOrDefaultAsync();

        auditLog.Should().NotBeNull();
        auditLog!.OldValuesJson.Should().Contain("Low");
        auditLog.NewValuesJson.Should().Contain("High");
    }

    #endregion

    #region DeleteWatchlistItemAsync Tests

    [Fact]
    public async Task DeleteWatchlistItemAsync_WithValidId_ShouldSoftDelete()
    {
        // Arrange
        var item = new Watchlist
        {
            FullName = "To Delete Person",
            RiskLevel = RiskLevel.Medium,
            MonitoringFrequency = MonitoringFrequency.Monthly,
            IsActive = true,
            UpdatedBy = _testUser.Id
        };
        _context.Watchlists.Add(item);
        await _context.SaveChangesAsync();

        // Act
        await _service.DeleteWatchlistItemAsync(item.Id, _testUser.Id);

        // Assert
        var deleted = await _context.Watchlists.FindAsync(item.Id);
        deleted.Should().NotBeNull();
        deleted!.IsActive.Should().BeFalse();
        deleted.UpdatedBy.Should().Be(_testUser.Id);
        deleted.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task DeleteWatchlistItemAsync_WithNonExistentId_ShouldThrowException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _service.DeleteWatchlistItemAsync(999, _testUser.Id));
    }

    [Fact]
    public async Task DeleteWatchlistItemAsync_ShouldCreateAuditLog()
    {
        // Arrange
        var item = new Watchlist
        {
            FullName = "Delete Audit Person",
            RiskLevel = RiskLevel.Medium,
            MonitoringFrequency = MonitoringFrequency.Monthly,
            IsActive = true,
            UpdatedBy = _testUser.Id
        };
        _context.Watchlists.Add(item);
        await _context.SaveChangesAsync();

        // Act
        await _service.DeleteWatchlistItemAsync(item.Id, _testUser.Id);

        // Assert
        var auditLog = await _context.AuditLogs
            .Where(a => a.EntityType == "Watchlist" && a.EntityId == item.Id.ToString() && a.Action == AuditActionType.Delete)
            .FirstOrDefaultAsync();

        auditLog.Should().NotBeNull();
        auditLog!.OldValuesJson.Should().Contain("true");
        auditLog.NewValuesJson.Should().Contain("false");
    }

    #endregion

    #region RecordCheckAsync Tests

    [Fact]
    public async Task RecordCheckAsync_WithValidData_ShouldUpdateCheckDates()
    {
        // Arrange
        var item = new Watchlist
        {
            FullName = "Check Person",
            RiskLevel = RiskLevel.High,
            MonitoringFrequency = MonitoringFrequency.Weekly,
            LastCheckDate = DateTime.UtcNow.AddDays(-7),
            IsActive = true,
            UpdatedBy = _testUser.Id
        };
        _context.Watchlists.Add(item);
        await _context.SaveChangesAsync();

        var nextCheckDate = DateTime.UtcNow.AddDays(7);

        // Act
        await _service.RecordCheckAsync(
            id: item.Id,
            userId: _testUser.Id,
            nextCheckDate: nextCheckDate);

        // Assert
        var updated = await _context.Watchlists.FindAsync(item.Id);
        updated.Should().NotBeNull();
        updated!.LastCheckDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        updated.NextCheckDate.Should().Be(nextCheckDate);
    }

    [Fact]
    public async Task RecordCheckAsync_WithDynamicsUpdate_ShouldUpdateDynamics()
    {
        // Arrange
        var item = new Watchlist
        {
            FullName = "Check Person",
            RiskLevel = RiskLevel.High,
            MonitoringFrequency = MonitoringFrequency.Weekly,
            DynamicsDescription = "Old dynamics",
            IsActive = true,
            UpdatedBy = _testUser.Id
        };
        _context.Watchlists.Add(item);
        await _context.SaveChangesAsync();

        // Act
        await _service.RecordCheckAsync(
            id: item.Id,
            userId: _testUser.Id,
            dynamicsUpdate: "Situation improved");

        // Assert
        var updated = await _context.Watchlists.FindAsync(item.Id);
        updated.Should().NotBeNull();
        updated!.DynamicsDescription.Should().Be("Situation improved");
    }

    [Fact]
    public async Task RecordCheckAsync_WithNewRiskLevel_ShouldUpdateRiskLevel()
    {
        // Arrange
        var item = new Watchlist
        {
            FullName = "Check Person",
            RiskLevel = RiskLevel.Low,
            MonitoringFrequency = MonitoringFrequency.Monthly,
            IsActive = true,
            UpdatedBy = _testUser.Id
        };
        _context.Watchlists.Add(item);
        await _context.SaveChangesAsync();

        // Act
        await _service.RecordCheckAsync(
            id: item.Id,
            userId: _testUser.Id,
            newRiskLevel: RiskLevel.High);

        // Assert
        var updated = await _context.Watchlists.FindAsync(item.Id);
        updated.Should().NotBeNull();
        updated!.RiskLevel.Should().Be(RiskLevel.High);
    }

    [Fact]
    public async Task RecordCheckAsync_WithNonExistentId_ShouldThrowException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _service.RecordCheckAsync(999, _testUser.Id));
    }

    [Fact]
    public async Task RecordCheckAsync_ShouldCreateAuditLog()
    {
        // Arrange
        var item = new Watchlist
        {
            FullName = "Check Audit Person",
            RiskLevel = RiskLevel.Medium,
            MonitoringFrequency = MonitoringFrequency.Weekly,
            IsActive = true,
            UpdatedBy = _testUser.Id
        };
        _context.Watchlists.Add(item);
        await _context.SaveChangesAsync();

        // Act
        await _service.RecordCheckAsync(
            id: item.Id,
            userId: _testUser.Id,
            nextCheckDate: DateTime.UtcNow.AddDays(7));

        // Assert
        var auditLog = await _context.AuditLogs
            .Where(a => a.EntityType == "Watchlist" && a.EntityId == item.Id.ToString() && a.Action == AuditActionType.Update)
            .FirstOrDefaultAsync();

        auditLog.Should().NotBeNull();
        auditLog!.UserId.Should().Be(_testUser.Id);
    }

    #endregion

    #region GetItemsRequiringCheckAsync Tests

    [Fact]
    public async Task GetItemsRequiringCheckAsync_ShouldReturnOnlyOverdueItems()
    {
        // Arrange
        var overdueItem1 = new Watchlist
        {
            FullName = "Overdue 1",
            RiskLevel = RiskLevel.High,
            MonitoringFrequency = MonitoringFrequency.Weekly,
            NextCheckDate = DateTime.UtcNow.AddDays(-5),
            IsActive = true,
            UpdatedBy = _testUser.Id
        };
        var overdueItem2 = new Watchlist
        {
            FullName = "Overdue 2",
            RiskLevel = RiskLevel.Critical,
            MonitoringFrequency = MonitoringFrequency.Weekly,
            NextCheckDate = DateTime.UtcNow.AddDays(-1),
            IsActive = true,
            UpdatedBy = _testUser.Id
        };
        var futureItem = new Watchlist
        {
            FullName = "Future",
            RiskLevel = RiskLevel.Medium,
            MonitoringFrequency = MonitoringFrequency.Monthly,
            NextCheckDate = DateTime.UtcNow.AddDays(10),
            IsActive = true,
            UpdatedBy = _testUser.Id
        };
        var noCheckDateItem = new Watchlist
        {
            FullName = "No Check Date",
            RiskLevel = RiskLevel.Low,
            MonitoringFrequency = MonitoringFrequency.Quarterly,
            NextCheckDate = null,
            IsActive = true,
            UpdatedBy = _testUser.Id
        };

        _context.Watchlists.AddRange(overdueItem1, overdueItem2, futureItem, noCheckDateItem);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetItemsRequiringCheckAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(w => w.FullName == "Overdue 1");
        result.Should().Contain(w => w.FullName == "Overdue 2");
        result.Should().NotContain(w => w.FullName == "Future");
        result.Should().NotContain(w => w.FullName == "No Check Date");
    }

    [Fact]
    public async Task GetItemsRequiringCheckAsync_ShouldOrderByNextCheckDateThenRiskLevel()
    {
        // Arrange
        var baseTime = DateTime.UtcNow.AddDays(-10);
        var item1 = new Watchlist
        {
            FullName = "Item 1",
            RiskLevel = RiskLevel.Medium,
            MonitoringFrequency = MonitoringFrequency.Monthly,
            NextCheckDate = baseTime, // Oldest
            IsActive = true,
            UpdatedBy = _testUser.Id
        };
        var item2 = new Watchlist
        {
            FullName = "Item 2",
            RiskLevel = RiskLevel.High,
            MonitoringFrequency = MonitoringFrequency.Weekly,
            NextCheckDate = baseTime.AddDays(3), // -7 days from now
            IsActive = true,
            UpdatedBy = _testUser.Id
        };
        var item3 = new Watchlist
        {
            FullName = "Item 3",
            RiskLevel = RiskLevel.Critical,
            MonitoringFrequency = MonitoringFrequency.Weekly,
            NextCheckDate = baseTime.AddDays(5), // -5 days from now (most recent)
            IsActive = true,
            UpdatedBy = _testUser.Id
        };

        _context.Watchlists.AddRange(item1, item2, item3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetItemsRequiringCheckAsync();

        // Assert - ordered by NextCheckDate ascending (oldest first)
        var resultList = result.ToList();
        resultList.Should().HaveCount(3);
        resultList[0].FullName.Should().Be("Item 1"); // Oldest check date (-10 days)
        resultList[1].FullName.Should().Be("Item 2"); // Second oldest (-7 days)
        resultList[2].FullName.Should().Be("Item 3"); // Most recent (-5 days)
    }

    [Fact]
    public async Task GetItemsRequiringCheckAsync_ShouldExcludeInactiveItems()
    {
        // Arrange
        var activeOverdue = new Watchlist
        {
            FullName = "Active Overdue",
            RiskLevel = RiskLevel.High,
            MonitoringFrequency = MonitoringFrequency.Weekly,
            NextCheckDate = DateTime.UtcNow.AddDays(-5),
            IsActive = true,
            UpdatedBy = _testUser.Id
        };
        var inactiveOverdue = new Watchlist
        {
            FullName = "Inactive Overdue",
            RiskLevel = RiskLevel.High,
            MonitoringFrequency = MonitoringFrequency.Weekly,
            NextCheckDate = DateTime.UtcNow.AddDays(-5),
            IsActive = false,
            UpdatedBy = _testUser.Id
        };

        _context.Watchlists.AddRange(activeOverdue, inactiveOverdue);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetItemsRequiringCheckAsync();

        // Assert
        result.Should().HaveCount(1);
        result.First().FullName.Should().Be("Active Overdue");
    }

    #endregion

    #region GetStatisticsAsync Tests

    [Fact]
    public async Task GetStatisticsAsync_ShouldReturnCorrectCounts()
    {
        // Arrange
        var items = new[]
        {
            new Watchlist
            {
                FullName = "Person 1",
                RiskLevel = RiskLevel.High,
                MonitoringFrequency = MonitoringFrequency.Weekly,
                IsActive = true,
                UpdatedBy = _testUser.Id
            },
            new Watchlist
            {
                FullName = "Person 2",
                RiskLevel = RiskLevel.High,
                MonitoringFrequency = MonitoringFrequency.Weekly,
                IsActive = true,
                UpdatedBy = _testUser.Id
            },
            new Watchlist
            {
                FullName = "Person 3",
                RiskLevel = RiskLevel.Medium,
                MonitoringFrequency = MonitoringFrequency.Monthly,
                IsActive = true,
                UpdatedBy = _testUser.Id
            }
        };
        _context.Watchlists.AddRange(items);
        await _context.SaveChangesAsync();

        // Act
        var stats = await _service.GetStatisticsAsync();

        // Assert
        stats.Total.Should().Be(3);
        stats.ByRiskLevel.Should().HaveCount(2);
        stats.ByRiskLevel["High"].Should().Be(2);
        stats.ByRiskLevel["Medium"].Should().Be(1);
    }

    [Fact]
    public async Task GetStatisticsAsync_ShouldCountItemsRequiringCheck()
    {
        // Arrange
        var overdueItem = new Watchlist
        {
            FullName = "Overdue",
            RiskLevel = RiskLevel.High,
            MonitoringFrequency = MonitoringFrequency.Weekly,
            NextCheckDate = DateTime.UtcNow.AddDays(-5),
            IsActive = true,
            UpdatedBy = _testUser.Id
        };
        var futureItem = new Watchlist
        {
            FullName = "Future",
            RiskLevel = RiskLevel.Medium,
            MonitoringFrequency = MonitoringFrequency.Monthly,
            NextCheckDate = DateTime.UtcNow.AddDays(10),
            IsActive = true,
            UpdatedBy = _testUser.Id
        };

        _context.Watchlists.AddRange(overdueItem, futureItem);
        await _context.SaveChangesAsync();

        // Act
        var stats = await _service.GetStatisticsAsync();

        // Assert
        stats.RequiresCheck.Should().Be(1);
    }

    [Fact]
    public async Task GetStatisticsAsync_ShouldGroupByMonitoringFrequency()
    {
        // Arrange
        var items = new[]
        {
            new Watchlist
            {
                FullName = "Weekly 1",
                RiskLevel = RiskLevel.Critical,
                MonitoringFrequency = MonitoringFrequency.Weekly,
                IsActive = true,
                UpdatedBy = _testUser.Id
            },
            new Watchlist
            {
                FullName = "Weekly 2",
                RiskLevel = RiskLevel.High,
                MonitoringFrequency = MonitoringFrequency.Weekly,
                IsActive = true,
                UpdatedBy = _testUser.Id
            },
            new Watchlist
            {
                FullName = "Weekly 3",
                RiskLevel = RiskLevel.High,
                MonitoringFrequency = MonitoringFrequency.Weekly,
                IsActive = true,
                UpdatedBy = _testUser.Id
            }
        };
        _context.Watchlists.AddRange(items);
        await _context.SaveChangesAsync();

        // Act
        var stats = await _service.GetStatisticsAsync();

        // Assert
        stats.ByMonitoringFrequency.Should().HaveCount(1);
        stats.ByMonitoringFrequency["Weekly"].Should().Be(3);
    }

    [Fact]
    public async Task GetStatisticsAsync_ShouldExcludeInactiveItems()
    {
        // Arrange
        var activeItem = new Watchlist
        {
            FullName = "Active",
            RiskLevel = RiskLevel.High,
            MonitoringFrequency = MonitoringFrequency.Weekly,
            IsActive = true,
            UpdatedBy = _testUser.Id
        };
        var inactiveItem = new Watchlist
        {
            FullName = "Inactive",
            RiskLevel = RiskLevel.High,
            MonitoringFrequency = MonitoringFrequency.Weekly,
            IsActive = false,
            UpdatedBy = _testUser.Id
        };

        _context.Watchlists.AddRange(activeItem, inactiveItem);
        await _context.SaveChangesAsync();

        // Act
        var stats = await _service.GetStatisticsAsync();

        // Assert
        stats.Total.Should().Be(1);
    }

    [Fact]
    public async Task GetStatisticsAsync_WithEmptyDatabase_ShouldReturnZeros()
    {
        // Act
        var stats = await _service.GetStatisticsAsync();

        // Assert
        stats.Total.Should().Be(0);
        stats.RequiresCheck.Should().Be(0);
        stats.ByRiskLevel.Should().BeEmpty();
        stats.ByRiskSphere.Should().BeEmpty();
        stats.ByMonitoringFrequency.Should().BeEmpty();
    }

    #endregion

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
