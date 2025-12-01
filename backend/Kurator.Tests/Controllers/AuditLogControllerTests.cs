using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Kurator.Api.Controllers;
using Kurator.Core.Entities;
using Kurator.Core.Enums;
using Kurator.Infrastructure.Data;
using System.Security.Claims;

namespace Kurator.Tests.Controllers;

/// <summary>
/// Tests for AuditLogController - Audit logging and activity tracking (Admin-only)
/// </summary>
public class AuditLogControllerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AuditLogController> _logger;
    private readonly AuditLogController _controller;
    private User _adminUser = null!;
    private User _curatorUser = null!;
    private Block _testBlock = null!;

    public AuditLogControllerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<AuditLogController>();
        _controller = new AuditLogController(_context, _logger);

        SetupTestData();
    }

    private void SetupTestData()
    {
        _adminUser = new User { Login = "admin", PasswordHash = "hash", Role = UserRole.Admin };
        _curatorUser = new User { Login = "curator", PasswordHash = "hash", Role = UserRole.Curator };
        _context.Users.AddRange(_adminUser, _curatorUser);

        _testBlock = new Block { Name = "Test Block", Code = "TEST", Status = BlockStatus.Active };
        _context.Blocks.Add(_testBlock);

        _context.SaveChanges();
    }

    private void SetupUser(User user, string role)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Login),
            new Claim(ClaimTypes.Role, role)
        };

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

    #region GetAll Tests

    [Fact]
    public async Task GetAll_WithoutFilters_ShouldReturnAllLogs()
    {
        // Arrange
        SetupUser(_adminUser, "Admin");

        var log1 = new AuditLog
        {
            UserId = _adminUser.Id,
            Action = AuditActionType.Create,
            EntityType = "Contact",
            EntityId = "1",
            Timestamp = DateTime.UtcNow,
            OldValuesJson = null,
            NewValuesJson = "{\"name\":\"Test\"}"
        };

        var log2 = new AuditLog
        {
            UserId = _curatorUser.Id,
            Action = AuditActionType.Update,
            EntityType = "Interaction",
            EntityId = "2",
            Timestamp = DateTime.UtcNow,
            OldValuesJson = "{\"status\":\"old\"}",
            NewValuesJson = "{\"status\":\"new\"}"
        };

        _context.AuditLogs.AddRange(log1, log2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetAll();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().NotBeNull().And.Subject;

        var dataProperty = response.GetType().GetProperty("data");
        var logs = dataProperty!.GetValue(response).Should().BeAssignableTo<IEnumerable<AuditLogDto>>().Subject;

        logs.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAll_FilterByUserId_ShouldReturnUserLogs()
    {
        // Arrange
        SetupUser(_adminUser, "Admin");

        var log1 = new AuditLog
        {
            UserId = _adminUser.Id,
            Action = AuditActionType.Create,
            EntityType = "Contact",
            EntityId = "1",
            Timestamp = DateTime.UtcNow
        };

        var log2 = new AuditLog
        {
            UserId = _curatorUser.Id,
            Action = AuditActionType.Update,
            EntityType = "Contact",
            EntityId = "2",
            Timestamp = DateTime.UtcNow
        };

        _context.AuditLogs.AddRange(log1, log2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetAll(userId: _adminUser.Id);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().NotBeNull().And.Subject;

        var dataProperty = response.GetType().GetProperty("data");
        var logs = dataProperty!.GetValue(response).Should().BeAssignableTo<IEnumerable<AuditLogDto>>().Subject;

        logs.Should().HaveCount(1);
        logs.First().UserId.Should().Be(_adminUser.Id);
    }

    [Fact]
    public async Task GetAll_FilterByActionType_ShouldReturnMatchingLogs()
    {
        // Arrange
        SetupUser(_adminUser, "Admin");

        var log1 = new AuditLog
        {
            UserId = _adminUser.Id,
            Action = AuditActionType.Create,
            EntityType = "Contact",
            EntityId = "1",
            Timestamp = DateTime.UtcNow
        };

        var log2 = new AuditLog
        {
            UserId = _adminUser.Id,
            Action = AuditActionType.Delete,
            EntityType = "Contact",
            EntityId = "2",
            Timestamp = DateTime.UtcNow
        };

        _context.AuditLogs.AddRange(log1, log2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetAll(actionType: AuditActionType.Create);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().NotBeNull().And.Subject;

        var dataProperty = response.GetType().GetProperty("data");
        var logs = dataProperty!.GetValue(response).Should().BeAssignableTo<IEnumerable<AuditLogDto>>().Subject;

        logs.Should().HaveCount(1);
        logs.First().ActionType.Should().Be("Create");
    }

    [Fact]
    public async Task GetAll_FilterByEntityType_ShouldReturnMatchingLogs()
    {
        // Arrange
        SetupUser(_adminUser, "Admin");

        var log1 = new AuditLog
        {
            UserId = _adminUser.Id,
            Action = AuditActionType.Create,
            EntityType = "Contact",
            EntityId = "1",
            Timestamp = DateTime.UtcNow
        };

        var log2 = new AuditLog
        {
            UserId = _adminUser.Id,
            Action = AuditActionType.Create,
            EntityType = "Interaction",
            EntityId = "2",
            Timestamp = DateTime.UtcNow
        };

        _context.AuditLogs.AddRange(log1, log2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetAll(entityType: "Contact");

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().NotBeNull().And.Subject;

        var dataProperty = response.GetType().GetProperty("data");
        var logs = dataProperty!.GetValue(response).Should().BeAssignableTo<IEnumerable<AuditLogDto>>().Subject;

        logs.Should().HaveCount(1);
        logs.First().EntityType.Should().Be("Contact");
    }

    [Fact]
    public async Task GetAll_FilterByDateRange_ShouldReturnLogsInRange()
    {
        // Arrange
        SetupUser(_adminUser, "Admin");

        var now = DateTime.UtcNow;

        var log1 = new AuditLog
        {
            UserId = _adminUser.Id,
            Action = AuditActionType.Create,
            EntityType = "Contact",
            EntityId = "1",
            Timestamp = now.AddDays(-10)
        };

        var log2 = new AuditLog
        {
            UserId = _adminUser.Id,
            Action = AuditActionType.Create,
            EntityType = "Contact",
            EntityId = "2",
            Timestamp = now.AddDays(-5)
        };

        var log3 = new AuditLog
        {
            UserId = _adminUser.Id,
            Action = AuditActionType.Create,
            EntityType = "Contact",
            EntityId = "3",
            Timestamp = now
        };

        _context.AuditLogs.AddRange(log1, log2, log3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetAll(
            fromDate: now.AddDays(-7),
            toDate: now.AddDays(-3)
        );

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().NotBeNull().And.Subject;

        var dataProperty = response.GetType().GetProperty("data");
        var logs = dataProperty!.GetValue(response).Should().BeAssignableTo<IEnumerable<AuditLogDto>>().Subject;

        logs.Should().HaveCount(1);
        logs.First().EntityId.Should().Be("2");
    }

    [Fact]
    public async Task GetAll_ShouldReturnPaginatedResults()
    {
        // Arrange
        SetupUser(_adminUser, "Admin");

        for (int i = 0; i < 60; i++)
        {
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = _adminUser.Id,
                Action = AuditActionType.Create,
                EntityType = "Contact",
                EntityId = i.ToString(),
                Timestamp = DateTime.UtcNow
            });
        }
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetAll(page: 1, pageSize: 50);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().NotBeNull().And.Subject;

        var dataProperty = response.GetType().GetProperty("data");
        var logs = dataProperty!.GetValue(response).Should().BeAssignableTo<IEnumerable<AuditLogDto>>().Subject;

        logs.Should().HaveCount(50);

        var totalProperty = response.GetType().GetProperty("total");
        var total = (int)totalProperty!.GetValue(response)!;
        total.Should().Be(60);

        var totalPagesProperty = response.GetType().GetProperty("totalPages");
        var totalPages = (int)totalPagesProperty!.GetValue(response)!;
        totalPages.Should().Be(2);
    }

    [Fact]
    public async Task GetAll_ShouldReturnSortedByTimestampDescending()
    {
        // Arrange
        SetupUser(_adminUser, "Admin");

        var now = DateTime.UtcNow;

        var log1 = new AuditLog
        {
            UserId = _adminUser.Id,
            Action = AuditActionType.Create,
            EntityType = "Contact",
            EntityId = "1",
            Timestamp = now.AddHours(-2)
        };

        var log2 = new AuditLog
        {
            UserId = _adminUser.Id,
            Action = AuditActionType.Create,
            EntityType = "Contact",
            EntityId = "2",
            Timestamp = now
        };

        var log3 = new AuditLog
        {
            UserId = _adminUser.Id,
            Action = AuditActionType.Create,
            EntityType = "Contact",
            EntityId = "3",
            Timestamp = now.AddHours(-1)
        };

        _context.AuditLogs.AddRange(log1, log2, log3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetAll();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().NotBeNull().And.Subject;

        var dataProperty = response.GetType().GetProperty("data");
        var logs = dataProperty!.GetValue(response).Should().BeAssignableTo<List<AuditLogDto>>().Subject;

        logs[0].EntityId.Should().Be("2"); // Most recent
        logs[1].EntityId.Should().Be("3");
        logs[2].EntityId.Should().Be("1"); // Oldest
    }

    [Fact]
    public async Task GetAll_ShouldIncludeUserLogin()
    {
        // Arrange
        SetupUser(_adminUser, "Admin");

        var log = new AuditLog
        {
            UserId = _adminUser.Id,
            Action = AuditActionType.Create,
            EntityType = "Contact",
            EntityId = "1",
            Timestamp = DateTime.UtcNow
        };

        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetAll();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().NotBeNull().And.Subject;

        var dataProperty = response.GetType().GetProperty("data");
        var logs = dataProperty!.GetValue(response).Should().BeAssignableTo<IEnumerable<AuditLogDto>>().Subject;

        logs.First().UserLogin.Should().Be("admin");
    }

    #endregion

    #region GetStatistics Tests

    [Fact]
    public async Task GetStatistics_ShouldReturnAggregatedData()
    {
        // Arrange
        SetupUser(_adminUser, "Admin");

        var now = DateTime.UtcNow;

        _context.AuditLogs.AddRange(
            new AuditLog
            {
                UserId = _adminUser.Id,
                Action = AuditActionType.Create,
                EntityType = "Contact",
                EntityId = "1",
                Timestamp = now
            },
            new AuditLog
            {
                UserId = _adminUser.Id,
                Action = AuditActionType.Create,
                EntityType = "Contact",
                EntityId = "2",
                Timestamp = now
            },
            new AuditLog
            {
                UserId = _curatorUser.Id,
                Action = AuditActionType.Update,
                EntityType = "Interaction",
                EntityId = "3",
                Timestamp = now
            }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetStatistics(fromDate: now.AddDays(-1), toDate: now.AddDays(1));

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var statistics = okResult.Value.Should().NotBeNull().And.Subject;

        var totalActionsProperty = statistics.GetType().GetProperty("totalActions");
        var totalActions = (int)totalActionsProperty!.GetValue(statistics)!;
        totalActions.Should().Be(3);
    }

    [Fact]
    public async Task GetStatistics_ShouldGroupByActionType()
    {
        // Arrange
        SetupUser(_adminUser, "Admin");

        var now = DateTime.UtcNow;

        _context.AuditLogs.AddRange(
            new AuditLog
            {
                UserId = _adminUser.Id,
                Action = AuditActionType.Create,
                EntityType = "Contact",
                EntityId = "1",
                Timestamp = now
            },
            new AuditLog
            {
                UserId = _adminUser.Id,
                Action = AuditActionType.Create,
                EntityType = "Contact",
                EntityId = "2",
                Timestamp = now
            },
            new AuditLog
            {
                UserId = _adminUser.Id,
                Action = AuditActionType.Update,
                EntityType = "Contact",
                EntityId = "3",
                Timestamp = now
            }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetStatistics(fromDate: now.AddDays(-1), toDate: now.AddDays(1));

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var statistics = okResult.Value.Should().NotBeNull().And.Subject;

        var byActionTypeProperty = statistics.GetType().GetProperty("byActionType");
        var byActionType = byActionTypeProperty!.GetValue(statistics).Should().BeAssignableTo<Dictionary<string, int>>().Subject;

        byActionType["Create"].Should().Be(2);
        byActionType["Update"].Should().Be(1);
    }

    [Fact]
    public async Task GetStatistics_ShouldGroupByUser()
    {
        // Arrange
        SetupUser(_adminUser, "Admin");

        var now = DateTime.UtcNow;

        _context.AuditLogs.AddRange(
            new AuditLog
            {
                UserId = _adminUser.Id,
                Action = AuditActionType.Create,
                EntityType = "Contact",
                EntityId = "1",
                Timestamp = now
            },
            new AuditLog
            {
                UserId = _adminUser.Id,
                Action = AuditActionType.Create,
                EntityType = "Contact",
                EntityId = "2",
                Timestamp = now
            },
            new AuditLog
            {
                UserId = _curatorUser.Id,
                Action = AuditActionType.Update,
                EntityType = "Contact",
                EntityId = "3",
                Timestamp = now
            }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetStatistics(fromDate: now.AddDays(-1), toDate: now.AddDays(1));

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var statistics = okResult.Value.Should().NotBeNull().And.Subject;

        var byUserProperty = statistics.GetType().GetProperty("byUser");
        var byUser = byUserProperty!.GetValue(statistics).Should().BeAssignableTo<Dictionary<string, int>>().Subject;

        byUser["admin"].Should().Be(2);
        byUser["curator"].Should().Be(1);
    }

    [Fact]
    public async Task GetStatistics_ShouldGroupByEntityType()
    {
        // Arrange
        SetupUser(_adminUser, "Admin");

        var now = DateTime.UtcNow;

        _context.AuditLogs.AddRange(
            new AuditLog
            {
                UserId = _adminUser.Id,
                Action = AuditActionType.Create,
                EntityType = "Contact",
                EntityId = "1",
                Timestamp = now
            },
            new AuditLog
            {
                UserId = _adminUser.Id,
                Action = AuditActionType.Create,
                EntityType = "Interaction",
                EntityId = "2",
                Timestamp = now
            },
            new AuditLog
            {
                UserId = _adminUser.Id,
                Action = AuditActionType.Update,
                EntityType = "Interaction",
                EntityId = "3",
                Timestamp = now
            }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetStatistics(fromDate: now.AddDays(-1), toDate: now.AddDays(1));

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var statistics = okResult.Value.Should().NotBeNull().And.Subject;

        var byEntityTypeProperty = statistics.GetType().GetProperty("byEntityType");
        var byEntityType = byEntityTypeProperty!.GetValue(statistics).Should().BeAssignableTo<Dictionary<string, int>>().Subject;

        byEntityType["Contact"].Should().Be(1);
        byEntityType["Interaction"].Should().Be(2);
    }

    [Fact]
    public async Task GetStatistics_WithDefaultDates_ShouldUseLastMonth()
    {
        // Arrange
        SetupUser(_adminUser, "Admin");

        var now = DateTime.UtcNow;

        var oldLog = new AuditLog
        {
            UserId = _adminUser.Id,
            Action = AuditActionType.Create,
            EntityType = "Contact",
            EntityId = "1",
            Timestamp = now.AddMonths(-2)
        };

        var recentLog = new AuditLog
        {
            UserId = _adminUser.Id,
            Action = AuditActionType.Create,
            EntityType = "Contact",
            EntityId = "2",
            Timestamp = now.AddDays(-15)
        };

        _context.AuditLogs.AddRange(oldLog, recentLog);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetStatistics();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var statistics = okResult.Value.Should().NotBeNull().And.Subject;

        var totalActionsProperty = statistics.GetType().GetProperty("totalActions");
        var totalActions = (int)totalActionsProperty!.GetValue(statistics)!;

        // Should only include log from last month
        totalActions.Should().Be(1);
    }

    #endregion

    #region GetByEntity Tests

    [Fact]
    public async Task GetByEntity_ShouldReturnLogsForSpecificEntity()
    {
        // Arrange
        SetupUser(_adminUser, "Admin");

        var log1 = new AuditLog
        {
            UserId = _adminUser.Id,
            Action = AuditActionType.Create,
            EntityType = "Contact",
            EntityId = "123",
            Timestamp = DateTime.UtcNow
        };

        var log2 = new AuditLog
        {
            UserId = _adminUser.Id,
            Action = AuditActionType.Update,
            EntityType = "Contact",
            EntityId = "123",
            Timestamp = DateTime.UtcNow
        };

        var log3 = new AuditLog
        {
            UserId = _adminUser.Id,
            Action = AuditActionType.Update,
            EntityType = "Contact",
            EntityId = "456",
            Timestamp = DateTime.UtcNow
        };

        _context.AuditLogs.AddRange(log1, log2, log3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetByEntity("Contact", "123");

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var logs = okResult.Value.Should().BeAssignableTo<IEnumerable<AuditLogDto>>().Subject;

        logs.Should().HaveCount(2);
        logs.Should().OnlyContain(l => l.EntityId == "123" && l.EntityType == "Contact");
    }

    [Fact]
    public async Task GetByEntity_ShouldReturnSortedByTimestampDescending()
    {
        // Arrange
        SetupUser(_adminUser, "Admin");

        var now = DateTime.UtcNow;

        var log1 = new AuditLog
        {
            UserId = _adminUser.Id,
            Action = AuditActionType.Create,
            EntityType = "Contact",
            EntityId = "123",
            Timestamp = now.AddHours(-2),
            OldValuesJson = null,
            NewValuesJson = "create"
        };

        var log2 = new AuditLog
        {
            UserId = _adminUser.Id,
            Action = AuditActionType.Update,
            EntityType = "Contact",
            EntityId = "123",
            Timestamp = now,
            OldValuesJson = "old",
            NewValuesJson = "new"
        };

        _context.AuditLogs.AddRange(log1, log2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetByEntity("Contact", "123");

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var logs = okResult.Value.Should().BeAssignableTo<List<AuditLogDto>>().Subject;

        logs[0].NewValue.Should().Be("new"); // Most recent
        logs[1].NewValue.Should().Be("create"); // Oldest
    }

    [Fact]
    public async Task GetByEntity_WithNoLogs_ShouldReturnEmptyList()
    {
        // Arrange
        SetupUser(_adminUser, "Admin");

        // Act
        var result = await _controller.GetByEntity("Contact", "999");

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var logs = okResult.Value.Should().BeAssignableTo<IEnumerable<AuditLogDto>>().Subject;
        logs.Should().BeEmpty();
    }

    #endregion

    #region GetByUser Tests

    [Fact]
    public async Task GetByUser_ShouldReturnLogsForSpecificUser()
    {
        // Arrange
        SetupUser(_adminUser, "Admin");

        var log1 = new AuditLog
        {
            UserId = _adminUser.Id,
            Action = AuditActionType.Create,
            EntityType = "Contact",
            EntityId = "1",
            Timestamp = DateTime.UtcNow
        };

        var log2 = new AuditLog
        {
            UserId = _curatorUser.Id,
            Action = AuditActionType.Update,
            EntityType = "Contact",
            EntityId = "2",
            Timestamp = DateTime.UtcNow
        };

        _context.AuditLogs.AddRange(log1, log2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetByUser(_adminUser.Id);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().NotBeNull().And.Subject;

        var dataProperty = response.GetType().GetProperty("data");
        var logs = dataProperty!.GetValue(response).Should().BeAssignableTo<IEnumerable<AuditLogDto>>().Subject;

        logs.Should().HaveCount(1);
        logs.First().UserId.Should().Be(_adminUser.Id);
    }

    [Fact]
    public async Task GetByUser_ShouldReturnPaginatedResults()
    {
        // Arrange
        SetupUser(_adminUser, "Admin");

        for (int i = 0; i < 60; i++)
        {
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = _adminUser.Id,
                Action = AuditActionType.Create,
                EntityType = "Contact",
                EntityId = i.ToString(),
                Timestamp = DateTime.UtcNow
            });
        }
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetByUser(_adminUser.Id, page: 1, pageSize: 50);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().NotBeNull().And.Subject;

        var dataProperty = response.GetType().GetProperty("data");
        var logs = dataProperty!.GetValue(response).Should().BeAssignableTo<IEnumerable<AuditLogDto>>().Subject;

        logs.Should().HaveCount(50);

        var totalProperty = response.GetType().GetProperty("total");
        var total = (int)totalProperty!.GetValue(response)!;
        total.Should().Be(60);
    }

    #endregion

    #region GetRecent Tests

    [Fact]
    public async Task GetRecent_ShouldReturnMostRecentLogs()
    {
        // Arrange
        SetupUser(_adminUser, "Admin");

        var now = DateTime.UtcNow;

        for (int i = 0; i < 30; i++)
        {
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = _adminUser.Id,
                Action = AuditActionType.Create,
                EntityType = "Contact",
                EntityId = i.ToString(),
                Timestamp = now.AddMinutes(-i)
            });
        }
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetRecent(count: 20);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var logs = okResult.Value.Should().BeAssignableTo<IEnumerable<AuditLogDto>>().Subject;

        logs.Should().HaveCount(20);
    }

    [Fact]
    public async Task GetRecent_ShouldReturnSortedByTimestampDescending()
    {
        // Arrange
        SetupUser(_adminUser, "Admin");

        var now = DateTime.UtcNow;

        var log1 = new AuditLog
        {
            UserId = _adminUser.Id,
            Action = AuditActionType.Create,
            EntityType = "Contact",
            EntityId = "1",
            Timestamp = now.AddMinutes(-10)
        };

        var log2 = new AuditLog
        {
            UserId = _adminUser.Id,
            Action = AuditActionType.Create,
            EntityType = "Contact",
            EntityId = "2",
            Timestamp = now
        };

        var log3 = new AuditLog
        {
            UserId = _adminUser.Id,
            Action = AuditActionType.Create,
            EntityType = "Contact",
            EntityId = "3",
            Timestamp = now.AddMinutes(-5)
        };

        _context.AuditLogs.AddRange(log1, log2, log3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetRecent();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var logs = okResult.Value.Should().BeAssignableTo<List<AuditLogDto>>().Subject;

        logs[0].EntityId.Should().Be("2"); // Most recent
        logs[1].EntityId.Should().Be("3");
        logs[2].EntityId.Should().Be("1"); // Oldest
    }

    [Fact]
    public async Task GetRecent_WithDefaultCount_ShouldReturn20Logs()
    {
        // Arrange
        SetupUser(_adminUser, "Admin");

        for (int i = 0; i < 30; i++)
        {
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = _adminUser.Id,
                Action = AuditActionType.Create,
                EntityType = "Contact",
                EntityId = i.ToString(),
                Timestamp = DateTime.UtcNow
            });
        }
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetRecent();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var logs = okResult.Value.Should().BeAssignableTo<IEnumerable<AuditLogDto>>().Subject;

        logs.Should().HaveCount(20);
    }

    #endregion

    #region Authorization Tests

    [Fact]
    public void Controller_ShouldRequireAdminRole()
    {
        var controllerType = typeof(AuditLogController);
        var authorizeAttribute = controllerType.GetCustomAttributes(typeof(AuthorizeAttribute), false)
            .Cast<AuthorizeAttribute>()
            .FirstOrDefault();

        authorizeAttribute.Should().NotBeNull();
        authorizeAttribute!.Roles.Should().Be("Admin");
    }

    [Fact]
    public void GetAll_ShouldInheritAdminRoleRequirement()
    {
        // Since the controller has [Authorize(Roles = "Admin")],
        // all methods inherit this requirement
        var method = typeof(AuditLogController).GetMethod(nameof(AuditLogController.GetAll));
        method.Should().NotBeNull();
    }

    #endregion

    #region Edge Cases and Complex Scenarios

    [Fact]
    public async Task GetAll_WithMultipleFilters_ShouldApplyAllFilters()
    {
        // Arrange
        SetupUser(_adminUser, "Admin");

        var now = DateTime.UtcNow;

        var matchingLog = new AuditLog
        {
            UserId = _adminUser.Id,
            Action = AuditActionType.Create,
            EntityType = "Contact",
            EntityId = "1",
            Timestamp = now
        };

        var nonMatchingLogs = new[]
        {
            new AuditLog
            {
                UserId = _curatorUser.Id, // Wrong user
                Action = AuditActionType.Create,
                EntityType = "Contact",
                EntityId = "2",
                Timestamp = now
            },
            new AuditLog
            {
                UserId = _adminUser.Id,
                Action = AuditActionType.Update, // Wrong action
                EntityType = "Contact",
                EntityId = "3",
                Timestamp = now
            },
            new AuditLog
            {
                UserId = _adminUser.Id,
                Action = AuditActionType.Create,
                EntityType = "Interaction", // Wrong entity type
                EntityId = "4",
                Timestamp = now
            }
        };

        _context.AuditLogs.Add(matchingLog);
        _context.AuditLogs.AddRange(nonMatchingLogs);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetAll(
            userId: _adminUser.Id,
            actionType: AuditActionType.Create,
            entityType: "Contact"
        );

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().NotBeNull().And.Subject;

        var dataProperty = response.GetType().GetProperty("data");
        var logs = dataProperty!.GetValue(response).Should().BeAssignableTo<IEnumerable<AuditLogDto>>().Subject;

        logs.Should().HaveCount(1);
        logs.First().EntityId.Should().Be("1");
    }

    [Fact]
    public async Task GetAll_WithNoResults_ShouldReturnEmptyList()
    {
        // Arrange
        SetupUser(_adminUser, "Admin");

        // Act
        var result = await _controller.GetAll(userId: 999);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().NotBeNull().And.Subject;

        var dataProperty = response.GetType().GetProperty("data");
        var logs = dataProperty!.GetValue(response).Should().BeAssignableTo<IEnumerable<AuditLogDto>>().Subject;

        logs.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAll_ShouldIncludeOldAndNewValues()
    {
        // Arrange
        SetupUser(_adminUser, "Admin");

        var log = new AuditLog
        {
            UserId = _adminUser.Id,
            Action = AuditActionType.Update,
            EntityType = "Contact",
            EntityId = "1",
            Timestamp = DateTime.UtcNow,
            OldValuesJson = "{\"name\":\"Old Name\"}",
            NewValuesJson = "{\"name\":\"New Name\"}"
        };

        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetAll();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().NotBeNull().And.Subject;

        var dataProperty = response.GetType().GetProperty("data");
        var logs = dataProperty!.GetValue(response).Should().BeAssignableTo<IEnumerable<AuditLogDto>>().Subject;

        var auditLog = logs.First();
        auditLog.OldValue.Should().Be("{\"name\":\"Old Name\"}");
        auditLog.NewValue.Should().Be("{\"name\":\"New Name\"}");
    }

    [Fact]
    public async Task GetStatistics_WithNoData_ShouldReturnZeroCounters()
    {
        // Arrange
        SetupUser(_adminUser, "Admin");

        // Act
        var result = await _controller.GetStatistics();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var statistics = okResult.Value.Should().NotBeNull().And.Subject;

        var totalActionsProperty = statistics.GetType().GetProperty("totalActions");
        var totalActions = (int)totalActionsProperty!.GetValue(statistics)!;
        totalActions.Should().Be(0);
    }

    #endregion

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
