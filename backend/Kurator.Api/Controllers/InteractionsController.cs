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
[Authorize]
public class InteractionsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<InteractionsController> _logger;

    public InteractionsController(
        ApplicationDbContext context,
        IEncryptionService encryptionService,
        ILogger<InteractionsController> logger)
    {
        _context = context;
        _encryptionService = encryptionService;
        _logger = logger;
    }

    private int GetUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
    private string GetUserLogin() => User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
    private bool IsAdmin() => User.IsInRole("Admin");

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? contactId = null,
        [FromQuery] int? blockId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        // ИЗМЕНЕНО: string → int? для типизированного справочника
        [FromQuery] int? interactionTypeId = null,
        [FromQuery] int? resultId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var userId = GetUserId();
        var isAdmin = IsAdmin();

        var query = _context.Interactions
            .Include(i => i.Contact)
                .ThenInclude(c => c.Block)
            .Include(i => i.Curator)
            // ИЗМЕНЕНО: Добавлен фильтр по IsActive
            .Where(i => i.IsActive)
            .AsQueryable();

        // Access control: Curators see only their blocks
        // ИЗМЕНЕНО: Используем BlockCurators table вместо PrimaryCuratorId/BackupCuratorId
        if (!isAdmin)
        {
            var userBlockIds = await _context.BlockCurators
                .Where(bc => bc.UserId == userId)
                .Select(bc => bc.BlockId)
                .Distinct()
                .ToListAsync();

            query = query.Where(i => userBlockIds.Contains(i.Contact.BlockId));
        }

        // Filters
        if (contactId.HasValue)
            query = query.Where(i => i.ContactId == contactId.Value);

        if (blockId.HasValue)
            query = query.Where(i => i.Contact.BlockId == blockId.Value);

        if (fromDate.HasValue)
            query = query.Where(i => i.InteractionDate >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(i => i.InteractionDate <= toDate.Value);

        // ИЗМЕНЕНО: interactionTypeId и resultId теперь int?
        if (interactionTypeId.HasValue)
            query = query.Where(i => i.InteractionTypeId == interactionTypeId.Value);

        if (resultId.HasValue)
            query = query.Where(i => i.ResultId == resultId.Value);

        var total = await query.CountAsync();

        var interactions = await query
            .OrderByDescending(i => i.InteractionDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(i => new InteractionDto
            {
                Id = i.Id,
                ContactId = i.ContactId,
                ContactName = _encryptionService.Decrypt(i.Contact.FullNameEncrypted),
                ContactDisplayId = i.Contact.ContactId,
                BlockName = i.Contact.Block.Name,
                InteractionDate = i.InteractionDate,
                InteractionTypeId = i.InteractionTypeId,
                CuratorId = i.CuratorId,
                CuratorLogin = i.Curator.Login,
                ResultId = i.ResultId,
                Comment = i.CommentEncrypted != null ? _encryptionService.Decrypt(i.CommentEncrypted) : null,
                // ИЗМЕНЕНО: StatusChangeTo → StatusChangeJson, AttachmentPath → AttachmentsJson
                StatusChangeJson = i.StatusChangeJson,
                AttachmentsJson = i.AttachmentsJson,
                NextTouchDate = i.NextTouchDate,
                CreatedAt = i.CreatedAt,
                UpdatedAt = i.UpdatedAt,
                UpdatedBy = i.UpdatedBy
            })
            .ToListAsync();

        return Ok(new
        {
            data = interactions,
            page,
            pageSize,
            total,
            totalPages = (int)Math.Ceiling(total / (double)pageSize)
        });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var userId = GetUserId();
        var isAdmin = IsAdmin();

        var interaction = await _context.Interactions
            .Include(i => i.Contact)
                .ThenInclude(c => c.Block)
            .Include(i => i.Curator)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (interaction == null)
            return NotFound();

        // Access control
        // ИЗМЕНЕНО: Используем BlockCurators table
        if (!isAdmin)
        {
            var hasAccess = await _context.BlockCurators
                .AnyAsync(bc => bc.BlockId == interaction.Contact.BlockId && bc.UserId == userId);

            if (!hasAccess)
                return Forbid();
        }

        var result = new InteractionDto
        {
            Id = interaction.Id,
            ContactId = interaction.ContactId,
            ContactName = _encryptionService.Decrypt(interaction.Contact.FullNameEncrypted),
            ContactDisplayId = interaction.Contact.ContactId,
            BlockName = interaction.Contact.Block.Name,
            InteractionDate = interaction.InteractionDate,
            InteractionTypeId = interaction.InteractionTypeId,
            CuratorId = interaction.CuratorId,
            CuratorLogin = interaction.Curator.Login,
            ResultId = interaction.ResultId,
            Comment = interaction.CommentEncrypted != null
                ? _encryptionService.Decrypt(interaction.CommentEncrypted)
                : null,
            // ИЗМЕНЕНО: StatusChangeTo → StatusChangeJson, AttachmentPath → AttachmentsJson
            StatusChangeJson = interaction.StatusChangeJson,
            AttachmentsJson = interaction.AttachmentsJson,
            NextTouchDate = interaction.NextTouchDate,
            CreatedAt = interaction.CreatedAt,
            UpdatedAt = interaction.UpdatedAt,
            UpdatedBy = interaction.UpdatedBy
        };

        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Curator,BackupCurator")]
    public async Task<IActionResult> Create([FromBody] CreateInteractionRequest request)
    {
        var userId = GetUserId();
        var userLogin = GetUserLogin();
        var isAdmin = IsAdmin();

        var contact = await _context.Contacts
            .Include(c => c.Block)
            .FirstOrDefaultAsync(c => c.Id == request.ContactId);

        if (contact == null)
            return BadRequest(new { message = "Contact not found" });

        // Access control
        // ИЗМЕНЕНО: Используем BlockCurators table
        if (!isAdmin)
        {
            var hasAccess = await _context.BlockCurators
                .AnyAsync(bc => bc.BlockId == contact.BlockId && bc.UserId == userId);

            if (!hasAccess)
                return Forbid();
        }

        var interaction = new Interaction
        {
            ContactId = request.ContactId,
            InteractionDate = request.InteractionDate ?? DateTime.UtcNow,
            InteractionTypeId = request.InteractionTypeId,
            CuratorId = userId,
            ResultId = request.ResultId,
            CommentEncrypted = !string.IsNullOrEmpty(request.Comment)
                ? _encryptionService.Encrypt(request.Comment)
                : null,
            // ИЗМЕНЕНО: StatusChangeTo → StatusChangeJson, AttachmentPath → AttachmentsJson
            StatusChangeJson = request.StatusChangeJson,
            AttachmentsJson = request.AttachmentsJson,
            NextTouchDate = request.NextTouchDate,
            // ИЗМЕНЕНО: IsActive по умолчанию true
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            // ИЗМЕНЕНО: UpdatedBy теперь int вместо string
            UpdatedBy = userId
        };

        _context.Interactions.Add(interaction);

        // Update contact's last interaction date and next touch date
        contact.LastInteractionDate = interaction.InteractionDate;
        // ИЗМЕНЕНО: ВАЖНАЯ ЛОГИКА - при создании interaction с NextTouchDate обновляем Contact.NextTouchDate
        if (request.NextTouchDate.HasValue)
        {
            contact.NextTouchDate = request.NextTouchDate.Value;
        }
        contact.UpdatedAt = DateTime.UtcNow;
        // ИЗМЕНЕНО: UpdatedBy теперь int
        contact.UpdatedBy = userId;

        // If status changed, record it
        // ИЗМЕНЕНО: Обработка StatusChangeJson вместо StatusChangeTo
        if (!string.IsNullOrEmpty(request.StatusChangeJson))
        {
            var oldStatus = contact.InfluenceStatusId?.ToString() ?? "null";

            // Парсим JSON для получения нового статуса
            try
            {
                var statusChange = System.Text.Json.JsonDocument.Parse(request.StatusChangeJson);
                if (statusChange.RootElement.TryGetProperty("newStatus", out var newStatusElement))
                {
                    var newStatusStr = newStatusElement.GetString();
                    // ИЗМЕНЕНО: Парсим int вместо enum, т.к. теперь InfluenceStatusId это int?
                    if (!string.IsNullOrEmpty(newStatusStr) && int.TryParse(newStatusStr, out var newStatusId))
                    {
                        var oldStatusId = contact.InfluenceStatusId?.ToString() ?? "null";
                        contact.InfluenceStatusId = newStatusId;

                        var statusHistory = new InfluenceStatusHistory
                        {
                            ContactId = contact.Id,
                            PreviousStatus = oldStatusId,
                            NewStatus = newStatusStr,
                            ChangedByUserId = userId,
                            ChangedAt = DateTime.UtcNow
                        };
                        _context.InfluenceStatusHistories.Add(statusHistory);

                        // ИЗМЕНЕНО: ActionType → Action, используем StatusChange вместо несуществующего ChangeInfluenceStatus
                        var auditLog = new AuditLog
                        {
                            UserId = userId,
                            Action = AuditActionType.StatusChange,
                            EntityType = "Contact",
                            EntityId = contact.Id.ToString(),
                            Timestamp = DateTime.UtcNow,
                            OldValuesJson = oldStatusId,
                            NewValuesJson = newStatusStr
                        };
                        _context.AuditLogs.Add(auditLog);
                    }
                }
            }
            catch (System.Text.Json.JsonException)
            {
                // Игнорируем невалидный JSON
            }
        }

        // Audit log for interaction
        // ИЗМЕНЕНО: ActionType → Action используем Create, NewValue → NewValuesJson
        var interactionAuditLog = new AuditLog
        {
            UserId = userId,
            Action = AuditActionType.Create,
            EntityType = "Interaction",
            EntityId = interaction.Id.ToString(),
            Timestamp = DateTime.UtcNow,
            NewValuesJson = $"Contact: {contact.ContactId}, Type: {request.InteractionTypeId}"
        };
        _context.AuditLogs.Add(interactionAuditLog);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Interaction created for contact {ContactId} by {User}", 
            contact.ContactId, userLogin);

        return CreatedAtAction(nameof(GetById), new { id = interaction.Id }, new { id = interaction.Id });
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Curator,BackupCurator")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateInteractionRequest request)
    {
        var userId = GetUserId();
        var userLogin = GetUserLogin();
        var isAdmin = IsAdmin();

        var interaction = await _context.Interactions
            .Include(i => i.Contact)
                .ThenInclude(c => c.Block)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (interaction == null)
            return NotFound();

        // Access control
        // ИЗМЕНЕНО: Используем BlockCurators table
        if (!isAdmin)
        {
            var hasAccess = await _context.BlockCurators
                .AnyAsync(bc => bc.BlockId == interaction.Contact.BlockId && bc.UserId == userId);

            if (!hasAccess)
                return Forbid();
        }

        interaction.InteractionDate = request.InteractionDate ?? interaction.InteractionDate;
        interaction.InteractionTypeId = request.InteractionTypeId;
        interaction.ResultId = request.ResultId;
        interaction.CommentEncrypted = !string.IsNullOrEmpty(request.Comment)
            ? _encryptionService.Encrypt(request.Comment)
            : null;
        // ИЗМЕНЕНО: StatusChangeTo → StatusChangeJson
        interaction.StatusChangeJson = request.StatusChangeJson;
        interaction.NextTouchDate = request.NextTouchDate;
        interaction.UpdatedAt = DateTime.UtcNow;
        // ИЗМЕНЕНО: UpdatedBy теперь int
        interaction.UpdatedBy = userId;

        // Update contact if needed
        // ИЗМЕНЕНО: ВАЖНАЯ ЛОГИКА - при обновлении NextTouchDate обновляем Contact.NextTouchDate
        if (request.NextTouchDate.HasValue)
        {
            interaction.Contact.NextTouchDate = request.NextTouchDate.Value;
            interaction.Contact.UpdatedAt = DateTime.UtcNow;
            // ИЗМЕНЕНО: UpdatedBy теперь int
            interaction.Contact.UpdatedBy = userId;
        }

        // ИЗМЕНЕНО: ActionType → Action используем Update, NewValue → NewValuesJson
        var auditLog = new AuditLog
        {
            UserId = userId,
            Action = AuditActionType.Update,
            EntityType = "Interaction",
            EntityId = interaction.Id.ToString(),
            Timestamp = DateTime.UtcNow,
            NewValuesJson = $"Updated interaction for {interaction.Contact.ContactId}"
        };
        _context.AuditLogs.Add(auditLog);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Interaction {Id} updated by {User}", id, userLogin);

        return NoContent();
    }

    // ИЗМЕНЕНО: DELETE → PUT {id}/deactivate с установкой IsActive = false (soft delete)
    [HttpPut("{id}/deactivate")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Deactivate(int id)
    {
        var userId = GetUserId();
        var userLogin = GetUserLogin();

        var interaction = await _context.Interactions
            .Include(i => i.Contact)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (interaction == null)
            return NotFound();

        // Soft delete - устанавливаем IsActive = false
        interaction.IsActive = false;
        interaction.UpdatedAt = DateTime.UtcNow;
        interaction.UpdatedBy = userId;

        // ИЗМЕНЕНО: ActionType → Action используем Delete, OldValue → OldValuesJson
        var auditLog = new AuditLog
        {
            UserId = userId,
            Action = AuditActionType.Delete,
            EntityType = "Interaction",
            EntityId = interaction.Id.ToString(),
            Timestamp = DateTime.UtcNow,
            OldValuesJson = $"Contact: {interaction.Contact.ContactId}"
        };
        _context.AuditLogs.Add(auditLog);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Interaction {Id} deactivated by {User}", id, userLogin);

        return NoContent();
    }

    [HttpGet("recent")]
    [Authorize(Roles = "Admin,Curator,BackupCurator")]
    public async Task<IActionResult> GetRecentInteractions([FromQuery] int count = 5)
    {
        var userId = GetUserId();
        var isAdmin = IsAdmin();

        var query = _context.Interactions
            .Include(i => i.Contact)
                .ThenInclude(c => c.Block)
            .Include(i => i.Curator)
            .AsQueryable();

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

        var interactions = await query
            .OrderByDescending(i => i.InteractionDate)
            .Take(count)
            .Select(i => new InteractionDto
            {
                Id = i.Id,
                ContactId = i.ContactId,
                ContactName = _encryptionService.Decrypt(i.Contact.FullNameEncrypted),
                ContactDisplayId = i.Contact.ContactId,
                BlockName = i.Contact.Block.Name,
                InteractionDate = i.InteractionDate,
                InteractionTypeId = i.InteractionTypeId,
                ResultId = i.ResultId,
                CuratorLogin = i.Curator.Login
            })
            .ToListAsync();

        return Ok(interactions);
    }
}

// DTOs
// ИЗМЕНЕНО: InteractionTypeId и ResultId теперь int?, StatusChangeTo → StatusChangeJson, AttachmentPath → AttachmentsJson, UpdatedBy → int
public record InteractionDto
{
    public int Id { get; init; }
    public int ContactId { get; init; }
    public string ContactName { get; init; } = string.Empty;
    public string ContactDisplayId { get; init; } = string.Empty;
    public string BlockName { get; init; } = string.Empty;
    public DateTime InteractionDate { get; init; }
    public int? InteractionTypeId { get; init; }
    public int CuratorId { get; init; }
    public string CuratorLogin { get; init; } = string.Empty;
    public int? ResultId { get; init; }
    public string? Comment { get; init; }
    public string? StatusChangeJson { get; init; }
    public string? AttachmentsJson { get; init; }
    public DateTime? NextTouchDate { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public int UpdatedBy { get; init; }
}

// ИЗМЕНЕНО: InteractionTypeId и ResultId теперь int?, StatusChangeTo → StatusChangeJson, AttachmentPath → AttachmentsJson
public record CreateInteractionRequest(
    int ContactId,
    DateTime? InteractionDate,
    int? InteractionTypeId,
    int? ResultId,
    string? Comment,
    string? StatusChangeJson,
    string? AttachmentsJson,
    DateTime? NextTouchDate
);

// ИЗМЕНЕНО: InteractionTypeId и ResultId теперь int?, StatusChangeTo → StatusChangeJson
public record UpdateInteractionRequest(
    DateTime? InteractionDate,
    int? InteractionTypeId,
    int? ResultId,
    string? Comment,
    string? StatusChangeJson,
    DateTime? NextTouchDate
);
