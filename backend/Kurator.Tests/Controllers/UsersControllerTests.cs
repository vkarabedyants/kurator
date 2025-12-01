using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
/// Comprehensive tests for UsersController (requires Admin role)
/// Covers: GetAll, GetById, GetCurators, Create, Update, Delete, ChangePassword, GetCurrentUser, GetStatistics
/// </summary>
public class UsersControllerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<UsersController> _logger;
    private readonly UsersController _controller;
    private readonly User _adminUser;

    public UsersControllerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _passwordHasher = new PasswordHasher();
        _logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<UsersController>();
        _controller = new UsersController(_context, _passwordHasher, _logger);

        // Create admin user for tests
        _adminUser = new User { Login = "admin", PasswordHash = "hash", Role = UserRole.Admin };
        _context.Users.Add(_adminUser);
        _context.SaveChanges();

        SetupAdminUser();
    }

    private void SetupAdminUser()
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, _adminUser.Id.ToString()),
            new Claim(ClaimTypes.Name, _adminUser.Login),
            new Claim(ClaimTypes.Role, "Admin")
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
    public async Task GetAll_ShouldReturnAllUsersWithBlockAssignments()
    {
        // Arrange
        var curator1 = new User { Login = "curator1", PasswordHash = "hash1", Role = UserRole.Curator };
        var curator2 = new User { Login = "curator2", PasswordHash = "hash2", Role = UserRole.Curator };
        _context.Users.AddRange(curator1, curator2);

        var block1 = new Block { Name = "Block 1", Code = "BLK001", Status = BlockStatus.Active };
        var block2 = new Block { Name = "Block 2", Code = "BLK002", Status = BlockStatus.Active };
        _context.Blocks.AddRange(block1, block2);

        await _context.SaveChangesAsync();

        var assignment1 = new BlockCurator
        {
            BlockId = block1.Id,
            UserId = curator1.Id,
            CuratorType = CuratorType.Primary,
            AssignedAt = DateTime.UtcNow,
            AssignedBy = _adminUser.Id
        };
        var assignment2 = new BlockCurator
        {
            BlockId = block2.Id,
            UserId = curator1.Id,
            CuratorType = CuratorType.Backup,
            AssignedAt = DateTime.UtcNow,
            AssignedBy = _adminUser.Id
        };
        _context.BlockCurators.AddRange(assignment1, assignment2);

        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetAll();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
        var users = okResult.Value.Should().BeAssignableTo<IEnumerable<UserDto>>().Subject;

        users.Should().HaveCount(3); // admin + curator1 + curator2

        var curator1Dto = users.First(u => u.Login == "curator1");
        curator1Dto.Role.Should().Be("Curator");
        curator1Dto.PrimaryBlockIds.Should().Contain(block1.Id);
        curator1Dto.BackupBlockIds.Should().Contain(block2.Id);
    }

    [Fact]
    public async Task GetAll_WithNoUsers_ShouldReturnOnlyCurrentAdmin()
    {
        // Act
        var result = await _controller.GetAll();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var users = okResult.Value.Should().BeAssignableTo<IEnumerable<UserDto>>().Subject;
        users.Should().HaveCount(1);
        users.First().Login.Should().Be("admin");
    }

    [Fact]
    public async Task GetAll_ShouldIncludeMfaStatus()
    {
        // Arrange
        var mfaUser = new User
        {
            Login = "mfauser",
            PasswordHash = "hash",
            Role = UserRole.Curator,
            MfaEnabled = true,
            IsFirstLogin = false
        };
        _context.Users.Add(mfaUser);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetAll();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var users = okResult.Value.Should().BeAssignableTo<IEnumerable<UserDto>>().Subject;

        var mfaUserDto = users.First(u => u.Login == "mfauser");
        mfaUserDto.MfaEnabled.Should().BeTrue();
        mfaUserDto.IsFirstLogin.Should().BeFalse();
    }

    [Fact]
    public async Task GetAll_ShouldReturnCorrectBlockNames()
    {
        // Arrange
        var curator = new User { Login = "curator", PasswordHash = "hash", Role = UserRole.Curator };
        _context.Users.Add(curator);

        var block = new Block { Name = "Test Block Name", Code = "TBN", Status = BlockStatus.Active };
        _context.Blocks.Add(block);

        await _context.SaveChangesAsync();

        var assignment = new BlockCurator
        {
            BlockId = block.Id,
            UserId = curator.Id,
            CuratorType = CuratorType.Primary,
            AssignedAt = DateTime.UtcNow,
            AssignedBy = _adminUser.Id
        };
        _context.BlockCurators.Add(assignment);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetAll();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var users = okResult.Value.Should().BeAssignableTo<IEnumerable<UserDto>>().Subject;

        var curatorDto = users.First(u => u.Login == "curator");
        curatorDto.PrimaryBlockNames.Should().Contain("Test Block Name");
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_WithValidId_ShouldReturnUser()
    {
        // Arrange
        var curator = new User
        {
            Login = "curator",
            PasswordHash = "hash",
            Role = UserRole.Curator,
            IsFirstLogin = false,
            MfaEnabled = true
        };
        _context.Users.Add(curator);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetById(curator.Id);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
        var userDto = okResult.Value.Should().BeOfType<UserDto>().Subject;

        userDto.Id.Should().Be(curator.Id);
        userDto.Login.Should().Be("curator");
        userDto.Role.Should().Be("Curator");
        userDto.MfaEnabled.Should().BeTrue();
        userDto.IsFirstLogin.Should().BeFalse();
    }

    [Fact]
    public async Task GetById_WithInvalidId_ShouldReturnNotFound()
    {
        // Act
        var result = await _controller.GetById(999);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public async Task GetById_WithNonPositiveId_ShouldReturnNotFound(int invalidId)
    {
        // Act
        var result = await _controller.GetById(invalidId);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetById_ShouldIncludeBlockAssignments()
    {
        // Arrange
        var curator = new User { Login = "curator", PasswordHash = "hash", Role = UserRole.Curator };
        _context.Users.Add(curator);

        var block = new Block { Name = "Test Block", Code = "TST", Status = BlockStatus.Active };
        _context.Blocks.Add(block);

        await _context.SaveChangesAsync();

        var assignment = new BlockCurator
        {
            BlockId = block.Id,
            UserId = curator.Id,
            CuratorType = CuratorType.Primary,
            AssignedAt = DateTime.UtcNow,
            AssignedBy = _adminUser.Id
        };
        _context.BlockCurators.Add(assignment);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetById(curator.Id);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var userDto = okResult.Value.Should().BeOfType<UserDto>().Subject;

        userDto.PrimaryBlockIds.Should().Contain(block.Id);
        userDto.PrimaryBlockNames.Should().Contain("Test Block");
    }

    #endregion

    #region GetCurators Tests

    [Fact]
    public async Task GetCurators_ShouldReturnOnlyCuratorUsers()
    {
        // Arrange
        var curator1 = new User { Login = "curator1", PasswordHash = "hash1", Role = UserRole.Curator, IsActive = true };
        var curator2 = new User { Login = "curator2", PasswordHash = "hash2", Role = UserRole.Curator, IsActive = true };
        var analyst = new User { Login = "analyst", PasswordHash = "hash3", Role = UserRole.ThreatAnalyst, IsActive = true };
        _context.Users.AddRange(curator1, curator2, analyst);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetCurators();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();

        var curatorsJson = System.Text.Json.JsonSerializer.Serialize(okResult.Value);
        curatorsJson.Should().Contain("curator1");
        curatorsJson.Should().Contain("curator2");
        curatorsJson.Should().NotContain("analyst");
    }

    [Fact]
    public async Task GetCurators_ShouldReturnOnlyActiveUsers()
    {
        // Arrange
        var activeCurator = new User { Login = "active", PasswordHash = "hash", Role = UserRole.Curator, IsActive = true };
        var inactiveCurator = new User { Login = "inactive", PasswordHash = "hash", Role = UserRole.Curator, IsActive = false };
        _context.Users.AddRange(activeCurator, inactiveCurator);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetCurators();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var curatorsJson = System.Text.Json.JsonSerializer.Serialize(okResult.Value);
        curatorsJson.Should().Contain("active");
        curatorsJson.Should().NotContain("inactive");
    }

    [Fact]
    public async Task GetCurators_WithNoCurators_ShouldReturnEmptyList()
    {
        // Arrange - Admin only exists
        var analyst = new User { Login = "analyst", PasswordHash = "hash", Role = UserRole.ThreatAnalyst, IsActive = true };
        _context.Users.Add(analyst);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetCurators();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var curatorsJson = System.Text.Json.JsonSerializer.Serialize(okResult.Value);
        curatorsJson.Should().Be("[]");
    }

    #endregion

    #region Create Tests

    [Fact]
    public async Task Create_WithValidData_ShouldCreateUser()
    {
        // Arrange
        var request = new CreateUserRequest("newuser", "password123", UserRole.Curator);

        // Act
        var result = await _controller.Create(request);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(_controller.GetById));

        var createdUser = await _context.Users.FirstOrDefaultAsync(u => u.Login == "newuser");
        createdUser.Should().NotBeNull();
        createdUser!.Role.Should().Be(UserRole.Curator);
        createdUser.IsFirstLogin.Should().BeTrue();
        createdUser.IsActive.Should().BeTrue();

        _passwordHasher.VerifyPassword("password123", createdUser.PasswordHash).Should().BeTrue();
    }

    [Theory]
    [InlineData(UserRole.Admin)]
    [InlineData(UserRole.Curator)]
    [InlineData(UserRole.ThreatAnalyst)]
    public async Task Create_WithDifferentRoles_ShouldCreateUserWithCorrectRole(UserRole role)
    {
        // Arrange
        var request = new CreateUserRequest($"user_{role}", "password123", role);

        // Act
        var result = await _controller.Create(request);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();

        var createdUser = await _context.Users.FirstOrDefaultAsync(u => u.Login == $"user_{role}");
        createdUser!.Role.Should().Be(role);
    }

    [Fact]
    public async Task Create_WithExistingLogin_ShouldReturnBadRequest()
    {
        // Arrange
        var existingUser = new User { Login = "existing", PasswordHash = "hash", Role = UserRole.Curator };
        _context.Users.Add(existingUser);
        await _context.SaveChangesAsync();

        var request = new CreateUserRequest("existing", "password123", UserRole.Admin);

        // Act
        var result = await _controller.Create(request);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task Create_ShouldCreateAuditLog()
    {
        // Arrange
        var request = new CreateUserRequest("newuser", "password123", UserRole.Curator);

        // Act
        await _controller.Create(request);

        // Assert
        var auditLog = await _context.AuditLogs.FirstOrDefaultAsync(a => a.EntityType == "User");
        auditLog.Should().NotBeNull();
        auditLog!.Action.Should().Be(AuditActionType.Create);
        auditLog.UserId.Should().Be(_adminUser.Id);
        auditLog.NewValuesJson.Should().Contain("newuser");
    }

    [Fact]
    public async Task Create_WithUnicodeLogin_ShouldWork()
    {
        // Arrange - Russian login
        var request = new CreateUserRequest("Куратор", "password123", UserRole.Curator);

        // Act
        var result = await _controller.Create(request);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
        var createdUser = await _context.Users.FirstOrDefaultAsync(u => u.Login == "Куратор");
        createdUser.Should().NotBeNull();
    }

    [Fact]
    public async Task Create_ShouldSetCreatedAtToNow()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow;
        var request = new CreateUserRequest("newuser", "password123", UserRole.Curator);

        // Act
        await _controller.Create(request);

        // Assert
        var createdUser = await _context.Users.FirstOrDefaultAsync(u => u.Login == "newuser");
        createdUser!.CreatedAt.Should().BeAfter(beforeCreation.AddSeconds(-1));
        createdUser.CreatedAt.Should().BeBefore(DateTime.UtcNow.AddSeconds(1));
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_WithValidData_ShouldUpdateUser()
    {
        // Arrange
        var user = new User
        {
            Login = "user",
            PasswordHash = "hash",
            Role = UserRole.Curator,
            IsActive = true
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var request = new UpdateUserRequest(UserRole.ThreatAnalyst, null);

        // Act
        var result = await _controller.Update(user.Id, request);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        var updatedUser = await _context.Users.FindAsync(user.Id);
        updatedUser!.Role.Should().Be(UserRole.ThreatAnalyst);
    }

    [Fact]
    public async Task Update_WithNewPassword_ShouldUpdatePassword()
    {
        // Arrange
        var user = new User
        {
            Login = "user",
            PasswordHash = _passwordHasher.HashPassword("oldpassword"),
            Role = UserRole.Curator
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var request = new UpdateUserRequest(UserRole.Curator, "newpassword123");

        // Act
        var result = await _controller.Update(user.Id, request);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        var updatedUser = await _context.Users.FindAsync(user.Id);
        _passwordHasher.VerifyPassword("newpassword123", updatedUser!.PasswordHash).Should().BeTrue();
        _passwordHasher.VerifyPassword("oldpassword", updatedUser.PasswordHash).Should().BeFalse();
    }

    [Fact]
    public async Task Update_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var request = new UpdateUserRequest(UserRole.Curator, null);

        // Act
        var result = await _controller.Update(999, request);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Update_ShouldCreateAuditLog()
    {
        // Arrange
        var user = new User { Login = "user", PasswordHash = "hash", Role = UserRole.Curator };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var request = new UpdateUserRequest(UserRole.ThreatAnalyst, null);

        // Act
        await _controller.Update(user.Id, request);

        // Assert
        var auditLog = await _context.AuditLogs.FirstOrDefaultAsync(a =>
            a.EntityType == "User" && a.Action == AuditActionType.Update);
        auditLog.Should().NotBeNull();
        auditLog!.OldValuesJson.Should().Contain("Curator");
        auditLog.NewValuesJson.Should().Contain("ThreatAnalyst");
    }

    [Fact]
    public async Task Update_WithNullPassword_ShouldNotChangePassword()
    {
        // Arrange
        var originalHash = _passwordHasher.HashPassword("originalpassword");
        var user = new User
        {
            Login = "user",
            PasswordHash = originalHash,
            Role = UserRole.Curator
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var request = new UpdateUserRequest(UserRole.ThreatAnalyst, null);

        // Act
        await _controller.Update(user.Id, request);

        // Assert
        var updatedUser = await _context.Users.FindAsync(user.Id);
        updatedUser!.PasswordHash.Should().Be(originalHash);
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_WithValidId_ShouldDeleteUser()
    {
        // Arrange
        var user = new User { Login = "user", PasswordHash = "hash", Role = UserRole.Curator, IsActive = true };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.Delete(user.Id);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        var deletedUser = await _context.Users.FindAsync(user.Id);
        deletedUser.Should().BeNull();
    }

    [Fact]
    public async Task Delete_WithInvalidId_ShouldReturnNotFound()
    {
        // Act
        var result = await _controller.Delete(999);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Delete_OwnAccount_ShouldReturnBadRequest()
    {
        // Act
        var result = await _controller.Delete(_adminUser.Id);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task Delete_UserWithBlockAssignments_ShouldReturnBadRequest()
    {
        // Arrange
        var curator = new User { Login = "curator", PasswordHash = "hash", Role = UserRole.Curator };
        _context.Users.Add(curator);

        var block = new Block { Name = "Test Block", Code = "TST", Status = BlockStatus.Active };
        _context.Blocks.Add(block);

        await _context.SaveChangesAsync();

        var assignment = new BlockCurator
        {
            BlockId = block.Id,
            UserId = curator.Id,
            CuratorType = CuratorType.Primary,
            AssignedAt = DateTime.UtcNow,
            AssignedBy = _adminUser.Id
        };
        _context.BlockCurators.Add(assignment);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.Delete(curator.Id);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task Delete_ShouldCreateAuditLog()
    {
        // Arrange
        var user = new User { Login = "user", PasswordHash = "hash", Role = UserRole.Curator };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        await _controller.Delete(user.Id);

        // Assert
        var auditLog = await _context.AuditLogs.FirstOrDefaultAsync(a =>
            a.EntityType == "User" && a.Action == AuditActionType.Delete);
        auditLog.Should().NotBeNull();
        auditLog!.OldValuesJson.Should().Contain("user");
    }

    #endregion

    #region ChangePassword Tests

    [Fact]
    public async Task ChangePassword_WithValidData_ShouldUpdatePassword()
    {
        // Arrange
        var user = new User
        {
            Login = "user",
            PasswordHash = _passwordHasher.HashPassword("oldpassword"),
            Role = UserRole.Curator
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var request = new ChangePasswordRequest("newpassword123");

        // Act
        var result = await _controller.ChangePassword(user.Id, request);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        var updatedUser = await _context.Users.FindAsync(user.Id);
        _passwordHasher.VerifyPassword("newpassword123", updatedUser!.PasswordHash).Should().BeTrue();
        _passwordHasher.VerifyPassword("oldpassword", updatedUser.PasswordHash).Should().BeFalse();
    }

    [Fact]
    public async Task ChangePassword_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var request = new ChangePasswordRequest("newpassword123");

        // Act
        var result = await _controller.ChangePassword(999, request);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task ChangePassword_ShouldCreateAuditLog()
    {
        // Arrange
        var user = new User { Login = "user", PasswordHash = "hash", Role = UserRole.Curator };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var request = new ChangePasswordRequest("newpassword123");

        // Act
        await _controller.ChangePassword(user.Id, request);

        // Assert
        var auditLog = await _context.AuditLogs.FirstOrDefaultAsync(a =>
            a.EntityType == "User" && a.Action == AuditActionType.Update);
        auditLog.Should().NotBeNull();
        auditLog!.NewValuesJson.Should().Contain("Password changed");
    }

    [Fact]
    public async Task ChangePassword_WithComplexPassword_ShouldWork()
    {
        // Arrange
        var user = new User { Login = "user", PasswordHash = "hash", Role = UserRole.Curator };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var complexPassword = "P@$$w0rd!@#$%^&*()_+-=[]{}|;':\",./<>?`~";
        var request = new ChangePasswordRequest(complexPassword);

        // Act
        var result = await _controller.ChangePassword(user.Id, request);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        var updatedUser = await _context.Users.FindAsync(user.Id);
        _passwordHasher.VerifyPassword(complexPassword, updatedUser!.PasswordHash).Should().BeTrue();
    }

    #endregion

    #region GetCurrentUser Tests

    [Fact]
    public async Task GetCurrentUser_ShouldReturnCurrentUser()
    {
        // Act
        var result = await _controller.GetCurrentUser();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
        var userDto = okResult.Value.Should().BeOfType<UserDto>().Subject;

        userDto.Id.Should().Be(_adminUser.Id);
        userDto.Login.Should().Be("admin");
        userDto.Role.Should().Be("Admin");
    }

    [Fact]
    public async Task GetCurrentUser_ShouldIncludeBlockAssignments()
    {
        // Arrange
        var block = new Block { Name = "Test Block", Code = "TST", Status = BlockStatus.Active };
        _context.Blocks.Add(block);
        await _context.SaveChangesAsync();

        var assignment = new BlockCurator
        {
            BlockId = block.Id,
            UserId = _adminUser.Id,
            CuratorType = CuratorType.Primary,
            AssignedAt = DateTime.UtcNow,
            AssignedBy = _adminUser.Id
        };
        _context.BlockCurators.Add(assignment);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetCurrentUser();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var userDto = okResult.Value.Should().BeOfType<UserDto>().Subject;

        userDto.PrimaryBlockIds.Should().Contain(block.Id);
        userDto.PrimaryBlockNames.Should().Contain("Test Block");
    }

    #endregion

    #region GetUserStatistics Tests

    [Fact]
    public async Task GetUserStatistics_ShouldReturnUserStatistics()
    {
        // Arrange
        var admin2 = new User { Login = "admin2", PasswordHash = "hash", Role = UserRole.Admin };
        var curator1 = new User { Login = "curator1", PasswordHash = "hash", Role = UserRole.Curator };
        var curator2 = new User { Login = "curator2", PasswordHash = "hash", Role = UserRole.Curator };
        var analyst = new User { Login = "analyst", PasswordHash = "hash", Role = UserRole.ThreatAnalyst };
        var inactiveUser = new User { Login = "inactive", PasswordHash = "hash", Role = UserRole.Curator, IsActive = false };

        _context.Users.AddRange(admin2, curator1, curator2, analyst, inactiveUser);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetUserStatistics();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();

        var stats = okResult.Value!;
        var totalUsersProperty = stats.GetType().GetProperty("totalUsers");
        var byRoleProperty = stats.GetType().GetProperty("byRole");

        totalUsersProperty.Should().NotBeNull();
        totalUsersProperty!.GetValue(stats).Should().Be(6); // admin + admin2 + curator1 + curator2 + analyst + inactive

        byRoleProperty.Should().NotBeNull();
        byRoleProperty!.GetValue(stats).Should().NotBeNull();
    }

    [Fact]
    public async Task GetUserStatistics_ShouldIncludeActiveInLastMonth()
    {
        // Arrange
        var recentUser = new User
        {
            Login = "recent",
            PasswordHash = "hash",
            Role = UserRole.Curator,
            LastLoginAt = DateTime.UtcNow.AddDays(-5)
        };
        var oldUser = new User
        {
            Login = "old",
            PasswordHash = "hash",
            Role = UserRole.Curator,
            LastLoginAt = DateTime.UtcNow.AddMonths(-2)
        };

        _context.Users.AddRange(recentUser, oldUser);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetUserStatistics();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var stats = okResult.Value!;
        var activeInLastMonthProperty = stats.GetType().GetProperty("activeInLastMonth");
        activeInLastMonthProperty.Should().NotBeNull();
        var activeCount = (int)activeInLastMonthProperty!.GetValue(stats)!;
        activeCount.Should().BeGreaterOrEqualTo(1); // At least the recent user
    }

    [Fact]
    public async Task GetUserStatistics_WithNoUsers_ShouldReturnZeroStats()
    {
        // Act - Only admin exists
        var result = await _controller.GetUserStatistics();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var stats = okResult.Value!;
        var totalUsersProperty = stats.GetType().GetProperty("totalUsers");
        totalUsersProperty!.GetValue(stats).Should().Be(1);
    }

    #endregion

    #region ToggleActive Tests

    [Fact]
    public async Task ToggleActive_ShouldDeactivateActiveUser()
    {
        // Arrange
        var user = new User { Login = "activeuser", PasswordHash = "hash", Role = UserRole.Curator, IsActive = true };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.ToggleActive(user.Id);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value;
        var isActive = (bool)response!.GetType().GetProperty("isActive")!.GetValue(response)!;
        isActive.Should().BeFalse();

        var updatedUser = await _context.Users.FindAsync(user.Id);
        updatedUser!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task ToggleActive_ShouldActivateInactiveUser()
    {
        // Arrange
        var user = new User { Login = "inactiveuser", PasswordHash = "hash", Role = UserRole.Curator, IsActive = false };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.ToggleActive(user.Id);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value;
        var isActive = (bool)response!.GetType().GetProperty("isActive")!.GetValue(response)!;
        isActive.Should().BeTrue();

        var updatedUser = await _context.Users.FindAsync(user.Id);
        updatedUser!.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task ToggleActive_ShouldNotAllowSelfDeactivation()
    {
        // Arrange - _adminUser is the current user

        // Act
        var result = await _controller.ToggleActive(_adminUser.Id);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ToggleActive_WithNonExistentId_ShouldReturnNotFound()
    {
        // Act
        var result = await _controller.ToggleActive(99999);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task ToggleActive_ShouldCreateAuditLog()
    {
        // Arrange
        var user = new User { Login = "userforaudit", PasswordHash = "hash", Role = UserRole.Curator, IsActive = true };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        await _controller.ToggleActive(user.Id);

        // Assert
        var auditLog = await _context.AuditLogs.FirstOrDefaultAsync(a =>
            a.EntityType == "User" && a.EntityId == user.Id.ToString() && a.Action == AuditActionType.Update);
        auditLog.Should().NotBeNull();
        auditLog!.OldValuesJson.Should().Contain("IsActive: True");
        auditLog.NewValuesJson.Should().Contain("IsActive: False");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Create_WithVeryLongPassword_ShouldWork()
    {
        // Arrange
        var longPassword = new string('a', 1000);
        var request = new CreateUserRequest("newuser", longPassword, UserRole.Curator);

        // Act
        var result = await _controller.Create(request);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();

        var createdUser = await _context.Users.FirstOrDefaultAsync(u => u.Login == "newuser");
        _passwordHasher.VerifyPassword(longPassword, createdUser!.PasswordHash).Should().BeTrue();
    }

    [Fact]
    public async Task Create_WithSpecialCharactersInLogin_ShouldWork()
    {
        // Arrange
        var request = new CreateUserRequest("user@example.com", "password123", UserRole.Curator);

        // Act
        var result = await _controller.Create(request);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task GetAll_WithManyUsers_ShouldReturnAllUsers()
    {
        // Arrange
        for (int i = 0; i < 100; i++)
        {
            _context.Users.Add(new User
            {
                Login = $"user{i}",
                PasswordHash = "hash",
                Role = UserRole.Curator
            });
        }
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetAll();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var users = okResult.Value.Should().BeAssignableTo<IEnumerable<UserDto>>().Subject;
        users.Should().HaveCount(101); // 100 + admin
    }

    #endregion

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
