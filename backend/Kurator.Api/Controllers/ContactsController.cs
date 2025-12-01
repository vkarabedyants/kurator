using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Kurator.Core.Entities;
using Kurator.Core.Enums;
using Kurator.Core.Interfaces;
using Kurator.Infrastructure.Data;
using System.Diagnostics;
using System.Security.Claims;

namespace Kurator.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ContactsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<ContactsController> _logger;

    public ContactsController(
        ApplicationDbContext context,
        IEncryptionService encryptionService,
        ILogger<ContactsController> logger)
    {
        _context = context;
        _encryptionService = encryptionService;
        _logger = logger;
        _logger.LogDebug("[Contacts] ContactsController instantiated");
    }

    private int GetUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
    private string GetUserLogin() => User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
    private bool IsAdmin() => User.IsInRole("Admin");
    private string GetClientIp() => HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? blockId = null,
        [FromQuery] string? search = null,
        [FromQuery] int? influenceStatusId = null,
        [FromQuery] int? influenceTypeId = null,
        [FromQuery] int? organizationId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var stopwatch = Stopwatch.StartNew();
        var userId = GetUserId();
        var userLogin = GetUserLogin();
        var isAdmin = IsAdmin();

        _logger.LogInformation("[Contacts] GetAll started. User: {UserLogin} (ID: {UserId}), IsAdmin: {IsAdmin}, Filters: blockId={BlockId}, search={Search}, page={Page}, pageSize={PageSize}",
            userLogin, userId, isAdmin, blockId, search, page, pageSize);

        // ИЗМЕНЕНО: Теперь используем BlockCurator table для проверки доступа
        var query = _context.Contacts
            .Include(c => c.Block)
            .Include(c => c.ResponsibleCurator)
            .Where(c => c.IsActive && c.Block.Status == BlockStatus.Active) // Фильтр архивных блоков
            .AsQueryable();

        // Access control: Curators see only their blocks
        if (!isAdmin)
        {
            var userBlockIds = await _context.BlockCurators
                .Where(bc => bc.UserId == userId)
                .Select(bc => bc.BlockId)
                .ToListAsync();

            query = query.Where(c => userBlockIds.Contains(c.BlockId));
        }

        // Filters
        if (blockId.HasValue)
            query = query.Where(c => c.BlockId == blockId.Value);

        // ИЗМЕНЕНО: Используем InfluenceStatusId и InfluenceTypeId вместо ENUMs
        if (influenceStatusId.HasValue)
            query = query.Where(c => c.InfluenceStatusId == influenceStatusId.Value);

        if (influenceTypeId.HasValue)
            query = query.Where(c => c.InfluenceTypeId == influenceTypeId.Value);

        if (organizationId.HasValue)
            query = query.Where(c => c.OrganizationId == organizationId.Value);

        // Search by ContactId or Position
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(c => c.ContactId.Contains(search) || 
                                    (c.Position != null && c.Position.Contains(search)));
        }

        var total = await query.CountAsync();

        var contacts = await query
            .OrderByDescending(c => c.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new ContactListDto
            {
                Id = c.Id,
                ContactId = c.ContactId,
                FullName = _encryptionService.Decrypt(c.FullNameEncrypted),
                BlockId = c.BlockId,
                BlockName = c.Block.Name,
                BlockCode = c.Block.Code,
                OrganizationId = c.OrganizationId,
                Position = c.Position,
                InfluenceStatusId = c.InfluenceStatusId,
                InfluenceTypeId = c.InfluenceTypeId,
                LastInteractionDate = c.LastInteractionDate,
                NextTouchDate = c.NextTouchDate,
                ResponsibleCuratorId = c.ResponsibleCuratorId,
                ResponsibleCuratorLogin = c.ResponsibleCurator.Login,
                UpdatedAt = c.UpdatedAt,
                UpdatedBy = c.UpdatedBy,
                IsOverdue = c.NextTouchDate.HasValue && c.NextTouchDate.Value < DateTime.UtcNow
            })
            .ToListAsync();

        stopwatch.Stop();
        _logger.LogInformation("[Contacts] GetAll completed. User: {UserLogin} (ID: {UserId}), ResultCount: {Count}, Total: {Total}, Duration: {Duration}ms",
            userLogin, userId, contacts.Count, total, stopwatch.ElapsedMilliseconds);

        return Ok(new
        {
            data = contacts,
            page,
            pageSize,
            total,
            totalPages = (int)Math.Ceiling(total / (double)pageSize)
        });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var stopwatch = Stopwatch.StartNew();
        var userId = GetUserId();
        var userLogin = GetUserLogin();
        var isAdmin = IsAdmin();

        _logger.LogInformation("[Contacts] GetById started. User: {UserLogin} (ID: {UserId}), ContactId: {ContactId}",
            userLogin, userId, id);

        var contact = await _context.Contacts
            .Include(c => c.Block)
            .Include(c => c.ResponsibleCurator)
            .Include(c => c.Interactions.OrderByDescending(i => i.InteractionDate))
            .Include(c => c.StatusHistory.OrderByDescending(s => s.ChangedAt))
            .FirstOrDefaultAsync(c => c.Id == id && c.IsActive && c.Block.Status == BlockStatus.Active);

        if (contact == null)
        {
            stopwatch.Stop();
            _logger.LogWarning("[Contacts] GetById - Contact not found. User: {UserLogin} (ID: {UserId}), ContactId: {Id}, Duration: {Duration}ms",
                userLogin, userId, id, stopwatch.ElapsedMilliseconds);
            return NotFound();
        }

        // Access control through BlockCurator table
        if (!isAdmin)
        {
            var hasAccess = await _context.BlockCurators
                .AnyAsync(bc => bc.BlockId == contact.BlockId && bc.UserId == userId);

            if (!hasAccess)
            {
                stopwatch.Stop();
                _logger.LogWarning("[Contacts] GetById - Access denied. User: {UserLogin} (ID: {UserId}), ContactId: {Id}, BlockId: {BlockId}, Duration: {Duration}ms",
                    userLogin, userId, id, contact.BlockId, stopwatch.ElapsedMilliseconds);
                return Forbid();
            }
        }

        _logger.LogDebug("[Contacts] GetById - Building response for contact: {ContactIdentifier}, BlockId: {BlockId}",
            contact.ContactId, contact.BlockId);

        var result = new ContactDetailDto
        {
            Id = contact.Id,
            ContactId = contact.ContactId,
            FullName = _encryptionService.Decrypt(contact.FullNameEncrypted),
            BlockId = contact.BlockId,
            BlockName = contact.Block.Name,
            BlockCode = contact.Block.Code,
            OrganizationId = contact.OrganizationId,
            Position = contact.Position,
            InfluenceStatusId = contact.InfluenceStatusId,
            InfluenceTypeId = contact.InfluenceTypeId,
            UsefulnessDescription = contact.UsefulnessDescription,
            CommunicationChannelId = contact.CommunicationChannelId,
            ContactSourceId = contact.ContactSourceId,
            LastInteractionDate = contact.LastInteractionDate,
            NextTouchDate = contact.NextTouchDate,
            Notes = contact.NotesEncrypted != null ? _encryptionService.Decrypt(contact.NotesEncrypted) : null,
            ResponsibleCuratorId = contact.ResponsibleCuratorId,
            ResponsibleCuratorLogin = contact.ResponsibleCurator.Login,
            CreatedAt = contact.CreatedAt,
            UpdatedAt = contact.UpdatedAt,
            UpdatedBy = contact.UpdatedBy,
            InteractionCount = contact.Interactions.Count,
            LastInteractionDaysAgo = contact.LastInteractionDate.HasValue
                ? (int)(DateTime.UtcNow - contact.LastInteractionDate.Value).TotalDays
                : null,
            IsOverdue = contact.NextTouchDate.HasValue && contact.NextTouchDate.Value < DateTime.UtcNow,
            Interactions = contact.Interactions.Select(i => new InteractionSummaryDto
            {
                Id = i.Id,
                InteractionDate = i.InteractionDate,
                InteractionTypeId = i.InteractionTypeId,
                ResultId = i.ResultId,
                Comment = i.CommentEncrypted != null ? _encryptionService.Decrypt(i.CommentEncrypted) : null,
                // ИЗМЕНЕНО: StatusChangeTo → StatusChangeJson
                StatusChangeJson = i.StatusChangeJson,
                CuratorLogin = i.Curator.Login
            }).ToList(),
            StatusHistory = contact.StatusHistory.Select(s => new StatusHistoryDto
            {
                Id = s.Id,
                OldStatus = s.PreviousStatus,
                NewStatus = s.NewStatus,
                ChangedAt = s.ChangedAt,
                ChangedBy = s.ChangedBy.Login
            }).ToList()
        };

        stopwatch.Stop();
        _logger.LogInformation("[Contacts] GetById completed. User: {UserLogin} (ID: {UserId}), ContactId: {Id}, ContactIdentifier: {ContactIdentifier}, InteractionCount: {InteractionCount}, Duration: {Duration}ms",
            userLogin, userId, id, contact.ContactId, contact.Interactions.Count, stopwatch.ElapsedMilliseconds);

        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Curator")]
    public async Task<IActionResult> Create([FromBody] CreateContactRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        var userId = GetUserId();
        var userLogin = GetUserLogin();

        _logger.LogInformation("[Contacts] Create started. User: {UserLogin} (ID: {UserId}), BlockId: {BlockId}",
            userLogin, userId, request.BlockId);
        _logger.LogDebug("[Contacts] Create request details: {Request}", System.Text.Json.JsonSerializer.Serialize(request));

        if (!ModelState.IsValid)
        {
            stopwatch.Stop();
            _logger.LogWarning("[Contacts] Create - Model validation failed. User: {UserLogin} (ID: {UserId}), Errors: {Errors}, Duration: {Duration}ms",
                userLogin, userId, string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)), stopwatch.ElapsedMilliseconds);
            return BadRequest(new { message = "Invalid request", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
        }

        var isAdmin = IsAdmin();

        // Access control through BlockCurator table
        if (!isAdmin)
        {
            var hasAccess = await _context.BlockCurators
                .AnyAsync(bc => bc.BlockId == request.BlockId && bc.UserId == userId);

            if (!hasAccess)
            {
                stopwatch.Stop();
                _logger.LogWarning("[Contacts] Create - Access denied. User: {UserLogin} (ID: {UserId}), BlockId: {BlockId}, Duration: {Duration}ms",
                    userLogin, userId, request.BlockId, stopwatch.ElapsedMilliseconds);
                return Forbid();
            }
        }

        // Generate ContactId (BLOCKCODE-###)
        var block = await _context.Blocks.FindAsync(request.BlockId);
        if (block == null)
        {
            stopwatch.Stop();
            _logger.LogWarning("[Contacts] Create - Block not found. User: {UserLogin} (ID: {UserId}), BlockId: {BlockId}, Duration: {Duration}ms",
                userLogin, userId, request.BlockId, stopwatch.ElapsedMilliseconds);
            return BadRequest(new { message = "Block not found" });
        }

        // Find the maximum existing number for this block's contacts
        var existingContacts = await _context.Contacts
            .Where(c => c.BlockId == request.BlockId)
            .Select(c => c.ContactId)
            .ToListAsync();

        int nextNumber = 1;
        foreach (var existingContactId in existingContacts)
        {
            // Try to extract number from formats like "BLOCK-001" or "BLOCK001"
            var numberPart = existingContactId;

            // Remove the block code prefix (with or without dash)
            if (existingContactId.StartsWith(block.Code + "-", StringComparison.OrdinalIgnoreCase))
            {
                numberPart = existingContactId.Substring(block.Code.Length + 1);
            }
            else if (existingContactId.StartsWith(block.Code, StringComparison.OrdinalIgnoreCase))
            {
                numberPart = existingContactId.Substring(block.Code.Length);
            }

            if (int.TryParse(numberPart, out int existingNumber))
            {
                if (existingNumber >= nextNumber)
                {
                    nextNumber = existingNumber + 1;
                }
            }
        }

        var contactId = $"{block.Code}-{nextNumber:D3}";

        // ИЗМЕНЕНО: InfluenceStatus и InfluenceType теперь int?, UpdatedBy - int
        var contact = new Contact
        {
            ContactId = contactId,
            BlockId = request.BlockId,
            FullNameEncrypted = _encryptionService.Encrypt(request.FullName),
            OrganizationId = request.OrganizationId,
            Position = request.Position,
            InfluenceStatusId = request.InfluenceStatusId,
            InfluenceTypeId = request.InfluenceTypeId,
            UsefulnessDescription = request.UsefulnessDescription,
            CommunicationChannelId = request.CommunicationChannelId,
            ContactSourceId = request.ContactSourceId,
            NextTouchDate = request.NextTouchDate,
            NotesEncrypted = !string.IsNullOrEmpty(request.Notes)
                ? _encryptionService.Encrypt(request.Notes)
                : null,
            ResponsibleCuratorId = userId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = userId
        };

        _context.Contacts.Add(contact);

        // ИЗМЕНЕНО: Используем Action вместо ActionType, NewValuesJson вместо NewValue
        var auditLog = new AuditLog
        {
            UserId = userId,
            Action = Core.Enums.AuditActionType.Create,
            EntityType = "Contact",
            EntityId = contact.Id.ToString(),
            Timestamp = DateTime.UtcNow,
            NewValuesJson = System.Text.Json.JsonSerializer.Serialize(new { ContactId = contactId, InfluenceStatusId = request.InfluenceStatusId })
        };
        _context.AuditLogs.Add(auditLog);

        await _context.SaveChangesAsync();

        stopwatch.Stop();
        _logger.LogInformation("[Contacts] Create completed. User: {UserLogin} (ID: {UserId}), NewContactId: {ContactId}, Id: {Id}, BlockId: {BlockId}, Duration: {Duration}ms",
            userLogin, userId, contactId, contact.Id, request.BlockId, stopwatch.ElapsedMilliseconds);

        return CreatedAtAction(nameof(GetById), new { id = contact.Id }, new { id = contact.Id, contactId });
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Curator")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateContactRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        var userId = GetUserId();
        var userLogin = GetUserLogin();
        var isAdmin = IsAdmin();

        _logger.LogInformation("[Contacts] Update started. User: {UserLogin} (ID: {UserId}), ContactId: {Id}",
            userLogin, userId, id);

        var contact = await _context.Contacts
            .Include(c => c.Block)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (contact == null)
        {
            stopwatch.Stop();
            _logger.LogWarning("[Contacts] Update - Contact not found. User: {UserLogin} (ID: {UserId}), ContactId: {Id}, Duration: {Duration}ms",
                userLogin, userId, id, stopwatch.ElapsedMilliseconds);
            return NotFound();
        }

        // Access control through BlockCurator table
        if (!isAdmin)
        {
            var hasAccess = await _context.BlockCurators
                .AnyAsync(bc => bc.BlockId == contact.BlockId && bc.UserId == userId);

            if (!hasAccess)
            {
                stopwatch.Stop();
                _logger.LogWarning("[Contacts] Update - Access denied. User: {UserLogin} (ID: {UserId}), ContactId: {Id}, BlockId: {BlockId}, Duration: {Duration}ms",
                    userLogin, userId, id, contact.BlockId, stopwatch.ElapsedMilliseconds);
                return Forbid();
            }
        }

        var oldStatusId = contact.InfluenceStatusId;
        _logger.LogDebug("[Contacts] Update - Current status: {OldStatusId}, New status: {NewStatusId}",
            oldStatusId, request.InfluenceStatusId);

        contact.OrganizationId = request.OrganizationId;
        contact.Position = request.Position;
        contact.InfluenceStatusId = request.InfluenceStatusId;
        contact.InfluenceTypeId = request.InfluenceTypeId;
        contact.UsefulnessDescription = request.UsefulnessDescription;
        contact.CommunicationChannelId = request.CommunicationChannelId;
        contact.ContactSourceId = request.ContactSourceId;
        contact.NextTouchDate = request.NextTouchDate;
        contact.NotesEncrypted = !string.IsNullOrEmpty(request.Notes)
            ? _encryptionService.Encrypt(request.Notes)
            : null;
        contact.UpdatedAt = DateTime.UtcNow;
        contact.UpdatedBy = userId;

        // ИЗМЕНЕНО: Track influence status change с использованием int? вместо ENUM
        if (oldStatusId != request.InfluenceStatusId)
        {
            var statusHistory = new InfluenceStatusHistory
            {
                ContactId = contact.Id,
                PreviousStatus = oldStatusId?.ToString() ?? "null",
                NewStatus = request.InfluenceStatusId?.ToString() ?? "null",
                ChangedByUserId = userId,
                ChangedAt = DateTime.UtcNow
            };
            _context.InfluenceStatusHistories.Add(statusHistory);

            var auditLog = new AuditLog
            {
                UserId = userId,
                Action = Core.Enums.AuditActionType.StatusChange,
                EntityType = "Contact",
                EntityId = contact.Id.ToString(),
                Timestamp = DateTime.UtcNow,
                OldValuesJson = System.Text.Json.JsonSerializer.Serialize(new { InfluenceStatusId = oldStatusId }),
                NewValuesJson = System.Text.Json.JsonSerializer.Serialize(new { InfluenceStatusId = request.InfluenceStatusId })
            };
            _context.AuditLogs.Add(auditLog);
        }
        else
        {
            var auditLog = new AuditLog
            {
                UserId = userId,
                Action = Core.Enums.AuditActionType.Update,
                EntityType = "Contact",
                EntityId = contact.Id.ToString(),
                Timestamp = DateTime.UtcNow,
                NewValuesJson = System.Text.Json.JsonSerializer.Serialize(new { ContactId = contact.ContactId })
            };
            _context.AuditLogs.Add(auditLog);
        }

        await _context.SaveChangesAsync();

        stopwatch.Stop();
        _logger.LogInformation("[Contacts] Update completed. User: {UserLogin} (ID: {UserId}), ContactId: {Id}, ContactIdentifier: {ContactIdentifier}, StatusChanged: {StatusChanged}, Duration: {Duration}ms",
            userLogin, userId, id, contact.ContactId, oldStatusId != request.InfluenceStatusId, stopwatch.ElapsedMilliseconds);

        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var stopwatch = Stopwatch.StartNew();
        var userId = GetUserId();
        var userLogin = GetUserLogin();

        _logger.LogInformation("[Contacts] Delete started. User: {UserLogin} (ID: {UserId}), ContactId: {Id}",
            userLogin, userId, id);

        var contact = await _context.Contacts.FindAsync(id);
        if (contact == null)
        {
            stopwatch.Stop();
            _logger.LogWarning("[Contacts] Delete - Contact not found. User: {UserLogin} (ID: {UserId}), ContactId: {Id}, Duration: {Duration}ms",
                userLogin, userId, id, stopwatch.ElapsedMilliseconds);
            return NotFound();
        }

        contact.IsActive = false;
        contact.UpdatedAt = DateTime.UtcNow;
        contact.UpdatedBy = userId;

        var auditLog = new AuditLog
        {
            UserId = userId,
            Action = Core.Enums.AuditActionType.Delete,
            EntityType = "Contact",
            EntityId = contact.Id.ToString(),
            Timestamp = DateTime.UtcNow,
            OldValuesJson = System.Text.Json.JsonSerializer.Serialize(new { ContactId = contact.ContactId })
        };
        _context.AuditLogs.Add(auditLog);

        await _context.SaveChangesAsync();

        stopwatch.Stop();
        _logger.LogInformation("[Contacts] Delete completed (soft delete). User: {UserLogin} (ID: {UserId}), ContactId: {Id}, ContactIdentifier: {ContactIdentifier}, Duration: {Duration}ms",
            userLogin, userId, id, contact.ContactId, stopwatch.ElapsedMilliseconds);

        return NoContent();
    }

    [HttpGet("overdue")]
    [Authorize(Roles = "Admin,Curator")]
    public async Task<IActionResult> GetOverdueContacts()
    {
        var stopwatch = Stopwatch.StartNew();
        var userId = GetUserId();
        var userLogin = GetUserLogin();
        var isAdmin = IsAdmin();

        _logger.LogInformation("[Contacts] GetOverdueContacts started. User: {UserLogin} (ID: {UserId}), IsAdmin: {IsAdmin}",
            userLogin, userId, isAdmin);

        // ИЗМЕНЕНО: Фильтрация по IsActive и использование BlockCurator table
        var query = _context.Contacts
            .Include(c => c.Block)
            .Include(c => c.ResponsibleCurator)
            .Where(c => c.IsActive && c.Block.Status == BlockStatus.Active && c.NextTouchDate.HasValue && c.NextTouchDate.Value < DateTime.UtcNow);

        if (!isAdmin)
        {
            var userBlockIds = await _context.BlockCurators
                .Where(bc => bc.UserId == userId)
                .Select(bc => bc.BlockId)
                .ToListAsync();

            query = query.Where(c => userBlockIds.Contains(c.BlockId));
        }

        var contacts = await query
            .OrderBy(c => c.NextTouchDate)
            .Select(c => new ContactListDto
            {
                Id = c.Id,
                ContactId = c.ContactId,
                FullName = _encryptionService.Decrypt(c.FullNameEncrypted),
                BlockId = c.BlockId,
                BlockName = c.Block.Name,
                BlockCode = c.Block.Code,
                Position = c.Position,
                InfluenceStatusId = c.InfluenceStatusId,
                LastInteractionDate = c.LastInteractionDate,
                NextTouchDate = c.NextTouchDate,
                ResponsibleCuratorLogin = c.ResponsibleCurator.Login,
                IsOverdue = true
            })
            .ToListAsync();

        stopwatch.Stop();
        _logger.LogInformation("[Contacts] GetOverdueContacts completed. User: {UserLogin} (ID: {UserId}), OverdueCount: {Count}, Duration: {Duration}ms",
            userLogin, userId, contacts.Count, stopwatch.ElapsedMilliseconds);

        return Ok(contacts);
    }
}

