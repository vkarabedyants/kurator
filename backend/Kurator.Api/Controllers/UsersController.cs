using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Kurator.Core.Entities;
using Kurator.Core.Enums;
using Kurator.Core.Interfaces;
using Kurator.Infrastructure.Data;
using System.Security.Claims;

namespace Kurator.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        ApplicationDbContext context,
        IPasswordHasher passwordHasher,
        ILogger<UsersController> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    private int GetUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
    private string GetUserLogin() => User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        // ИЗМЕНЕНО: Убрали PrimaryBlocks/BackupBlocks, используем BlockAssignments через BlockCurator table
        var users = await _context.Users
            .Include(u => u.BlockAssignments)
                .ThenInclude(ba => ba.Block)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Login = u.Login,
                Role = u.Role.ToString(),
                CreatedAt = u.CreatedAt,
                LastLoginAt = u.LastLoginAt,
                IsActive = u.IsActive,
                IsFirstLogin = u.IsFirstLogin,
                MfaEnabled = u.MfaEnabled,
                // ИЗМЕНЕНО: Получаем блоки через BlockAssignments с разделением по CuratorType
                PrimaryBlockIds = u.BlockAssignments
                    .Where(ba => ba.CuratorType == CuratorType.Primary)
                    .Select(ba => ba.BlockId)
                    .ToList(),
                PrimaryBlockNames = u.BlockAssignments
                    .Where(ba => ba.CuratorType == CuratorType.Primary)
                    .Select(ba => ba.Block.Name)
                    .ToList(),
                BackupBlockIds = u.BlockAssignments
                    .Where(ba => ba.CuratorType == CuratorType.Backup)
                    .Select(ba => ba.BlockId)
                    .ToList(),
                BackupBlockNames = u.BlockAssignments
                    .Where(ba => ba.CuratorType == CuratorType.Backup)
                    .Select(ba => ba.Block.Name)
                    .ToList()
            })
            .ToListAsync();

        return Ok(users);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        // ИЗМЕНЕНО: Используем BlockAssignments вместо PrimaryBlocks/BackupBlocks
        var user = await _context.Users
            .Include(u => u.BlockAssignments)
                .ThenInclude(ba => ba.Block)
            .Where(u => u.Id == id)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Login = u.Login,
                Role = u.Role.ToString(),
                CreatedAt = u.CreatedAt,
                LastLoginAt = u.LastLoginAt,
                IsActive = u.IsActive,
                IsFirstLogin = u.IsFirstLogin,
                MfaEnabled = u.MfaEnabled,
                PrimaryBlockIds = u.BlockAssignments
                    .Where(ba => ba.CuratorType == CuratorType.Primary)
                    .Select(ba => ba.BlockId)
                    .ToList(),
                PrimaryBlockNames = u.BlockAssignments
                    .Where(ba => ba.CuratorType == CuratorType.Primary)
                    .Select(ba => ba.Block.Name)
                    .ToList(),
                BackupBlockIds = u.BlockAssignments
                    .Where(ba => ba.CuratorType == CuratorType.Backup)
                    .Select(ba => ba.BlockId)
                    .ToList(),
                BackupBlockNames = u.BlockAssignments
                    .Where(ba => ba.CuratorType == CuratorType.Backup)
                    .Select(ba => ba.Block.Name)
                    .ToList()
            })
            .FirstOrDefaultAsync();

        if (user == null)
            return NotFound();

        return Ok(user);
    }

    [HttpGet("curators")]
    public async Task<IActionResult> GetCurators()
    {
        // ИЗМЕНЕНО: Убрали роль BackupCurator из валидации (теперь есть только Curator роль)
        var curators = await _context.Users
            .Where(u => u.Role == UserRole.Curator && u.IsActive)
            .Select(u => new
            {
                u.Id,
                u.Login,
                Role = u.Role.ToString()
            })
            .ToListAsync();

        return Ok(curators);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
    {
        var adminUserId = GetUserId();
        var adminLogin = GetUserLogin();

        if (await _context.Users.AnyAsync(u => u.Login == request.Login))
        {
            return BadRequest(new { message = "User with this login already exists" });
        }

        var user = new User
        {
            Login = request.Login,
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            Role = request.Role,
            // ИЗМЕНЕНО: Добавлена поддержка IsFirstLogin (по умолчанию true)
            IsFirstLogin = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);

        // ИЗМЕНЕНО: ActionType → Action используем Create, NewValue → NewValuesJson
        var auditLog = new AuditLog
        {
            UserId = adminUserId,
            Action = AuditActionType.Create,
            EntityType = "User",
            EntityId = user.Id.ToString(),
            Timestamp = DateTime.UtcNow,
            NewValuesJson = $"Login: {user.Login}, Role: {user.Role}"
        };
        _context.AuditLogs.Add(auditLog);

        await _context.SaveChangesAsync();

        _logger.LogInformation("User created: {Login} with role {Role} by {Admin}", 
            user.Login, user.Role, adminLogin);

        return CreatedAtAction(nameof(GetById), new { id = user.Id }, new { id = user.Id, login = user.Login });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUserRequest request)
    {
        var adminUserId = GetUserId();
        var adminLogin = GetUserLogin();

        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return NotFound();

        var oldRole = user.Role;

        user.Role = request.Role;

        // Update password only if provided
        if (!string.IsNullOrEmpty(request.NewPassword))
        {
            user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        }

        // ИЗМЕНЕНО: ActionType → Action используем Update, OldValue → OldValuesJson, NewValue → NewValuesJson
        var auditLog = new AuditLog
        {
            UserId = adminUserId,
            Action = AuditActionType.Update,
            EntityType = "User",
            EntityId = user.Id.ToString(),
            Timestamp = DateTime.UtcNow,
            OldValuesJson = $"Role: {oldRole}",
            NewValuesJson = $"Role: {request.Role}"
        };
        _context.AuditLogs.Add(auditLog);

        await _context.SaveChangesAsync();

        _logger.LogInformation("User updated: {Login} by {Admin}", user.Login, adminLogin);

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var adminUserId = GetUserId();
        var adminLogin = GetUserLogin();

        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return NotFound();

        // Don't allow deleting yourself
        if (user.Id == adminUserId)
        {
            return BadRequest(new { message = "Cannot delete your own account" });
        }

        // Check if user is assigned to any blocks
        // ИЗМЕНЕНО: Используем BlockCurators table
        var blockCount = await _context.BlockCurators
            .CountAsync(bc => bc.UserId == id);

        if (blockCount > 0)
        {
            return BadRequest(new { message = "Cannot delete user assigned to blocks. Reassign blocks first." });
        }

        _context.Users.Remove(user);

        // ИЗМЕНЕНО: ActionType → Action используем Delete, OldValue → OldValuesJson
        var auditLog = new AuditLog
        {
            UserId = adminUserId,
            Action = AuditActionType.Delete,
            EntityType = "User",
            EntityId = user.Id.ToString(),
            Timestamp = DateTime.UtcNow,
            OldValuesJson = $"Login: {user.Login}, Role: {user.Role}"
        };
        _context.AuditLogs.Add(auditLog);

        await _context.SaveChangesAsync();

        _logger.LogInformation("User deleted: {Login} by {Admin}", user.Login, adminLogin);

        return NoContent();
    }

    [HttpPost("{id}/change-password")]
    public async Task<IActionResult> ChangePassword(int id, [FromBody] ChangePasswordRequest request)
    {
        var adminUserId = GetUserId();
        var adminLogin = GetUserLogin();

        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return NotFound();

        user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);

        // ИЗМЕНЕНО: ActionType → Action используем Update, NewValue → NewValuesJson
        var auditLog = new AuditLog
        {
            UserId = adminUserId,
            Action = AuditActionType.Update,
            EntityType = "User",
            EntityId = user.Id.ToString(),
            Timestamp = DateTime.UtcNow,
            NewValuesJson = "Password changed"
        };
        _context.AuditLogs.Add(auditLog);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Password changed for user {Login} by {Admin}", user.Login, adminLogin);

        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = GetUserId();
        // ИЗМЕНЕНО: Используем BlockAssignments
        var user = await _context.Users
            .Include(u => u.BlockAssignments)
                .ThenInclude(ba => ba.Block)
            .Where(u => u.Id == userId)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Login = u.Login,
                Role = u.Role.ToString(),
                CreatedAt = u.CreatedAt,
                LastLoginAt = u.LastLoginAt,
                IsActive = u.IsActive,
                IsFirstLogin = u.IsFirstLogin,
                MfaEnabled = u.MfaEnabled,
                PrimaryBlockIds = u.BlockAssignments
                    .Where(ba => ba.CuratorType == CuratorType.Primary)
                    .Select(ba => ba.BlockId)
                    .ToList(),
                PrimaryBlockNames = u.BlockAssignments
                    .Where(ba => ba.CuratorType == CuratorType.Primary)
                    .Select(ba => ba.Block.Name)
                    .ToList(),
                BackupBlockIds = u.BlockAssignments
                    .Where(ba => ba.CuratorType == CuratorType.Backup)
                    .Select(ba => ba.BlockId)
                    .ToList(),
                BackupBlockNames = u.BlockAssignments
                    .Where(ba => ba.CuratorType == CuratorType.Backup)
                    .Select(ba => ba.Block.Name)
                    .ToList()
            })
            .FirstOrDefaultAsync();

        if (user == null)
            return NotFound();

        return Ok(user);
    }

    [HttpPost("{id}/toggle-active")]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var adminUserId = GetUserId();
        var adminLogin = GetUserLogin();

        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return NotFound();

        // Don't allow deactivating yourself
        if (user.Id == adminUserId)
        {
            return BadRequest(new { message = "Cannot deactivate your own account" });
        }

        var oldStatus = user.IsActive;
        user.IsActive = !user.IsActive;

        // Create audit log
        var auditLog = new AuditLog
        {
            UserId = adminUserId,
            Action = AuditActionType.Update,
            EntityType = "User",
            EntityId = user.Id.ToString(),
            Timestamp = DateTime.UtcNow,
            OldValuesJson = $"IsActive: {oldStatus}",
            NewValuesJson = $"IsActive: {user.IsActive}"
        };
        _context.AuditLogs.Add(auditLog);

        await _context.SaveChangesAsync();

        _logger.LogInformation("User {Login} {Action} by {Admin}",
            user.Login, user.IsActive ? "activated" : "deactivated", adminLogin);

        return Ok(new { isActive = user.IsActive });
    }

    [HttpGet("statistics")]
    public async Task<IActionResult> GetUserStatistics()
    {
        var stats = await _context.Users
            .GroupBy(u => u.Role)
            .Select(g => new
            {
                Role = g.Key.ToString(),
                Count = g.Count()
            })
            .ToListAsync();

        var totalUsers = await _context.Users.CountAsync();
        var activeInLastMonth = await _context.Users
            .CountAsync(u => u.LastLoginAt.HasValue && u.LastLoginAt.Value >= DateTime.UtcNow.AddMonths(-1));

        return Ok(new
        {
            totalUsers,
            activeInLastMonth,
            byRole = stats
        });
    }
}

// DTOs
// ИЗМЕНЕНО: Добавлена поддержка IsActive, IsFirstLogin, MfaEnabled
public record UserDto
{
    public int Id { get; init; }
    public string Login { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? LastLoginAt { get; init; }
    public bool IsActive { get; init; }
    public bool IsFirstLogin { get; init; }
    public bool MfaEnabled { get; init; }
    public List<int> PrimaryBlockIds { get; init; } = new();
    public List<string> PrimaryBlockNames { get; init; } = new();
    public List<int> BackupBlockIds { get; init; } = new();
    public List<string> BackupBlockNames { get; init; } = new();
}

public record CreateUserRequest(
    string Login,
    string Password,
    UserRole Role
);

public record UpdateUserRequest(
    UserRole Role,
    string? NewPassword
);

public record ChangePasswordRequest(
    string NewPassword
);
