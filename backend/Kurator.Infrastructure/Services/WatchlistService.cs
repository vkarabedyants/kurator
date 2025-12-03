using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Kurator.Core.Entities;
using Kurator.Core.Enums;
using Kurator.Core.Interfaces;
using Kurator.Infrastructure.Data;

namespace Kurator.Infrastructure.Services;

public class WatchlistService : IWatchlistService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<WatchlistService> _logger;

    public WatchlistService(
        ApplicationDbContext context,
        ILogger<WatchlistService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<(IEnumerable<Watchlist> Items, int Total)> GetWatchlistItemsAsync(
        RiskLevel? riskLevel = null,
        int? riskSphereId = null,
        MonitoringFrequency? monitoringFrequency = null,
        int? watchOwnerId = null,
        bool? requiresCheck = null,
        int page = 1,
        int pageSize = 50)
    {
        var query = _context.Watchlists
            .Include(w => w.WatchOwner)
            .Where(w => w.IsActive)
            .AsQueryable();

        // Apply filters
        if (riskLevel.HasValue)
            query = query.Where(w => w.RiskLevel == riskLevel.Value);

        if (riskSphereId.HasValue)
            query = query.Where(w => w.RiskSphereId == riskSphereId.Value);

        if (monitoringFrequency.HasValue)
            query = query.Where(w => w.MonitoringFrequency == monitoringFrequency.Value);

        if (watchOwnerId.HasValue)
            query = query.Where(w => w.WatchOwnerId == watchOwnerId.Value);

        if (requiresCheck == true)
            query = query.Where(w => w.NextCheckDate.HasValue && w.NextCheckDate.Value <= DateTime.UtcNow);

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(w => w.RiskLevel)
            .ThenBy(w => w.NextCheckDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        _logger.LogInformation(
            "Retrieved {Count} watchlist items (page {Page}, filters: riskLevel={RiskLevel}, requiresCheck={RequiresCheck})",
            items.Count, page, riskLevel, requiresCheck);

        return (items, total);
    }

    public async Task<Watchlist?> GetWatchlistItemByIdAsync(int id)
    {
        var item = await _context.Watchlists
            .Include(w => w.WatchOwner)
            .FirstOrDefaultAsync(w => w.Id == id);

        if (item == null)
        {
            _logger.LogWarning("Watchlist item {Id} not found", id);
        }

        return item;
    }

    public async Task<Watchlist> CreateWatchlistItemAsync(
        string fullName,
        int userId,
        string? roleStatus = null,
        int? riskSphereId = null,
        string? threatSource = null,
        DateTime? conflictDate = null,
        RiskLevel riskLevel = RiskLevel.Low,
        MonitoringFrequency monitoringFrequency = MonitoringFrequency.Monthly,
        DateTime? lastCheckDate = null,
        DateTime? nextCheckDate = null,
        string? dynamicsDescription = null,
        int? watchOwnerId = null,
        string? attachmentsJson = null)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Full name is required", nameof(fullName));

        var watchlistItem = new Watchlist
        {
            FullName = fullName,
            RoleStatus = roleStatus,
            RiskSphereId = riskSphereId,
            ThreatSource = threatSource,
            ConflictDate = conflictDate,
            RiskLevel = riskLevel,
            MonitoringFrequency = monitoringFrequency,
            LastCheckDate = lastCheckDate,
            NextCheckDate = nextCheckDate,
            DynamicsDescription = dynamicsDescription,
            WatchOwnerId = watchOwnerId ?? userId,
            AttachmentsJson = attachmentsJson,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = userId
        };

        _context.Watchlists.Add(watchlistItem);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Created watchlist item {Id} for '{FullName}' with risk level {RiskLevel} by user {UserId}",
            watchlistItem.Id, fullName, riskLevel, userId);

        // Create audit log
        await CreateAuditLogAsync(
            userId,
            "Create",
            "Watchlist",
            watchlistItem.Id,
            null,
            System.Text.Json.JsonSerializer.Serialize(new
            {
                watchlistItem.FullName,
                watchlistItem.RiskLevel,
                watchlistItem.MonitoringFrequency
            }));

        return watchlistItem;
    }

    public async Task UpdateWatchlistItemAsync(
        int id,
        int userId,
        string? roleStatus = null,
        int? riskSphereId = null,
        string? threatSource = null,
        DateTime? conflictDate = null,
        RiskLevel? riskLevel = null,
        MonitoringFrequency? monitoringFrequency = null,
        DateTime? lastCheckDate = null,
        DateTime? nextCheckDate = null,
        string? dynamicsDescription = null,
        int? watchOwnerId = null,
        string? attachmentsJson = null)
    {
        var watchlistItem = await _context.Watchlists.FindAsync(id);
        if (watchlistItem == null)
            throw new InvalidOperationException($"Watchlist item {id} not found");

        var oldValues = System.Text.Json.JsonSerializer.Serialize(new
        {
            watchlistItem.RoleStatus,
            RiskLevel = watchlistItem.RiskLevel.ToString(),
            MonitoringFrequency = watchlistItem.MonitoringFrequency.ToString(),
            watchlistItem.WatchOwnerId
        });

        // Update fields
        watchlistItem.RoleStatus = roleStatus;
        watchlistItem.RiskSphereId = riskSphereId;
        watchlistItem.ThreatSource = threatSource;
        watchlistItem.ConflictDate = conflictDate;

        if (riskLevel.HasValue)
            watchlistItem.RiskLevel = riskLevel.Value;

        if (monitoringFrequency.HasValue)
            watchlistItem.MonitoringFrequency = monitoringFrequency.Value;

        watchlistItem.LastCheckDate = lastCheckDate;
        watchlistItem.NextCheckDate = nextCheckDate;
        watchlistItem.DynamicsDescription = dynamicsDescription;
        watchlistItem.WatchOwnerId = watchOwnerId;
        watchlistItem.AttachmentsJson = attachmentsJson;
        watchlistItem.UpdatedAt = DateTime.UtcNow;
        watchlistItem.UpdatedBy = userId;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated watchlist item {Id} by user {UserId}", id, userId);

        // Create audit log
        var newValues = System.Text.Json.JsonSerializer.Serialize(new
        {
            watchlistItem.RoleStatus,
            RiskLevel = watchlistItem.RiskLevel.ToString(),
            MonitoringFrequency = watchlistItem.MonitoringFrequency.ToString(),
            watchlistItem.WatchOwnerId
        });

        await CreateAuditLogAsync(userId, "Update", "Watchlist", id, oldValues, newValues);
    }

    public async Task DeleteWatchlistItemAsync(int id, int userId)
    {
        var watchlistItem = await _context.Watchlists.FindAsync(id);
        if (watchlistItem == null)
            throw new InvalidOperationException($"Watchlist item {id} not found");

        var oldValues = System.Text.Json.JsonSerializer.Serialize(new
        {
            watchlistItem.IsActive,
            watchlistItem.FullName
        });

        // Soft delete
        watchlistItem.IsActive = false;
        watchlistItem.UpdatedAt = DateTime.UtcNow;
        watchlistItem.UpdatedBy = userId;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Deactivated watchlist item {Id} by user {UserId}", id, userId);

        // Create audit log
        await CreateAuditLogAsync(
            userId,
            "Delete",
            "Watchlist",
            id,
            oldValues,
            System.Text.Json.JsonSerializer.Serialize(new { IsActive = false }));
    }

    public async Task RecordCheckAsync(
        int id,
        int userId,
        DateTime? nextCheckDate = null,
        string? dynamicsUpdate = null,
        RiskLevel? newRiskLevel = null)
    {
        var watchlistItem = await _context.Watchlists.FindAsync(id);
        if (watchlistItem == null)
            throw new InvalidOperationException($"Watchlist item {id} not found");

        var oldValues = System.Text.Json.JsonSerializer.Serialize(new
        {
            watchlistItem.LastCheckDate,
            watchlistItem.NextCheckDate,
            watchlistItem.RiskLevel,
            watchlistItem.DynamicsDescription
        });

        // Update check dates
        watchlistItem.LastCheckDate = DateTime.UtcNow;
        watchlistItem.NextCheckDate = nextCheckDate;

        // Update dynamics if provided
        if (!string.IsNullOrEmpty(dynamicsUpdate))
        {
            watchlistItem.DynamicsDescription = dynamicsUpdate;
        }

        // Update risk level if provided
        if (newRiskLevel.HasValue)
        {
            watchlistItem.RiskLevel = newRiskLevel.Value;
        }

        watchlistItem.UpdatedAt = DateTime.UtcNow;
        watchlistItem.UpdatedBy = userId;

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Recorded check for watchlist item {Id} by user {UserId}, next check: {NextCheckDate}",
            id, userId, nextCheckDate);

        // Create audit log
        var newValues = System.Text.Json.JsonSerializer.Serialize(new
        {
            watchlistItem.LastCheckDate,
            watchlistItem.NextCheckDate,
            watchlistItem.RiskLevel,
            watchlistItem.DynamicsDescription
        });

        await CreateAuditLogAsync(userId, "Check", "Watchlist", id, oldValues, newValues);
    }

    public async Task<IEnumerable<Watchlist>> GetItemsRequiringCheckAsync()
    {
        var items = await _context.Watchlists
            .Include(w => w.WatchOwner)
            .Where(w => w.IsActive && w.NextCheckDate.HasValue && w.NextCheckDate.Value <= DateTime.UtcNow)
            .OrderBy(w => w.NextCheckDate)
            .ThenByDescending(w => w.RiskLevel)
            .ToListAsync();

        _logger.LogInformation("Retrieved {Count} watchlist items requiring check", items.Count);

        return items;
    }

    public async Task<WatchlistStatistics> GetStatisticsAsync()
    {
        var total = await _context.Watchlists.CountAsync(w => w.IsActive);
        var requiresCheck = await _context.Watchlists
            .CountAsync(w => w.IsActive && w.NextCheckDate.HasValue && w.NextCheckDate.Value <= DateTime.UtcNow);

        var byRiskLevel = await _context.Watchlists
            .Where(w => w.IsActive)
            .GroupBy(w => w.RiskLevel)
            .Select(g => new { RiskLevel = g.Key.ToString(), Count = g.Count() })
            .ToDictionaryAsync(x => x.RiskLevel, x => x.Count);

        var byRiskSphere = await _context.Watchlists
            .Where(w => w.IsActive && w.RiskSphereId != null)
            .GroupBy(w => w.RiskSphereId!.Value)
            .Select(g => new { RiskSphere = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.RiskSphere, x => x.Count);

        var byMonitoringFrequency = await _context.Watchlists
            .Where(w => w.IsActive)
            .GroupBy(w => w.MonitoringFrequency)
            .Select(g => new { Frequency = g.Key.ToString(), Count = g.Count() })
            .ToDictionaryAsync(x => x.Frequency, x => x.Count);

        _logger.LogInformation(
            "Generated watchlist statistics: Total={Total}, RequiresCheck={RequiresCheck}",
            total, requiresCheck);

        return new WatchlistStatistics
        {
            Total = total,
            RequiresCheck = requiresCheck,
            ByRiskLevel = byRiskLevel,
            ByRiskSphere = byRiskSphere,
            ByMonitoringFrequency = byMonitoringFrequency
        };
    }

    private async Task CreateAuditLogAsync(
        int userId,
        string actionType,
        string entityType,
        int entityId,
        string? oldValues,
        string? newValues)
    {
        var action = actionType switch
        {
            "Create" => Core.Enums.AuditActionType.Create,
            "Update" => Core.Enums.AuditActionType.Update,
            "Delete" => Core.Enums.AuditActionType.Delete,
            "Check" => Core.Enums.AuditActionType.Update,
            _ => Core.Enums.AuditActionType.Update
        };

        var auditLog = new AuditLog
        {
            UserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId.ToString(),
            OldValuesJson = oldValues,
            NewValuesJson = newValues,
            Timestamp = DateTime.UtcNow
        };

        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();
    }
}