// DTOs
// ИЗМЕНЕНО: InfluenceStatus/Type теперь int?, OrganizationId тоже int?, UpdatedBy - int
public record ContactListDto
{
    public int Id { get; init; }
    public string ContactId { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public int BlockId { get; init; }
    public string BlockName { get; init; } = string.Empty;
    public string BlockCode { get; init; } = string.Empty;
    public int? OrganizationId { get; init; }
    public string? Position { get; init; }
    public int? InfluenceStatusId { get; init; }
    public int? InfluenceTypeId { get; init; }
    public DateTime? LastInteractionDate { get; init; }
    public DateTime? NextTouchDate { get; init; }
    public int ResponsibleCuratorId { get; init; }
    public string ResponsibleCuratorLogin { get; init; } = string.Empty;
    public DateTime UpdatedAt { get; init; }
    public int UpdatedBy { get; init; }
    public bool IsOverdue { get; init; }
}

public record ContactDetailDto : ContactListDto
{
    public string? UsefulnessDescription { get; init; }
    public int? CommunicationChannelId { get; init; }
    public int? ContactSourceId { get; init; }
    public string? Notes { get; init; }
    public DateTime CreatedAt { get; init; }
    public int InteractionCount { get; init; }
    public int? LastInteractionDaysAgo { get; init; }
    public List<InteractionSummaryDto> Interactions { get; init; } = new();
    public List<StatusHistoryDto> StatusHistory { get; init; } = new();
}

// ИЗМЕНЕНО: InteractionTypeId и ResultId теперь int?, StatusChangeTo → StatusChangeJson
public record InteractionSummaryDto
{
    public int Id { get; init; }
    public DateTime InteractionDate { get; init; }
    public int? InteractionTypeId { get; init; }
    public int? ResultId { get; init; }
    public string? Comment { get; init; }
    public string? StatusChangeJson { get; init; }
    public string CuratorLogin { get; init; } = string.Empty;
}

public record StatusHistoryDto
{
    public int Id { get; init; }
    public string OldStatus { get; init; } = string.Empty;
    public string NewStatus { get; init; } = string.Empty;
    public DateTime ChangedAt { get; init; }
    public string ChangedBy { get; init; } = string.Empty;
}

// ИЗМЕНЕНО: Все ссылки на справочники теперь int?, убран ENUM
public record CreateContactRequest(
    int BlockId,
    string FullName,
    int? OrganizationId,
    string? Position,
    int? InfluenceStatusId,
    int? InfluenceTypeId,
    string? UsefulnessDescription,
    int? CommunicationChannelId,
    int? ContactSourceId,
    DateTime? NextTouchDate,
    string? Notes
);

public record UpdateContactRequest(
    int? OrganizationId,
    string? Position,
    int? InfluenceStatusId,
    int? InfluenceTypeId,
    string? UsefulnessDescription,
    int? CommunicationChannelId,
    int? ContactSourceId,
    DateTime? NextTouchDate,
    string? Notes
);
