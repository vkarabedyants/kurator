using Xunit;
using FluentAssertions;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Kurator.Api.Controllers;
using Kurator.Infrastructure.Data;
using Kurator.Infrastructure.Services;
using Kurator.Core.Entities;
using Kurator.Core.Enums;

namespace Kurator.Tests.Controllers;

public class AuthControllerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly PasswordHasher _passwordHasher;
    private readonly IConfiguration _configuration;
    private readonly Mock<ILogger<AuthController>> _loggerMock;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _passwordHasher = new PasswordHasher();

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                {"JwtSettings:Secret", "test-secret-key-that-is-long-enough-for-jwt-token-generation-min-32-chars"},
                {"JwtSettings:Issuer", "TestIssuer"},
                {"JwtSettings:Audience", "TestAudience"},
                {"JwtSettings:ExpiryMinutes", "60"}
            }!)
            .Build();

        _loggerMock = new Mock<ILogger<AuthController>>();
        _controller = new AuthController(_context, _passwordHasher, _configuration, _loggerMock.Object);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturnOkWithToken()
    {
        // Arrange
        var user = new User
        {
            Login = "testuser",
            PasswordHash = _passwordHasher.HashPassword("password123"),
            Role = UserRole.Curator,
            IsFirstLogin = false,
            MfaEnabled = false
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var request = new LoginRequest("testuser", "password123");

        // Act
        var result = await _controller.Login(request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
        okResult.Value.GetType().GetProperty("token").Should().NotBeNull();
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ShouldReturnUnauthorized()
    {
        // Arrange
        var user = new User
        {
            Login = "testuser",
            PasswordHash = _passwordHasher.HashPassword("password123"),
            Role = UserRole.Curator
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var request = new LoginRequest("testuser", "wrongpassword");

        // Act
        var result = await _controller.Login(request);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Login_WithNonExistentUser_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new LoginRequest("nonexistent", "password123");

        // Act
        var result = await _controller.Login(request);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Login_WithFirstLoginUser_ShouldReturnRequireMfaSetup()
    {
        // Arrange
        var user = new User
        {
            Login = "newuser",
            PasswordHash = _passwordHasher.HashPassword("password123"),
            Role = UserRole.Curator,
            IsFirstLogin = true,
            MfaEnabled = false
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var request = new LoginRequest("newuser", "password123");

        // Act
        var result = await _controller.Login(request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
        okResult.Value.GetType().GetProperty("requireMfaSetup").Should().NotBeNull();
    }

    [Fact]
    public async Task Login_WithMfaEnabledUser_ShouldReturnRequireMfaVerification()
    {
        // Arrange
        var user = new User
        {
            Login = "mfauser",
            PasswordHash = _passwordHasher.HashPassword("password123"),
            Role = UserRole.Curator,
            IsFirstLogin = false,
            MfaEnabled = true,
            MfaSecret = "TEST_SECRET"
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var request = new LoginRequest("mfauser", "password123");

        // Act
        var result = await _controller.Login(request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
        okResult.Value.GetType().GetProperty("requireMfaVerification").Should().NotBeNull();
    }

    [Fact]
    public async Task Register_WithValidData_ShouldCreateUser()
    {
        // Arrange
        var request = new RegisterRequest("newuser", "password123", UserRole.Curator);

        // Act
        var result = await _controller.Register(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Login == "newuser");
        user.Should().NotBeNull();
        user!.Role.Should().Be(UserRole.Curator);
    }

    [Fact]
    public async Task Register_WithExistingLogin_ShouldReturnBadRequest()
    {
        // Arrange
        var existingUser = new User
        {
            Login = "existinguser",
            PasswordHash = _passwordHasher.HashPassword("password123"),
            Role = UserRole.Curator
        };
        _context.Users.Add(existingUser);
        await _context.SaveChangesAsync();

        var request = new RegisterRequest("existinguser", "newpassword", UserRole.Admin);

        // Act
        var result = await _controller.Register(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task SetupMfa_WithValidData_ShouldReturnMfaSecret()
    {
        // Arrange
        var user = new User
        {
            Login = "testuser",
            PasswordHash = _passwordHasher.HashPassword("password123"),
            Role = UserRole.Curator,
            IsFirstLogin = true
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var request = new SetupMfaRequest(user.Id, "password123", null);

        // Act
        var result = await _controller.SetupMfa(request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
        okResult.Value.GetType().GetProperty("mfaSecret").Should().NotBeNull();
        okResult.Value.GetType().GetProperty("qrCodeUrl").Should().NotBeNull();
    }

    [Fact]
    public async Task SetupMfa_WithInvalidPassword_ShouldReturnUnauthorized()
    {
        // Arrange
        var user = new User
        {
            Login = "testuser",
            PasswordHash = _passwordHasher.HashPassword("password123"),
            Role = UserRole.Curator
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var request = new SetupMfaRequest(user.Id, "wrongpassword", null);

        // Act
        var result = await _controller.SetupMfa(request);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task SetupMfa_WithNonExistentUser_ShouldReturnNotFound()
    {
        // Arrange
        var request = new SetupMfaRequest(999, "password123", null);

        // Act
        var result = await _controller.SetupMfa(request);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task VerifyMfa_WithValidCode_ShouldReturnTokenAndEnableMfa()
    {
        // Arrange
        var user = new User
        {
            Login = "testuser",
            PasswordHash = _passwordHasher.HashPassword("password123"),
            Role = UserRole.Curator,
            MfaSecret = "TEST_SECRET",
            MfaEnabled = false
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var request = new VerifyMfaRequest(user.Id, "123456"); // Any 6-digit code works with stub

        // Act
        var result = await _controller.VerifyMfa(request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
        okResult.Value.GetType().GetProperty("token").Should().NotBeNull();

        var updatedUser = await _context.Users.FindAsync(user.Id);
        updatedUser!.MfaEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task VerifyMfa_WithInvalidCode_ShouldReturnUnauthorized()
    {
        // Arrange
        var user = new User
        {
            Login = "testuser",
            PasswordHash = _passwordHasher.HashPassword("password123"),
            Role = UserRole.Curator,
            MfaSecret = "TEST_SECRET"
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var request = new VerifyMfaRequest(user.Id, "invalid");

        // Act
        var result = await _controller.VerifyMfa(request);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task VerifyMfa_WithoutMfaSetup_ShouldReturnBadRequest()
    {
        // Arrange
        var user = new User
        {
            Login = "testuser",
            PasswordHash = _passwordHasher.HashPassword("password123"),
            Role = UserRole.Curator
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var request = new VerifyMfaRequest(user.Id, "123456");

        // Act
        var result = await _controller.VerifyMfa(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
