using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Kurator.Core.Enums;
using Kurator.Infrastructure.Data;

namespace Kurator.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AuditLogController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AuditLogController> _logger;

    public AuditLogController(ApplicationDbContext context, ILogger<AuditLogController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? userId = null,
        [FromQuery] int? blockId = null,
        [FromQuery] AuditActionType? actionType = null,
        [FromQuery] string? entityType = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var query = _context.AuditLogs
            .Include(a => a.User)
            .AsQueryable();

        // Filters
        if (userId.HasValue)
            query = query.Where(a => a.UserId == userId.Value);

        // ИЗМЕНЕНО: ActionType → Action
        if (actionType.HasValue)
            query = query.Where(a => a.Action == actionType.Value);

        if (!string.IsNullOrEmpty(entityType))
            query = query.Where(a => a.EntityType == entityType);

        if (fromDate.HasValue)
            query = query.Where(a => a.Timestamp >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(a => a.Timestamp <= toDate.Value);

        // Filter by block (for contacts and interactions)
        if (blockId.HasValue)
        {
            var contactIds = await _context.Contacts
                .Where(c => c.BlockId == blockId.Value)
                .Select(c => c.Id.ToString())
                .ToListAsync();

            query = query.Where(a => 
                (a.EntityType == "Contact" && contactIds.Contains(a.EntityId)) ||
                (a.EntityType == "Interaction" && _context.Interactions
                    .Where(i => contactIds.Contains(i.ContactId.ToString()))
                    .Select(i => i.Id.ToString())
                    .Contains(a.EntityId))
            );
        }

        var total = await query.CountAsync();

        var logs = await query
            .OrderByDescending(a => a.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuditLogDto
            {
                Id = (int)a.Id,
                UserId = a.UserId,
                UserLogin = a.User.Login,
                // ИЗМЕНЕНО: ActionType → Action
                ActionType = a.Action.ToString(),
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                Timestamp = a.Timestamp,
                // ИЗМЕНЕНО: OldValue → OldValuesJson, NewValue → NewValuesJson
                OldValue = a.OldValuesJson,
                NewValue = a.NewValuesJson
            })
            .ToListAsync();

        return Ok(new
        {
            data = logs,
            page,
            pageSize,
            total,
            totalPages = (int)Math.Ceiling((double)total / pageSize)
        });
    }

    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        fromDate ??= DateTime.UtcNow.AddMonths(-1);
        toDate ??= DateTime.UtcNow;

        var query = _context.AuditLogs
            .Where(a => a.Timestamp >= fromDate && a.Timestamp <= toDate);

        var totalActions = await query.CountAsync();

        // ИЗМЕНЕНО: ActionType → Action
        var byActionType = await query
            .GroupBy(a => a.Action)
            .Select(g => new { ActionType = g.Key.ToString(), Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToDictionaryAsync(x => x.ActionType, x => x.Count);

        var byUser = await query
            .Include(a => a.User)
            .GroupBy(a => a.User.Login)
            .Select(g => new { User = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(10)
            .ToDictionaryAsync(x => x.User, x => x.Count);

        var byEntityType = await query
            .GroupBy(a => a.EntityType)
            .Select(g => new { EntityType = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.EntityType, x => x.Count);

        return Ok(new
        {
            period = new { from = fromDate, to = toDate },
            totalActions,
            byActionType,
            byUser,
            byEntityType
        });
    }

    [HttpGet("entity/{entityType}/{entityId}")]
    public async Task<IActionResult> GetByEntity(string entityType, string entityId)
    {
        var logs = await _context.AuditLogs
            .Include(a => a.User)
            .Where(a => a.EntityType == entityType && a.EntityId == entityId)
            .OrderByDescending(a => a.Timestamp)
            .Select(a => new AuditLogDto
            {
                Id = (int)a.Id,
                UserId = a.UserId,
                UserLogin = a.User.Login,
                // ИЗМЕНЕНО: ActionType → Action
                ActionType = a.Action.ToString(),
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                Timestamp = a.Timestamp,
                // ИЗМЕНЕНО: OldValue → OldValuesJson, NewValue → NewValuesJson
                OldValue = a.OldValuesJson,
                NewValue = a.NewValuesJson
            })
            .ToListAsync();

        return Ok(logs);
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetByUser(
        int userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var query = _context.AuditLogs
            .Include(a => a.User)
            .Where(a => a.UserId == userId);

        var total = await query.CountAsync();

        var logs = await query
            .OrderByDescending(a => a.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuditLogDto
            {
                Id = (int)a.Id,
                UserId = a.UserId,
                UserLogin = a.User.Login,
                // ИЗМЕНЕНО: ActionType → Action
                ActionType = a.Action.ToString(),
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                Timestamp = a.Timestamp,
                // ИЗМЕНЕНО: OldValue → OldValuesJson, NewValue → NewValuesJson
                OldValue = a.OldValuesJson,
                NewValue = a.NewValuesJson
            })
            .ToListAsync();

        return Ok(new
        {
            data = logs,
            page,
            pageSize,
            total,
            totalPages = (int)Math.Ceiling((double)total / pageSize)
        });
    }

    [HttpGet("recent")]
    public async Task<IActionResult> GetRecent([FromQuery] int count = 20)
    {
        var logs = await _context.AuditLogs
            .Include(a => a.User)
            .OrderByDescending(a => a.Timestamp)
            .Take(count)
            .Select(a => new AuditLogDto
            {
                Id = (int)a.Id,
                UserId = a.UserId,
                UserLogin = a.User.Login,
                // ИЗМЕНЕНО: ActionType → Action
                ActionType = a.Action.ToString(),
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                Timestamp = a.Timestamp,
                // ИЗМЕНЕНО: OldValue → OldValuesJson, NewValue → NewValuesJson
                OldValue = a.OldValuesJson,
                NewValue = a.NewValuesJson
            })
            .ToListAsync();

        return Ok(logs);
    }
}

// DTOs
public record AuditLogDto
{
    public int Id { get; init; }
    public int UserId { get; init; }
    public string UserLogin { get; init; } = string.Empty;
    public string ActionType { get; init; } = string.Empty;
    public string EntityType { get; init; } = string.Empty;
    public string EntityId { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
    public string? OldValue { get; init; }
    public string? NewValue { get; init; }
}
