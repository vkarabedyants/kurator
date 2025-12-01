using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Kurator.Core.Enums;
using Kurator.Core.Interfaces;
using Kurator.Infrastructure.Data;
using System.Diagnostics;
using System.Security.Claims;

namespace Kurator.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        ApplicationDbContext context,
        IEncryptionService encryptionService,
        ILogger<DashboardController> logger)
    {
        _context = context;
        _encryptionService = encryptionService;
        _logger = logger;
        _logger.LogDebug("[Dashboard] DashboardController instantiated");
    }

    private int GetUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
    private string GetUserLogin() => User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
    private bool IsAdmin() => User.IsInRole("Admin");

    [HttpGet("curator")]
    [Authorize(Roles = "Admin,Curator")]
    public async Task<IActionResult> GetCuratorDashboard()
    {
        var stopwatch = Stopwatch.StartNew();
        var userId = GetUserId();
        var userLogin = GetUserLogin();
        var isAdmin = IsAdmin();

        _logger.LogInformation("[Dashboard] GetCuratorDashboard started. User: {UserLogin} (ID: {UserId}), IsAdmin: {IsAdmin}",
            userLogin, userId, isAdmin);

        // Get user's blocks
        // ИЗМЕНЕНО: Используем BlockCurators table вместо PrimaryCuratorId/BackupCuratorId
        var userBlockIds = await _context.BlockCurators
            .Where(bc => bc.UserId == userId)
            .Select(bc => bc.BlockId)
            .Distinct()
            .ToListAsync();

        if (userBlockIds.Count == 0 && !isAdmin)
        {
            return Ok(new CuratorDashboardDto
            {
                TotalContacts = 0,
                InteractionsLastMonth = 0,
                AverageInteractionInterval = 0,
                OverdueContacts = 0,
                RecentInteractions = new List<RecentInteractionDto>(),
                ContactsRequiringAttention = new List<AttentionContactDto>(),
                ContactsByInfluenceStatus = new Dictionary<string, int>(),
                InteractionsByType = new Dictionary<string, int>()
            });
        }

        // Total contacts in curator's blocks (only active blocks)
        var totalContacts = await _context.Contacts
            .Include(c => c.Block)
            .CountAsync(c => userBlockIds.Contains(c.BlockId) && c.IsActive && c.Block.Status == BlockStatus.Active);

        // Interactions in last month
        var lastMonth = DateTime.UtcNow.AddMonths(-1);
        var interactionsLastMonth = await _context.Interactions
            .Include(i => i.Contact)
                .ThenInclude(c => c.Block)
            .CountAsync(i => userBlockIds.Contains(i.Contact.BlockId) && i.IsActive && i.Contact.Block.Status == BlockStatus.Active && i.InteractionDate >= lastMonth);

        // Average interval between interactions (days)
        var activeContacts = await _context.Contacts
            .Include(c => c.Block)
            .Where(c => userBlockIds.Contains(c.BlockId) && c.IsActive && c.Block.Status == BlockStatus.Active && c.LastInteractionDate.HasValue)
            .Select(c => new
            {
                c.LastInteractionDate,
                c.CreatedAt
            })
            .ToListAsync();

        double averageInterval = 0;
        if (activeContacts.Count > 0)
        {
            var intervals = activeContacts
                .Select(c => (DateTime.UtcNow - c.LastInteractionDate!.Value).TotalDays)
                .ToList();
            averageInterval = intervals.Average();
        }

        // Overdue contacts
        var overdueContacts = await _context.Contacts
            .Include(c => c.Block)
            .CountAsync(c => userBlockIds.Contains(c.BlockId) &&
                            c.IsActive &&
                            c.Block.Status == BlockStatus.Active &&
                            c.NextTouchDate.HasValue &&
                            c.NextTouchDate.Value < DateTime.UtcNow);

        // Recent 5 interactions
        var recentInteractions = await _context.Interactions
            .Include(i => i.Contact)
                .ThenInclude(c => c.Block)
            .Where(i => userBlockIds.Contains(i.Contact.BlockId) &&
                       i.IsActive &&
                       i.Contact.Block.Status == BlockStatus.Active)
            .OrderByDescending(i => i.InteractionDate)
            .Take(5)
            .Select(i => new RecentInteractionDto
            {
                Id = i.Id,
                ContactName = _encryptionService.Decrypt(i.Contact.FullNameEncrypted),
                ContactId = i.Contact.ContactId,
                InteractionDate = i.InteractionDate,
                InteractionTypeId = i.InteractionTypeId,
                ResultId = i.ResultId
            })
            .ToListAsync();

        // Contacts requiring attention (overdue next touch date)
        var attentionContacts = await _context.Contacts
            .Include(c => c.Block)
            .Where(c => userBlockIds.Contains(c.BlockId) &&
                       c.IsActive &&
                       c.Block.Status == BlockStatus.Active &&
                       c.NextTouchDate.HasValue &&
                       c.NextTouchDate.Value < DateTime.UtcNow)
            .OrderBy(c => c.NextTouchDate)
            .Take(10)
            .Select(c => new AttentionContactDto
            {
                Id = c.Id,
                ContactId = c.ContactId,
                FullName = _encryptionService.Decrypt(c.FullNameEncrypted),
                NextTouchDate = c.NextTouchDate,
                DaysOverdue = (int)(DateTime.UtcNow - c.NextTouchDate!.Value).TotalDays,
                // ИЗМЕНЕНО: InfluenceStatus теперь InfluenceStatusId (int?)
                InfluenceStatus = c.InfluenceStatusId.HasValue ? c.InfluenceStatusId.Value.ToString() : "Unknown"
            })
            .ToListAsync();

        // Contacts by influence status
        // ИЗМЕНЕНО: InfluenceStatus теперь InfluenceStatusId (int?), фильтруем null и преобразуем в string
        var contactsByStatus = await _context.Contacts
            .Where(c => userBlockIds.Contains(c.BlockId) && c.InfluenceStatusId.HasValue)
            .GroupBy(c => c.InfluenceStatusId!.Value)
            .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count);

        // Interactions by type (last month)
        // ИЗМЕНЕНО: Фильтруем null значения InteractionTypeId и преобразуем в string для Dictionary
        var interactionsByType = await _context.Interactions
            .Include(i => i.Contact)
            .Where(i => userBlockIds.Contains(i.Contact.BlockId) && i.InteractionDate >= lastMonth && i.InteractionTypeId.HasValue)
            .GroupBy(i => i.InteractionTypeId!.Value)
            .Select(g => new { Type = g.Key.ToString(), Count = g.Count() })
            .ToDictionaryAsync(x => x.Type, x => x.Count);

        var dashboard = new CuratorDashboardDto
        {
            TotalContacts = totalContacts,
            InteractionsLastMonth = interactionsLastMonth,
            AverageInteractionInterval = Math.Round(averageInterval, 1),
            OverdueContacts = overdueContacts,
            RecentInteractions = recentInteractions,
            ContactsRequiringAttention = attentionContacts,
            ContactsByInfluenceStatus = contactsByStatus,
            InteractionsByType = interactionsByType
        };

        stopwatch.Stop();
        _logger.LogInformation("[Dashboard] GetCuratorDashboard completed. User: {UserLogin} (ID: {UserId}), TotalContacts: {TotalContacts}, OverdueContacts: {OverdueContacts}, Duration: {Duration}ms",
            userLogin, userId, totalContacts, overdueContacts, stopwatch.ElapsedMilliseconds);

        return Ok(dashboard);
    }

    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAdminDashboard()
    {
        var stopwatch = Stopwatch.StartNew();
        var userId = GetUserId();
        var userLogin = GetUserLogin();

        _logger.LogInformation("[Dashboard] GetAdminDashboard started. User: {UserLogin} (ID: {UserId})",
            userLogin, userId);

        var totalContacts = await _context.Contacts
            .Include(c => c.Block)
            .CountAsync(c => c.IsActive && c.Block.Status == BlockStatus.Active);
        var totalInteractions = await _context.Interactions
            .Include(i => i.Contact)
                .ThenInclude(c => c.Block)
            .CountAsync(i => i.IsActive && i.Contact.Block.Status == BlockStatus.Active);
        var totalBlocks = await _context.Blocks.CountAsync(b => b.Status == BlockStatus.Active);
        var totalUsers = await _context.Users.CountAsync();

        var lastMonth = DateTime.UtcNow.AddMonths(-1);
        var newContactsLastMonth = await _context.Contacts
            .Include(c => c.Block)
            .CountAsync(c => c.IsActive && c.Block.Status == BlockStatus.Active && c.CreatedAt >= lastMonth);
        var interactionsLastMonth = await _context.Interactions
            .Include(i => i.Contact)
                .ThenInclude(c => c.Block)
            .CountAsync(i => i.IsActive && i.Contact.Block.Status == BlockStatus.Active && i.InteractionDate >= lastMonth);

        // Contacts by block
        var contactsByBlock = await _context.Contacts
            .Include(c => c.Block)
            .Where(c => c.IsActive && c.Block.Status == BlockStatus.Active)
            .GroupBy(c => c.Block.Name)
            .Select(g => new { Block = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToDictionaryAsync(x => x.Block, x => x.Count);

        // Contacts by influence status (all)
        // ИЗМЕНЕНО: InfluenceStatusId (int?), фильтруем null
        var contactsByStatus = await _context.Contacts
            .Include(c => c.Block)
            .Where(c => c.IsActive && c.Block.Status == BlockStatus.Active && c.InfluenceStatusId.HasValue)
            .GroupBy(c => c.InfluenceStatusId!.Value)
            .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count);

        // Contacts by influence type
        // ИЗМЕНЕНО: InfluenceTypeId (int?), фильтруем null
        var contactsByType = await _context.Contacts
            .Include(c => c.Block)
            .Where(c => c.IsActive && c.Block.Status == BlockStatus.Active && c.InfluenceTypeId.HasValue)
            .GroupBy(c => c.InfluenceTypeId!.Value)
            .Select(g => new { Type = g.Key.ToString(), Count = g.Count() })
            .ToDictionaryAsync(x => x.Type, x => x.Count);

        // Interactions by block (last month)
        var interactionsByBlock = await _context.Interactions
            .Include(i => i.Contact)
                .ThenInclude(c => c.Block)
            .Where(i => i.IsActive && i.Contact.Block.Status == BlockStatus.Active && i.InteractionDate >= lastMonth)
            .GroupBy(i => i.Contact.Block.Name)
            .Select(g => new { Block = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToDictionaryAsync(x => x.Block, x => x.Count);

        // Top curators by activity (last month)
        var topCurators = await _context.Interactions
            .Include(i => i.Curator)
            .Include(i => i.Contact)
                .ThenInclude(c => c.Block)
            .Where(i => i.IsActive && i.Contact.Block.Status == BlockStatus.Active && i.InteractionDate >= lastMonth)
            .GroupBy(i => i.Curator.Login)
            .Select(g => new { Curator = g.Key, InteractionCount = g.Count() })
            .OrderByDescending(x => x.InteractionCount)
            .Take(5)
            .ToDictionaryAsync(x => x.Curator, x => x.InteractionCount);

        // Status change dynamics (last 3 months)
        var threeMonthsAgo = DateTime.UtcNow.AddMonths(-3);
        var statusChanges = await _context.InfluenceStatusHistories
            .Where(h => h.ChangedAt >= threeMonthsAgo)
            .GroupBy(h => new { h.PreviousStatus, h.NewStatus })
            .Select(g => new { 
                Transition = g.Key.PreviousStatus + "→" + g.Key.NewStatus, 
                Count = g.Count() 
            })
            .OrderByDescending(x => x.Count)
            .Take(10)
            .ToDictionaryAsync(x => x.Transition, x => x.Count);

        // Recent audit activity (last 20 entries)
        var recentAuditLogs = await _context.AuditLogs
            .Include(a => a.User)
            .OrderByDescending(a => a.Timestamp)
            .Take(20)
            .Select(a => new AuditLogSummaryDto
            {
                Id = (int)a.Id,
                UserLogin = a.User.Login,
                // ИЗМЕНЕНО: ActionType → Action
                ActionType = a.Action.ToString(),
                EntityType = a.EntityType,
                Timestamp = a.Timestamp
            })
            .ToListAsync();

        var dashboard = new AdminDashboardDto
        {
            TotalContacts = totalContacts,
            TotalInteractions = totalInteractions,
            TotalBlocks = totalBlocks,
            TotalUsers = totalUsers,
            NewContactsLastMonth = newContactsLastMonth,
            InteractionsLastMonth = interactionsLastMonth,
            ContactsByBlock = contactsByBlock,
            ContactsByInfluenceStatus = contactsByStatus,
            ContactsByInfluenceType = contactsByType,
            InteractionsByBlock = interactionsByBlock,
            TopCuratorsByActivity = topCurators,
            StatusChangeDynamics = statusChanges,
            RecentAuditLogs = recentAuditLogs
        };

        stopwatch.Stop();
        _logger.LogInformation("[Dashboard] GetAdminDashboard completed. User: {UserLogin} (ID: {UserId}), TotalContacts: {TotalContacts}, TotalBlocks: {TotalBlocks}, TotalUsers: {TotalUsers}, Duration: {Duration}ms",
            userLogin, userId, totalContacts, totalBlocks, totalUsers, stopwatch.ElapsedMilliseconds);

        return Ok(dashboard);
    }

    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int? blockId = null)
    {
        var userId = GetUserId();
        var isAdmin = IsAdmin();

        fromDate ??= DateTime.UtcNow.AddMonths(-1);
        toDate ??= DateTime.UtcNow;

        var query = _context.Interactions
            .Include(i => i.Contact)
            .Where(i => i.InteractionDate >= fromDate && i.InteractionDate <= toDate);

        // Access control
        // ИЗМЕНЕНО: Используем BlockCurators table
        if (!isAdmin)
        {
            var userBlockIds = await _context.BlockCurators
                .Where(bc => bc.UserId == userId)
                .Select(bc => bc.BlockId)
                .Distinct()
                .ToListAsync();

            query = query.Where(i => userBlockIds.Contains(i.Contact.BlockId));
        }

        if (blockId.HasValue)
        {
            query = query.Where(i => i.Contact.BlockId == blockId.Value);
        }

        var totalInteractions = await query.CountAsync();
        var uniqueContacts = await query.Select(i => i.ContactId).Distinct().CountAsync();

        // ИЗМЕНЕНО: InteractionTypeId и ResultId теперь int?, фильтруем null и преобразуем в string
        var byType = await query
            .Where(i => i.InteractionTypeId.HasValue)
            .GroupBy(i => i.InteractionTypeId!.Value)
            .Select(g => new { Type = g.Key.ToString(), Count = g.Count() })
            .ToDictionaryAsync(x => x.Type, x => x.Count);

        var byResult = await query
            .Where(i => i.ResultId.HasValue)
            .GroupBy(i => i.ResultId!.Value)
            .Select(g => new { Result = g.Key.ToString(), Count = g.Count() })
            .ToDictionaryAsync(x => x.Result, x => x.Count);

        return Ok(new
        {
            period = new { from = fromDate, to = toDate },
            totalInteractions,
            uniqueContacts,
            byType,
            byResult
        });
    }
}

// DTOs
public record CuratorDashboardDto
{
    public int TotalContacts { get; init; }
    public int InteractionsLastMonth { get; init; }
    public double AverageInteractionInterval { get; init; }
    public int OverdueContacts { get; init; }
    public List<RecentInteractionDto> RecentInteractions { get; init; } = new();
    public List<AttentionContactDto> ContactsRequiringAttention { get; init; } = new();
    public Dictionary<string, int> ContactsByInfluenceStatus { get; init; } = new();
    public Dictionary<string, int> InteractionsByType { get; init; } = new();
}

// ИЗМЕНЕНО: InteractionTypeId и ResultId теперь int?
public record RecentInteractionDto
{
    public int Id { get; init; }
    public string ContactName { get; init; } = string.Empty;
    public string ContactId { get; init; } = string.Empty;
    public DateTime InteractionDate { get; init; }
    public int? InteractionTypeId { get; init; }
    public int? ResultId { get; init; }
}

public record AttentionContactDto
{
    public int Id { get; init; }
    public string ContactId { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public DateTime? NextTouchDate { get; init; }
    public int DaysOverdue { get; init; }
    public string InfluenceStatus { get; init; } = string.Empty;
}

public record AdminDashboardDto
{
    public int TotalContacts { get; init; }
    public int TotalInteractions { get; init; }
    public int TotalBlocks { get; init; }
    public int TotalUsers { get; init; }
    public int NewContactsLastMonth { get; init; }
    public int InteractionsLastMonth { get; init; }
    public Dictionary<string, int> ContactsByBlock { get; init; } = new();
    public Dictionary<string, int> ContactsByInfluenceStatus { get; init; } = new();
    public Dictionary<string, int> ContactsByInfluenceType { get; init; } = new();
    public Dictionary<string, int> InteractionsByBlock { get; init; } = new();
    public Dictionary<string, int> TopCuratorsByActivity { get; init; } = new();
    public Dictionary<string, int> StatusChangeDynamics { get; init; } = new();
    public List<AuditLogSummaryDto> RecentAuditLogs { get; init; } = new();
}

public record AuditLogSummaryDto
{
    public int Id { get; init; }
    public string UserLogin { get; init; } = string.Empty;
    public string ActionType { get; init; } = string.Empty;
    public string EntityType { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
}
