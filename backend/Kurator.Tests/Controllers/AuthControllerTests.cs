using Xunit;
using FluentAssertions;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Kurator.Api.Controllers;
using Kurator.Infrastructure.Data;
using Kurator.Infrastructure.Services;
using Kurator.Core.Entities;
using Kurator.Core.Enums;
using System.Net;

namespace Kurator.Tests.Controllers;

/// <summary>
/// Comprehensive tests for AuthController
/// Covers: Login, Register, SetupMfa, VerifyMfa endpoints
/// </summary>
public class AuthControllerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly PasswordHasher _passwordHasher;
    private readonly IConfiguration _configuration;
    private readonly Mock<ILogger<AuthController>> _loggerMock;
    private readonly TotpService _totpService;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _passwordHasher = new PasswordHasher();

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                {"JwtSettings:Secret", "test-secret-key-that-is-long-enough-for-jwt-token-generation-min-32-chars"},
                {"JwtSettings:Issuer", "TestIssuer"},
                {"JwtSettings:Audience", "TestAudience"},
                {"JwtSettings:ExpiryMinutes", "60"}
            })
            .Build();

        _loggerMock = new Mock<ILogger<AuthController>>();
        _totpService = new TotpService();
        _controller = new AuthController(_context, _passwordHasher, _configuration, _loggerMock.Object, _totpService);

        // Setup HttpContext with mocked connection for RemoteIpAddress
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.1");
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    #region Login Tests - Positive Cases

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

        var valueType = okResult.Value!.GetType();
        var tokenProperty = valueType.GetProperty("token");
        tokenProperty.Should().NotBeNull();
        var tokenValue = tokenProperty!.GetValue(okResult.Value) as string;
        tokenValue.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldUpdateLastLoginAt()
    {
        // Arrange
        var user = new User
        {
            Login = "testuser",
            PasswordHash = _passwordHasher.HashPassword("password123"),
            Role = UserRole.Curator,
            IsFirstLogin = false,
            MfaEnabled = false,
            LastLoginAt = DateTime.UtcNow.AddDays(-1)
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var request = new LoginRequest("testuser", "password123");
        var beforeLogin = DateTime.UtcNow;

        // Act
        await _controller.Login(request);

        // Assert
        var updatedUser = await _context.Users.FindAsync(user.Id);
        updatedUser!.LastLoginAt.Should().BeAfter(beforeLogin.AddSeconds(-1));
    }

    [Theory]
    [InlineData(UserRole.Admin)]
    [InlineData(UserRole.Curator)]
    [InlineData(UserRole.ThreatAnalyst)]
    public async Task Login_WithDifferentRoles_ShouldReturnCorrectRoleInToken(UserRole role)
    {
        // Arrange
        var user = new User
        {
            Login = $"user_{role}",
            PasswordHash = _passwordHasher.HashPassword("password123"),
            Role = role,
            IsFirstLogin = false,
            MfaEnabled = false
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var request = new LoginRequest($"user_{role}", "password123");

        // Act
        var result = await _controller.Login(request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();

        var valueType = okResult.Value!.GetType();
        var userProperty = valueType.GetProperty("user");
        userProperty.Should().NotBeNull();
        var userValue = userProperty!.GetValue(okResult.Value);
        var roleProperty = userValue!.GetType().GetProperty("role");
        var roleValue = roleProperty!.GetValue(userValue) as string;
        roleValue.Should().Be(role.ToString());
    }

    #endregion

    #region Login Tests - Negative Cases

    [Theory]
    [InlineData("testuser", "wrongpassword", true)]
    [InlineData("nonexistent", "password123", false)]
    public async Task Login_WithInvalidCredentials_ShouldReturnUnauthorized(string login, string password, bool createUser)
    {
        // Arrange
        if (createUser)
        {
            var user = new User
            {
                Login = "testuser",
                PasswordHash = _passwordHasher.HashPassword("password123"),
                Role = UserRole.Curator
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }

        var request = new LoginRequest(login, password);

        // Act
        var result = await _controller.Login(request);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Login_WithEmptyLogin_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new LoginRequest("", "password123");

        // Act
        var result = await _controller.Login(request);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Login_WithEmptyPassword_ShouldReturnUnauthorized()
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

        var request = new LoginRequest("testuser", "");

        // Act
        var result = await _controller.Login(request);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Login_WithCaseSensitiveLogin_ShouldReturnUnauthorized()
    {
        // Arrange
        var user = new User
        {
            Login = "TestUser",
            PasswordHash = _passwordHasher.HashPassword("password123"),
            Role = UserRole.Curator,
            IsFirstLogin = false
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var request = new LoginRequest("testuser", "password123");

        // Act
        var result = await _controller.Login(request);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    #endregion

    #region Login Tests - MFA Flow

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

        var valueType = okResult.Value!.GetType();
        var requireMfaSetupProperty = valueType.GetProperty("requireMfaSetup");
        requireMfaSetupProperty.Should().NotBeNull();
        var requireMfaSetupValue = (bool)requireMfaSetupProperty!.GetValue(okResult.Value)!;
        requireMfaSetupValue.Should().BeTrue();

        var userIdProperty = valueType.GetProperty("userId");
        userIdProperty.Should().NotBeNull();
        var userIdValue = (int)userIdProperty!.GetValue(okResult.Value)!;
        userIdValue.Should().Be(user.Id);
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

        var valueType = okResult.Value!.GetType();
        var requireMfaVerificationProperty = valueType.GetProperty("requireMfaVerification");
        requireMfaVerificationProperty.Should().NotBeNull();
        var requireMfaVerificationValue = (bool)requireMfaVerificationProperty!.GetValue(okResult.Value)!;
        requireMfaVerificationValue.Should().BeTrue();
    }

    [Fact]
    public async Task Login_WithFirstLoginAndMfaEnabled_ShouldPrioritizeFirstLogin()
    {
        // Arrange
        var user = new User
        {
            Login = "testuser",
            PasswordHash = _passwordHasher.HashPassword("password123"),
            Role = UserRole.Curator,
            IsFirstLogin = true,
            MfaEnabled = true,
            MfaSecret = "TEST_SECRET"
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var request = new LoginRequest("testuser", "password123");

        // Act
        var result = await _controller.Login(request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var valueType = okResult.Value!.GetType();
        var requireMfaSetupProperty = valueType.GetProperty("requireMfaSetup");
        requireMfaSetupProperty.Should().NotBeNull();
        var requireMfaSetupValue = (bool)requireMfaSetupProperty!.GetValue(okResult.Value)!;
        requireMfaSetupValue.Should().BeTrue();
    }

    #endregion

    #region Register Tests - Positive Cases

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

    [Theory]
    [InlineData(UserRole.Admin)]
    [InlineData(UserRole.Curator)]
    [InlineData(UserRole.ThreatAnalyst)]
    public async Task Register_WithDifferentRoles_ShouldCreateUserWithCorrectRole(UserRole role)
    {
        // Arrange
        var request = new RegisterRequest($"user_{role}", "password123", role);

        // Act
        var result = await _controller.Register(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Login == $"user_{role}");
        user.Should().NotBeNull();
        user!.Role.Should().Be(role);
    }

    [Fact]
    public async Task Register_ShouldHashPassword()
    {
        // Arrange
        var request = new RegisterRequest("newuser", "password123", UserRole.Curator);

        // Act
        await _controller.Register(request);

        // Assert
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Login == "newuser");
        user.Should().NotBeNull();
        user!.PasswordHash.Should().NotBe("password123");
        _passwordHasher.VerifyPassword("password123", user.PasswordHash).Should().BeTrue();
    }

    [Fact]
    public async Task Register_ShouldSetCreatedAtToNow()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow;
        var request = new RegisterRequest("newuser", "password123", UserRole.Curator);

        // Act
        await _controller.Register(request);

        // Assert
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Login == "newuser");
        user!.CreatedAt.Should().BeAfter(beforeCreation.AddSeconds(-1));
        user.CreatedAt.Should().BeBefore(DateTime.UtcNow.AddSeconds(1));
    }

    [Fact]
    public async Task Register_WithUnicodeLogin_ShouldCreateUser()
    {
        // Arrange - Russian login
        var request = new RegisterRequest("Пользователь", "password123", UserRole.Curator);

        // Act
        var result = await _controller.Register(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Login == "Пользователь");
        user.Should().NotBeNull();
    }

    #endregion

    #region Register Tests - Negative Cases

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
    public async Task Register_WithExistingLoginDifferentCase_ShouldCreateUser()
    {
        // Arrange - Logins are case-sensitive
        var existingUser = new User
        {
            Login = "TestUser",
            PasswordHash = _passwordHasher.HashPassword("password123"),
            Role = UserRole.Curator
        };
        _context.Users.Add(existingUser);
        await _context.SaveChangesAsync();

        var request = new RegisterRequest("testuser", "newpassword", UserRole.Admin);

        // Act
        var result = await _controller.Register(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    #endregion

    #region SetupMfa Tests - Positive Cases

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

        var valueType = okResult.Value!.GetType();
        var mfaSecretProperty = valueType.GetProperty("mfaSecret");
        mfaSecretProperty.Should().NotBeNull();
        var mfaSecretValue = mfaSecretProperty!.GetValue(okResult.Value) as string;
        mfaSecretValue.Should().NotBeNullOrEmpty();

        var qrCodeUrlProperty = valueType.GetProperty("qrCodeUrl");
        qrCodeUrlProperty.Should().NotBeNull();
        var qrCodeUrlValue = qrCodeUrlProperty!.GetValue(okResult.Value) as string;
        qrCodeUrlValue.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task SetupMfa_ShouldSaveSecretToUser()
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
        await _controller.SetupMfa(request);

        // Assert
        var updatedUser = await _context.Users.FindAsync(user.Id);
        updatedUser!.MfaSecret.Should().NotBeNullOrEmpty();
        updatedUser.IsFirstLogin.Should().BeFalse();
        updatedUser.MfaEnabled.Should().BeFalse(); // Should be false until first code verification
    }

    [Fact]
    public async Task SetupMfa_WithPublicKey_ShouldSavePublicKey()
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

        var publicKey = "-----BEGIN PUBLIC KEY-----\nMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8A...\n-----END PUBLIC KEY-----";
        var request = new SetupMfaRequest(user.Id, "password123", publicKey);

        // Act
        await _controller.SetupMfa(request);

        // Assert
        var updatedUser = await _context.Users.FindAsync(user.Id);
        updatedUser!.PublicKey.Should().Be(publicKey);
    }

    #endregion

    #region SetupMfa Tests - Negative Cases

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
    public async Task SetupMfa_WithEmptyPassword_ShouldReturnUnauthorized()
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

        var request = new SetupMfaRequest(user.Id, "", null);

        // Act
        var result = await _controller.SetupMfa(request);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public async Task SetupMfa_WithInvalidUserId_ShouldReturnNotFound(int invalidUserId)
    {
        // Arrange
        var request = new SetupMfaRequest(invalidUserId, "password123", null);

        // Act
        var result = await _controller.SetupMfa(request);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region VerifyMfa Tests - Positive Cases

    [Fact]
    public async Task VerifyMfa_WithValidCode_ShouldReturnTokenAndEnableMfa()
    {
        // Arrange
        var validSecret = _totpService.GenerateSecret();
        var validCode = _totpService.GenerateCode(validSecret);

        var user = new User
        {
            Login = "testuser",
            PasswordHash = _passwordHasher.HashPassword("password123"),
            Role = UserRole.Curator,
            MfaSecret = validSecret,
            MfaEnabled = false
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var request = new VerifyMfaRequest(user.Id, validCode);

        // Act
        var result = await _controller.VerifyMfa(request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();

        var valueType = okResult.Value!.GetType();
        var tokenProperty = valueType.GetProperty("token");
        tokenProperty.Should().NotBeNull();
        var tokenValue = tokenProperty!.GetValue(okResult.Value) as string;
        tokenValue.Should().NotBeNullOrEmpty();

        var updatedUser = await _context.Users.FindAsync(user.Id);
        updatedUser!.MfaEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task VerifyMfa_WithAlreadyEnabledMfa_ShouldReturnToken()
    {
        // Arrange
        var validSecret = _totpService.GenerateSecret();
        var validCode = _totpService.GenerateCode(validSecret);

        var user = new User
        {
            Login = "testuser",
            PasswordHash = _passwordHasher.HashPassword("password123"),
            Role = UserRole.Curator,
            MfaSecret = validSecret,
            MfaEnabled = true
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var request = new VerifyMfaRequest(user.Id, validCode);

        // Act
        var result = await _controller.VerifyMfa(request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var valueType = okResult.Value!.GetType();
        var tokenProperty = valueType.GetProperty("token");
        var tokenValue = tokenProperty!.GetValue(okResult.Value) as string;
        tokenValue.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task VerifyMfa_ShouldUpdateLastLoginAt()
    {
        // Arrange
        var validSecret = _totpService.GenerateSecret();
        var validCode = _totpService.GenerateCode(validSecret);

        var user = new User
        {
            Login = "testuser",
            PasswordHash = _passwordHasher.HashPassword("password123"),
            Role = UserRole.Curator,
            MfaSecret = validSecret,
            MfaEnabled = false,
            LastLoginAt = DateTime.UtcNow.AddDays(-1)
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var request = new VerifyMfaRequest(user.Id, validCode);
        var beforeVerification = DateTime.UtcNow;

        // Act
        await _controller.VerifyMfa(request);

        // Assert
        var updatedUser = await _context.Users.FindAsync(user.Id);
        updatedUser!.LastLoginAt.Should().BeAfter(beforeVerification.AddSeconds(-1));
    }

    #endregion

    #region VerifyMfa Tests - Negative Cases

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

    [Fact]
    public async Task VerifyMfa_WithNonExistentUser_ShouldReturnNotFound()
    {
        // Arrange
        var request = new VerifyMfaRequest(999, "123456");

        // Act
        var result = await _controller.VerifyMfa(request);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task VerifyMfa_WithEmptyCode_ShouldReturnUnauthorized()
    {
        // Arrange
        var validSecret = _totpService.GenerateSecret();

        var user = new User
        {
            Login = "testuser",
            PasswordHash = _passwordHasher.HashPassword("password123"),
            Role = UserRole.Curator,
            MfaSecret = validSecret
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var request = new VerifyMfaRequest(user.Id, "");

        // Act
        var result = await _controller.VerifyMfa(request);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Theory]
    [InlineData("12345")]      // Too short
    [InlineData("1234567")]    // Too long
    [InlineData("abcdef")]     // Non-numeric
    [InlineData("12 34 56")]   // With spaces
    public async Task VerifyMfa_WithMalformedCode_ShouldReturnUnauthorized(string malformedCode)
    {
        // Arrange
        var validSecret = _totpService.GenerateSecret();

        var user = new User
        {
            Login = "testuser",
            PasswordHash = _passwordHasher.HashPassword("password123"),
            Role = UserRole.Curator,
            MfaSecret = validSecret
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var request = new VerifyMfaRequest(user.Id, malformedCode);

        // Act
        var result = await _controller.VerifyMfa(request);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public async Task VerifyMfa_WithInvalidUserId_ShouldReturnNotFound(int invalidUserId)
    {
        // Arrange
        var request = new VerifyMfaRequest(invalidUserId, "123456");

        // Act
        var result = await _controller.VerifyMfa(request);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Login_WithVeryLongPassword_ShouldWork()
    {
        // Arrange
        var longPassword = new string('a', 1000);
        var user = new User
        {
            Login = "testuser",
            PasswordHash = _passwordHasher.HashPassword(longPassword),
            Role = UserRole.Curator,
            IsFirstLogin = false,
            MfaEnabled = false
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var request = new LoginRequest("testuser", longPassword);

        // Act
        var result = await _controller.Login(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Login_WithSpecialCharactersInPassword_ShouldWork()
    {
        // Arrange
        var specialPassword = "!@#$%^&*()_+-=[]{}|;':\",./<>?`~";
        var user = new User
        {
            Login = "testuser",
            PasswordHash = _passwordHasher.HashPassword(specialPassword),
            Role = UserRole.Curator,
            IsFirstLogin = false,
            MfaEnabled = false
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var request = new LoginRequest("testuser", specialPassword);

        // Act
        var result = await _controller.Login(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Register_WithSpecialCharactersInLogin_ShouldWork()
    {
        // Arrange
        var request = new RegisterRequest("user_with-special.chars@test", "password123", UserRole.Curator);

        // Act
        var result = await _controller.Register(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Login == "user_with-special.chars@test");
        user.Should().NotBeNull();
    }

    [Fact]
    public async Task ConcurrentLogins_ShouldUpdateLastLoginAt()
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

        // Act - Simulate concurrent logins
        var task1 = _controller.Login(request);
        var task2 = _controller.Login(request);
        var results = await Task.WhenAll(task1, task2);

        // Assert - Both should succeed
        results[0].Should().BeOfType<OkObjectResult>();
        results[1].Should().BeOfType<OkObjectResult>();
    }

    #endregion

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
