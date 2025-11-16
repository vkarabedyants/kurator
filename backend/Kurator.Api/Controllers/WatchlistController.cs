using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Kurator.Core.Entities;
using Kurator.Core.Enums;
using Kurator.Infrastructure.Data;
using System.Security.Claims;

namespace Kurator.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,ThreatAnalyst")]
public class WatchlistController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<WatchlistController> _logger;

    // ИЗМЕНЕНО: Убран IEncryptionService т.к. FullName НЕ шифруется
    public WatchlistController(
        ApplicationDbContext context,
        ILogger<WatchlistController> logger)
    {
        _context = context;
        _logger = logger;
    }

    private int GetUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] RiskLevel? riskLevel = null,
        [FromQuery] int? riskSphereId = null,
        [FromQuery] MonitoringFrequency? monitoringFrequency = null,
        [FromQuery] int? watchOwnerId = null,
        [FromQuery] bool? requiresCheck = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        // ИЗМЕНЕНО: Добавлен фильтр по IsActive
        var query = _context.Watchlists
            .Include(w => w.WatchOwner)
            .Where(w => w.IsActive)
            .AsQueryable();

        // Filters
        if (riskLevel.HasValue)
            query = query.Where(w => w.RiskLevel == riskLevel.Value);

        // ИЗМЕНЕНО: RiskSphereId теперь int?
        if (riskSphereId.HasValue)
            query = query.Where(w => w.RiskSphereId == riskSphereId.Value);

        if (monitoringFrequency.HasValue)
            query = query.Where(w => w.MonitoringFrequency == monitoringFrequency.Value);

        if (watchOwnerId.HasValue)
            query = query.Where(w => w.WatchOwnerId == watchOwnerId.Value);

        if (requiresCheck == true)
            query = query.Where(w => w.NextCheckDate.HasValue && w.NextCheckDate.Value <= DateTime.UtcNow);

        var total = await query.CountAsync();

        var watchlistItems = await query
            .OrderByDescending(w => w.RiskLevel)
            .ThenBy(w => w.NextCheckDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(w => new WatchlistDto
            {
                Id = w.Id,
                FullName = w.FullName,
                RoleStatus = w.RoleStatus,
                RiskSphereId = w.RiskSphereId,
                ThreatSource = w.ThreatSource,
                ConflictDate = w.ConflictDate,
                RiskLevel = w.RiskLevel.ToString(),
                MonitoringFrequency = w.MonitoringFrequency.ToString(),
                LastCheckDate = w.LastCheckDate,
                NextCheckDate = w.NextCheckDate,
                DynamicsDescription = w.DynamicsDescription,
                WatchOwnerId = w.WatchOwnerId,
                WatchOwnerLogin = w.WatchOwner != null ? w.WatchOwner.Login : null,
                AttachmentsJson = w.AttachmentsJson,
                CreatedAt = w.CreatedAt,
                UpdatedAt = w.UpdatedAt,
                UpdatedBy = w.UpdatedBy,
                RequiresCheck = w.NextCheckDate.HasValue && w.NextCheckDate.Value <= DateTime.UtcNow
            })
            .ToListAsync();

        return Ok(new
        {
            data = watchlistItems,
            page,
            pageSize,
            total,
            totalPages = (int)Math.Ceiling(total / (double)pageSize)
        });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var watchlistItem = await _context.Watchlists
            .Include(w => w.WatchOwner)
            .FirstOrDefaultAsync(w => w.Id == id);

        if (watchlistItem == null)
            return NotFound();

        // ИЗМЕНЕНО: FullName НЕ шифруется
        var result = new WatchlistDto
        {
            Id = watchlistItem.Id,
            FullName = watchlistItem.FullName,
            RoleStatus = watchlistItem.RoleStatus,
            RiskSphereId = watchlistItem.RiskSphereId,
            ThreatSource = watchlistItem.ThreatSource,
            ConflictDate = watchlistItem.ConflictDate,
            RiskLevel = watchlistItem.RiskLevel.ToString(),
            MonitoringFrequency = watchlistItem.MonitoringFrequency.ToString(),
            LastCheckDate = watchlistItem.LastCheckDate,
            NextCheckDate = watchlistItem.NextCheckDate,
            DynamicsDescription = watchlistItem.DynamicsDescription,
            WatchOwnerId = watchlistItem.WatchOwnerId,
            WatchOwnerLogin = watchlistItem.WatchOwner?.Login,
            AttachmentsJson = watchlistItem.AttachmentsJson,
            CreatedAt = watchlistItem.CreatedAt,
            UpdatedAt = watchlistItem.UpdatedAt,
            UpdatedBy = watchlistItem.UpdatedBy,
            RequiresCheck = watchlistItem.NextCheckDate.HasValue && watchlistItem.NextCheckDate.Value <= DateTime.UtcNow
        };

        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateWatchlistRequest request)
    {
        var userId = GetUserId();

        // ИЗМЕНЕНО: FullName НЕ шифруется, UpdatedBy - int
        var watchlistItem = new Watchlist
        {
            FullName = request.FullName,
            RoleStatus = request.RoleStatus,
            RiskSphereId = request.RiskSphereId,
            ThreatSource = request.ThreatSource,
            ConflictDate = request.ConflictDate,
            RiskLevel = request.RiskLevel,
            MonitoringFrequency = request.MonitoringFrequency,
            LastCheckDate = request.LastCheckDate,
            NextCheckDate = request.NextCheckDate,
            DynamicsDescription = request.DynamicsDescription,
            WatchOwnerId = request.WatchOwnerId ?? userId,
            AttachmentsJson = request.AttachmentsJson,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = userId
        };

        _context.Watchlists.Add(watchlistItem);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Watchlist item created by user {UserId}", userId);

        return CreatedAtAction(nameof(GetById), new { id = watchlistItem.Id }, new { id = watchlistItem.Id });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateWatchlistRequest request)
    {
        var userId = GetUserId();
        var watchlistItem = await _context.Watchlists.FindAsync(id);

        if (watchlistItem == null)
            return NotFound();

        // ИЗМЕНЕНО: UpdatedBy - int
        watchlistItem.RoleStatus = request.RoleStatus;
        watchlistItem.RiskSphereId = request.RiskSphereId;
        watchlistItem.ThreatSource = request.ThreatSource;
        watchlistItem.ConflictDate = request.ConflictDate;
        watchlistItem.RiskLevel = request.RiskLevel;
        watchlistItem.MonitoringFrequency = request.MonitoringFrequency;
        watchlistItem.LastCheckDate = request.LastCheckDate;
        watchlistItem.NextCheckDate = request.NextCheckDate;
        watchlistItem.DynamicsDescription = request.DynamicsDescription;
        watchlistItem.WatchOwnerId = request.WatchOwnerId;
        watchlistItem.AttachmentsJson = request.AttachmentsJson;
        watchlistItem.UpdatedAt = DateTime.UtcNow;
        watchlistItem.UpdatedBy = userId;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Watchlist item {Id} updated by user {UserId}", id, userId);

        return NoContent();
    }

    [HttpPost("{id}/check")]
    public async Task<IActionResult> RecordCheck(int id, [FromBody] RecordCheckRequest request)
    {
        var userId = GetUserId();
        var watchlistItem = await _context.Watchlists.FindAsync(id);

        if (watchlistItem == null)
            return NotFound();

        watchlistItem.LastCheckDate = DateTime.UtcNow;
        watchlistItem.NextCheckDate = request.NextCheckDate;

        if (!string.IsNullOrEmpty(request.DynamicsUpdate))
        {
            watchlistItem.DynamicsDescription = request.DynamicsUpdate;
        }

        if (request.NewRiskLevel.HasValue)
        {
            watchlistItem.RiskLevel = request.NewRiskLevel.Value;
        }

        watchlistItem.UpdatedAt = DateTime.UtcNow;
        watchlistItem.UpdatedBy = userId;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Check recorded for watchlist item {Id} by user {UserId}", id, userId);

        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        // ИЗМЕНЕНО: Soft delete через IsActive
        var userId = GetUserId();
        var watchlistItem = await _context.Watchlists.FindAsync(id);

        if (watchlistItem == null)
            return NotFound();

        watchlistItem.IsActive = false;
        watchlistItem.UpdatedAt = DateTime.UtcNow;
        watchlistItem.UpdatedBy = userId;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Watchlist item {Id} deactivated by user {UserId}", id, userId);

        return NoContent();
    }

    [HttpGet("requiring-check")]
    public async Task<IActionResult> GetRequiringCheck()
    {
        // ИЗМЕНЕНО: Фильтр по IsActive, FullName НЕ шифруется
        var items = await _context.Watchlists
            .Include(w => w.WatchOwner)
            .Where(w => w.IsActive && w.NextCheckDate.HasValue && w.NextCheckDate.Value <= DateTime.UtcNow)
            .OrderBy(w => w.NextCheckDate)
            .ThenByDescending(w => w.RiskLevel)
            .Select(w => new WatchlistDto
            {
                Id = w.Id,
                FullName = w.FullName,
                RoleStatus = w.RoleStatus,
                RiskSphereId = w.RiskSphereId,
                RiskLevel = w.RiskLevel.ToString(),
                LastCheckDate = w.LastCheckDate,
                NextCheckDate = w.NextCheckDate,
                WatchOwnerLogin = w.WatchOwner != null ? w.WatchOwner.Login : null,
                RequiresCheck = true
            })
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics()
    {
        // ИЗМЕНЕНО: Фильтр по IsActive
        var total = await _context.Watchlists.CountAsync(w => w.IsActive);
        var requiresCheck = await _context.Watchlists
            .CountAsync(w => w.IsActive && w.NextCheckDate.HasValue && w.NextCheckDate.Value <= DateTime.UtcNow);

        var byRiskLevel = await _context.Watchlists
            .Where(w => w.IsActive)
            .GroupBy(w => w.RiskLevel)
            .Select(g => new { RiskLevel = g.Key.ToString(), Count = g.Count() })
            .ToDictionaryAsync(x => x.RiskLevel, x => x.Count);

        var byRiskSphere = await _context.Watchlists
            .Where(w => w.IsActive)
            .GroupBy(w => w.RiskSphereId)
            .Select(g => new { RiskSphere = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.RiskSphere, x => x.Count);

        var byMonitoringFrequency = await _context.Watchlists
            .Where(w => w.IsActive)
            .GroupBy(w => w.MonitoringFrequency)
            .Select(g => new { Frequency = g.Key.ToString(), Count = g.Count() })
            .ToDictionaryAsync(x => x.Frequency, x => x.Count);

        return Ok(new
        {
            total,
            requiresCheck,
            byRiskLevel,
            byRiskSphere,
            byMonitoringFrequency
        });
    }
}

// DTOs
// ИЗМЕНЕНО: RiskSphereId теперь int?, UpdatedBy - int, AttachedMaterials → AttachmentsJson
public record WatchlistDto
{
    public int Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string? RoleStatus { get; init; }
    public int? RiskSphereId { get; init; }
    public string? ThreatSource { get; init; }
    public DateTime? ConflictDate { get; init; }
    public string RiskLevel { get; init; } = string.Empty;
    public string MonitoringFrequency { get; init; } = string.Empty;
    public DateTime? LastCheckDate { get; init; }
    public DateTime? NextCheckDate { get; init; }
    public string? DynamicsDescription { get; init; }
    public int? WatchOwnerId { get; init; }
    public string? WatchOwnerLogin { get; init; }
    public string? AttachmentsJson { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public int UpdatedBy { get; init; }
    public bool RequiresCheck { get; init; }
}

public record CreateWatchlistRequest(
    string FullName,
    string? RoleStatus,
    int? RiskSphereId,
    string? ThreatSource,
    DateTime? ConflictDate,
    RiskLevel RiskLevel,
    MonitoringFrequency MonitoringFrequency,
    DateTime? LastCheckDate,
    DateTime? NextCheckDate,
    string? DynamicsDescription,
    int? WatchOwnerId,
    string? AttachmentsJson
);

public record UpdateWatchlistRequest(
    string? RoleStatus,
    int? RiskSphereId,
    string? ThreatSource,
    DateTime? ConflictDate,
    RiskLevel RiskLevel,
    MonitoringFrequency MonitoringFrequency,
    DateTime? LastCheckDate,
    DateTime? NextCheckDate,
    string? DynamicsDescription,
    int? WatchOwnerId,
    string? AttachmentsJson
);

public record RecordCheckRequest(
    DateTime? NextCheckDate,
    string? DynamicsUpdate,
    RiskLevel? NewRiskLevel
);
