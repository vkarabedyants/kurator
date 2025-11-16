using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Kurator.Core.Entities;
using Kurator.Core.Interfaces;
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

    public AuthController(
        ApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IConfiguration configuration,
        ILogger<AuthController> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Login == request.Login);

        if (user == null || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            return Unauthorized(new { message = "Invalid credentials" });
        }

        // ИЗМЕНЕНО: Проверяем IsFirstLogin - если первый вход, требуем настройку MFA
        if (user.IsFirstLogin)
        {
            return Ok(new
            {
                requireMfaSetup = true,
                userId = user.Id,
                login = user.Login,
                message = "First login detected. Please set up MFA."
            });
        }

        // ИЗМЕНЕНО: Проверяем MfaEnabled - если включен MFA, требуем TOTP код
        if (user.MfaEnabled)
        {
            // Для MFA потребуется дополнительный шаг верификации
            return Ok(new
            {
                requireMfaVerification = true,
                userId = user.Id,
                message = "MFA verification required"
            });
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var token = GenerateJwtToken(user);

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

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (await _context.Users.AnyAsync(u => u.Login == request.Login))
        {
            return BadRequest(new { message = "User already exists" });
        }

        var user = new User
        {
            Login = request.Login,
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            Role = request.Role,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation("New user registered: {Login}", user.Login);

        return Ok(new { message = "User registered successfully" });
    }

    // ИЗМЕНЕНО: Новый endpoint для настройки MFA при первом входе
    [HttpPost("setup-mfa")]
    public async Task<IActionResult> SetupMfa([FromBody] SetupMfaRequest request)
    {
        var user = await _context.Users.FindAsync(request.UserId);

        if (user == null)
            return NotFound(new { message = "User not found" });

        // Проверяем пароль еще раз для безопасности
        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            return Unauthorized(new { message = "Invalid credentials" });
        }

        // Генерируем TOTP секрет (в реальной системе использовать библиотеку типа OtpNet)
        var mfaSecret = GenerateMfaSecret();

        user.MfaSecret = mfaSecret;
        user.MfaEnabled = false; // Включится после верификации первого кода
        user.IsFirstLogin = false; // Снимаем флаг первого входа

        // Если указан PublicKey, сохраняем его
        if (!string.IsNullOrEmpty(request.PublicKey))
        {
            user.PublicKey = request.PublicKey;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("MFA setup initiated for user {Login}", user.Login);

        // Возвращаем секрет для генерации QR кода на клиенте
        return Ok(new
        {
            mfaSecret,
            qrCodeUrl = $"otpauth://totp/Kurator:{user.Login}?secret={mfaSecret}&issuer=Kurator",
            message = "Scan QR code with your authenticator app"
        });
    }

    // ИЗМЕНЕНО: Новый endpoint для верификации TOTP кода
    [HttpPost("verify-mfa")]
    public async Task<IActionResult> VerifyMfa([FromBody] VerifyMfaRequest request)
    {
        var user = await _context.Users.FindAsync(request.UserId);

        if (user == null)
            return NotFound(new { message = "User not found" });

        if (string.IsNullOrEmpty(user.MfaSecret))
            return BadRequest(new { message = "MFA not set up for this user" });

        // Проверяем TOTP код (в реальной системе использовать библиотеку типа OtpNet)
        var isValidCode = VerifyTotpCode(user.MfaSecret, request.TotpCode);

        if (!isValidCode)
        {
            return Unauthorized(new { message = "Invalid MFA code" });
        }

        // Если это первая верификация, включаем MFA
        if (!user.MfaEnabled)
        {
            user.MfaEnabled = true;
            await _context.SaveChangesAsync();
            _logger.LogInformation("MFA enabled for user {Login}", user.Login);
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var token = GenerateJwtToken(user);

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

    // Вспомогательный метод для генерации MFA секрета (заглушка)
    private string GenerateMfaSecret()
    {
        // В реальной системе использовать криптографически стойкий генератор
        // Например: KeyGeneration.GenerateRandomKey(20) из OtpNet
        var random = new Random();
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        return new string(Enumerable.Repeat(chars, 32)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    // Вспомогательный метод для проверки TOTP кода (заглушка)
    private bool VerifyTotpCode(string secret, string code)
    {
        // В реальной системе использовать библиотеку OtpNet:
        // var totp = new Totp(Base32Encoding.ToBytes(secret));
        // return totp.VerifyTotp(code, out _, new VerificationWindow(2, 2));

        // Временная заглушка для тестирования - принимаем любой 6-значный код
        return code.Length == 6 && code.All(char.IsDigit);
    }

    private string GenerateJwtToken(User user)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["Secret"]!;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

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
            expires: DateTime.UtcNow.AddMinutes(Convert.ToDouble(jwtSettings["ExpiryMinutes"])),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
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
