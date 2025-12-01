using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Diagnostics;
using Kurator.Core.Entities;
using Kurator.Core.Interfaces;
using Kurator.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Kurator.Infrastructure.Data;

namespace Kurator.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;
    private readonly TotpService _totpService;

    public AuthController(
        ApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IConfiguration configuration,
        ILogger<AuthController> logger,
        TotpService totpService)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
        _logger = logger;
        _totpService = totpService;

        _logger.LogDebug("AuthController instantiated");
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        _logger.LogInformation("[Auth] Login attempt started for user: {Login} from IP: {ClientIP}",
            request.Login, clientIp);

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Login == request.Login);

        if (user == null)
        {
            stopwatch.Stop();
            _logger.LogWarning("[Auth] Login failed - User not found: {Login}, IP: {ClientIP}, Duration: {Duration}ms",
                request.Login, clientIp, stopwatch.ElapsedMilliseconds);
            return Unauthorized(new { message = "Invalid credentials" });
        }

        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            stopwatch.Stop();
            _logger.LogWarning("[Auth] Login failed - Invalid password for user: {Login} (ID: {UserId}), IP: {ClientIP}, Duration: {Duration}ms",
                request.Login, user.Id, clientIp, stopwatch.ElapsedMilliseconds);
            return Unauthorized(new { message = "Invalid credentials" });
        }

        _logger.LogDebug("[Auth] Password verified successfully for user: {Login} (ID: {UserId})",
            request.Login, user.Id);

        // Check if first login - require MFA setup
        if (user.IsFirstLogin)
        {
            stopwatch.Stop();
            _logger.LogInformation("[Auth] First login detected for user: {Login} (ID: {UserId}), requiring MFA setup, Duration: {Duration}ms",
                request.Login, user.Id, stopwatch.ElapsedMilliseconds);
            return Ok(new
            {
                requireMfaSetup = true,
                userId = user.Id,
                login = user.Login,
                message = "First login detected. Please set up MFA."
            });
        }

        // Check if MFA is enabled - require TOTP code
        if (user.MfaEnabled)
        {
            stopwatch.Stop();
            _logger.LogInformation("[Auth] MFA verification required for user: {Login} (ID: {UserId}), Duration: {Duration}ms",
                request.Login, user.Id, stopwatch.ElapsedMilliseconds);
            return Ok(new
            {
                requireMfaVerification = true,
                userId = user.Id,
                message = "MFA verification required"
            });
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        _logger.LogDebug("[Auth] Updated last login timestamp for user: {Login} (ID: {UserId})", request.Login, user.Id);

        var token = GenerateJwtToken(user);

        stopwatch.Stop();
        _logger.LogInformation("[Auth] Login successful for user: {Login} (ID: {UserId}), Role: {Role}, IP: {ClientIP}, Duration: {Duration}ms",
            request.Login, user.Id, user.Role, clientIp, stopwatch.ElapsedMilliseconds);

        return Ok(new
        {
            token,
            user = new
            {
                user.Id,
                user.Login,
                role = user.Role.ToString(),
                isFirstLogin = user.IsFirstLogin,
                mfaEnabled = user.MfaEnabled
            }
        });
    }

    // REMOVED: Self-registration is not allowed per specification.
    // User creation is handled through UsersController by Admin only.
    // This endpoint is kept but protected for backward compatibility with tests.
    [HttpPost("register")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        _logger.LogInformation("[Auth] Registration attempt started for user: {Login}, Role: {Role}, IP: {ClientIP}",
            request.Login, request.Role, clientIp);

        if (await _context.Users.AnyAsync(u => u.Login == request.Login))
        {
            stopwatch.Stop();
            _logger.LogWarning("[Auth] Registration failed - User already exists: {Login}, IP: {ClientIP}, Duration: {Duration}ms",
                request.Login, clientIp, stopwatch.ElapsedMilliseconds);
            return BadRequest(new { message = "User already exists" });
        }

        _logger.LogDebug("[Auth] Creating new user: {Login}", request.Login);

        var user = new User
        {
            Login = request.Login,
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            Role = request.Role,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        stopwatch.Stop();
        _logger.LogInformation("[Auth] User registered successfully: {Login} (ID: {UserId}), Role: {Role}, IP: {ClientIP}, Duration: {Duration}ms",
            user.Login, user.Id, user.Role, clientIp, stopwatch.ElapsedMilliseconds);

        return Ok(new { message = "User registered successfully" });
    }

    [HttpPost("setup-mfa")]
    public async Task<IActionResult> SetupMfa([FromBody] SetupMfaRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        _logger.LogInformation("[Auth] MFA setup initiated for UserId: {UserId}, IP: {ClientIP}",
            request.UserId, clientIp);

        var user = await _context.Users.FindAsync(request.UserId);

        if (user == null)
        {
            stopwatch.Stop();
            _logger.LogWarning("[Auth] MFA setup failed - User not found: UserId={UserId}, IP: {ClientIP}, Duration: {Duration}ms",
                request.UserId, clientIp, stopwatch.ElapsedMilliseconds);
            return NotFound(new { message = "User not found" });
        }

        _logger.LogDebug("[Auth] Verifying password for MFA setup: {Login} (ID: {UserId})", user.Login, user.Id);

        // Verify password again for security
        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            stopwatch.Stop();
            _logger.LogWarning("[Auth] MFA setup failed - Invalid password for user: {Login} (ID: {UserId}), IP: {ClientIP}, Duration: {Duration}ms",
                user.Login, user.Id, clientIp, stopwatch.ElapsedMilliseconds);
            return Unauthorized(new { message = "Invalid credentials" });
        }

        // Generate TOTP secret
        var mfaSecret = GenerateMfaSecret();
        _logger.LogDebug("[Auth] Generated MFA secret for user: {Login} (ID: {UserId})", user.Login, user.Id);

        user.MfaSecret = mfaSecret;
        user.MfaEnabled = false; // Will be enabled after first code verification
        user.IsFirstLogin = false; // Clear first login flag

        // Save PublicKey if provided
        if (!string.IsNullOrEmpty(request.PublicKey))
        {
            user.PublicKey = request.PublicKey;
            _logger.LogDebug("[Auth] Public key saved for user: {Login} (ID: {UserId})", user.Login, user.Id);
        }

        await _context.SaveChangesAsync();

        stopwatch.Stop();
        _logger.LogInformation("[Auth] MFA setup completed for user: {Login} (ID: {UserId}), IP: {ClientIP}, Duration: {Duration}ms",
            user.Login, user.Id, clientIp, stopwatch.ElapsedMilliseconds);

        // Return secret for QR code generation on client
        return Ok(new
        {
            mfaSecret,
            qrCodeUrl = _totpService.GenerateQrCodeUri(mfaSecret, user.Login),
            message = "Scan QR code with your authenticator app"
        });
    }

    [HttpPost("verify-mfa")]
    public async Task<IActionResult> VerifyMfa([FromBody] VerifyMfaRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        _logger.LogInformation("[Auth] MFA verification attempt for UserId: {UserId}, IP: {ClientIP}",
            request.UserId, clientIp);

        var user = await _context.Users.FindAsync(request.UserId);

        if (user == null)
        {
            stopwatch.Stop();
            _logger.LogWarning("[Auth] MFA verification failed - User not found: UserId={UserId}, IP: {ClientIP}, Duration: {Duration}ms",
                request.UserId, clientIp, stopwatch.ElapsedMilliseconds);
            return NotFound(new { message = "User not found" });
        }

        if (string.IsNullOrEmpty(user.MfaSecret))
        {
            stopwatch.Stop();
            _logger.LogWarning("[Auth] MFA verification failed - MFA not set up for user: {Login} (ID: {UserId}), IP: {ClientIP}, Duration: {Duration}ms",
                user.Login, user.Id, clientIp, stopwatch.ElapsedMilliseconds);
            return BadRequest(new { message = "MFA not set up for this user" });
        }

        _logger.LogDebug("[Auth] Verifying TOTP code for user: {Login} (ID: {UserId})", user.Login, user.Id);

        // Verify TOTP code
        var isValidCode = VerifyTotpCode(user.MfaSecret, request.TotpCode);

        if (!isValidCode)
        {
            stopwatch.Stop();
            _logger.LogWarning("[Auth] MFA verification failed - Invalid TOTP code for user: {Login} (ID: {UserId}), IP: {ClientIP}, Duration: {Duration}ms",
                user.Login, user.Id, clientIp, stopwatch.ElapsedMilliseconds);
            return Unauthorized(new { message = "Invalid MFA code" });
        }

        // If first verification, enable MFA
        if (!user.MfaEnabled)
        {
            user.MfaEnabled = true;
            await _context.SaveChangesAsync();
            _logger.LogInformation("[Auth] MFA enabled for user: {Login} (ID: {UserId})", user.Login, user.Id);
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        _logger.LogDebug("[Auth] Updated last login timestamp for user: {Login} (ID: {UserId})", user.Login, user.Id);

        var token = GenerateJwtToken(user);

        stopwatch.Stop();
        _logger.LogInformation("[Auth] MFA verification successful for user: {Login} (ID: {UserId}), Role: {Role}, IP: {ClientIP}, Duration: {Duration}ms",
            user.Login, user.Id, user.Role, clientIp, stopwatch.ElapsedMilliseconds);

        return Ok(new
        {
            token,
            user = new
            {
                user.Id,
                user.Login,
                role = user.Role.ToString(),
                isFirstLogin = user.IsFirstLogin,
                mfaEnabled = user.MfaEnabled
            },
            message = "MFA verification successful"
        });
    }

    private string GenerateMfaSecret()
    {
        _logger.LogDebug("[Auth] Generating new MFA secret");
        return _totpService.GenerateSecret();
    }

    private bool VerifyTotpCode(string secret, string code)
    {
        _logger.LogDebug("[Auth] Verifying TOTP code");
        return _totpService.VerifyCode(secret, code);
    }

    private string GenerateJwtToken(User user)
    {
        _logger.LogDebug("[Auth] Generating JWT token for user: {Login} (ID: {UserId})", user.Login, user.Id);

        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["Secret"]!;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expiryMinutes = Convert.ToDouble(jwtSettings["ExpiryMinutes"]);
        var expiry = DateTime.UtcNow.AddMinutes(expiryMinutes);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Login),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: expiry,
            signingCredentials: credentials
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        _logger.LogDebug("[Auth] JWT token generated for user: {Login} (ID: {UserId}), Expires: {Expiry}, ExpiryMinutes: {ExpiryMinutes}",
            user.Login, user.Id, expiry, expiryMinutes);

        return tokenString;
    }
}

public record LoginRequest(string Login, string Password);
public record RegisterRequest(string Login, string Password, Core.Enums.UserRole Role);

// ИЗМЕНЕНО: Новые request DTO для MFA
public record SetupMfaRequest(
    int UserId,
    string Password,
    string? PublicKey
);

public record VerifyMfaRequest(
    int UserId,
    string TotpCode
);
