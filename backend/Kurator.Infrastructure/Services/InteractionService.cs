using Kurator.Core.Entities;
using Kurator.Core.Enums;
using Kurator.Core.Interfaces;
using Kurator.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Kurator.Infrastructure.Services;

public class InteractionService : IInteractionService
{
    private readonly ApplicationDbContext _context;
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<InteractionService> _logger;

    public InteractionService(
        ApplicationDbContext context,
        IEncryptionService encryptionService,
        ILogger<InteractionService> logger)
    {
        _context = context;
        _encryptionService = encryptionService;
        _logger = logger;
    }

    public async Task<(IEnumerable<Interaction> Interactions, int Total)> GetInteractionsAsync(
        int userId,
        bool isAdmin,
        int? contactId = null,
        int? blockId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int? interactionTypeId = null,
        int? resultId = null,
        int page = 1,
        int pageSize = 50)
    {
        var query = _context.Set<Interaction>()
            .Include(i => i.Contact)
                .ThenInclude(c => c.Block)
            .Include(i => i.Curator)
            .Where(i => i.IsActive && i.Contact.Block.Status == BlockStatus.Active)
            .AsQueryable();

        // Access control: Curators see only their blocks
        if (!isAdmin)
        {
            var userBlockIds = await _context.Set<BlockCurator>()
                .Where(bc => bc.UserId == userId)
                .Select(bc => bc.BlockId)
                .Distinct()
                .ToListAsync();

            query = query.Where(i => userBlockIds.Contains(i.Contact.BlockId));
        }

        // Apply filters
        if (contactId.HasValue)
            query = query.Where(i => i.ContactId == contactId.Value);

        if (blockId.HasValue)
            query = query.Where(i => i.Contact.BlockId == blockId.Value);

        if (fromDate.HasValue)
            query = query.Where(i => i.InteractionDate >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(i => i.InteractionDate <= toDate.Value);

        if (interactionTypeId.HasValue)
            query = query.Where(i => i.InteractionTypeId == interactionTypeId.Value);

        if (resultId.HasValue)
            query = query.Where(i => i.ResultId == resultId.Value);

        var total = await query.CountAsync();

        var interactions = await query
            .OrderByDescending(i => i.InteractionDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (interactions, total);
    }

    public async Task<Interaction?> GetInteractionByIdAsync(int id, int userId, bool isAdmin)
    {
        var interaction = await _context.Set<Interaction>()
            .Include(i => i.Contact)
                .ThenInclude(c => c.Block)
            .Include(i => i.Curator)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (interaction == null)
            return null;

        // Access control
        if (!isAdmin)
        {
            var hasAccess = await _context.Set<BlockCurator>()
                .AnyAsync(bc => bc.BlockId == interaction.Contact.BlockId && bc.UserId == userId);

            if (!hasAccess)
                return null;
        }

        return interaction;
    }

    public async Task<Interaction> CreateInteractionAsync(
        int contactId,
        int userId,
        bool isAdmin,
        DateTime? interactionDate = null,
        int? interactionTypeId = null,
        int? resultId = null,
        string? comment = null,
        string? statusChangeJson = null,
        string? attachmentsJson = null,
        DateTime? nextTouchDate = null)
    {
        var contact = await _context.Set<Contact>()
            .Include(c => c.Block)
            .FirstOrDefaultAsync(c => c.Id == contactId);

        if (contact == null)
        {
            throw new ArgumentException("Contact not found", nameof(contactId));
        }

        // Access control
        if (!isAdmin)
        {
            var hasAccess = await _context.Set<BlockCurator>()
                .AnyAsync(bc => bc.BlockId == contact.BlockId && bc.UserId == userId);

            if (!hasAccess)
            {
                throw new UnauthorizedAccessException("User does not have access to this contact's block");
            }
        }

        var interaction = new Interaction
        {
            ContactId = contactId,
            InteractionDate = interactionDate ?? DateTime.UtcNow,
            InteractionTypeId = interactionTypeId,
            CuratorId = userId,
            ResultId = resultId,
            CommentEncrypted = !string.IsNullOrEmpty(comment)
                ? _encryptionService.Encrypt(comment)
                : null,
            StatusChangeJson = statusChangeJson,
            AttachmentsJson = attachmentsJson,
            NextTouchDate = nextTouchDate,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = userId
        };

        _context.Set<Interaction>().Add(interaction);

        // Update contact's last interaction date and next touch date
        contact.LastInteractionDate = interaction.InteractionDate;
        if (nextTouchDate.HasValue)
        {
            contact.NextTouchDate = nextTouchDate.Value;
        }
        contact.UpdatedAt = DateTime.UtcNow;
        contact.UpdatedBy = userId;

        // If status changed, record it
        if (!string.IsNullOrEmpty(statusChangeJson))
        {
            await RecordStatusChangeAsync(contactId, userId, statusChangeJson);
        }

        // Audit log for interaction
        var auditLog = new AuditLog
        {
            UserId = userId,
            Action = AuditActionType.Create,
            EntityType = "Interaction",
            EntityId = interaction.Id.ToString(),
            Timestamp = DateTime.UtcNow,
            NewValuesJson = $"Contact: {contact.ContactId}, Type: {interactionTypeId}"
        };
        _context.Set<AuditLog>().Add(auditLog);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Interaction created for contact {ContactId} by user {UserId}",
            contact.ContactId, userId);

        return interaction;
    }

    public async Task UpdateInteractionAsync(
        int id,
        int userId,
        bool isAdmin,
        DateTime? interactionDate = null,
        int? interactionTypeId = null,
        int? resultId = null,
        string? comment = null,
        string? statusChangeJson = null,
        DateTime? nextTouchDate = null)
    {
        var interaction = await _context.Set<Interaction>()
            .Include(i => i.Contact)
                .ThenInclude(c => c.Block)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (interaction == null)
        {
            throw new ArgumentException("Interaction not found", nameof(id));
        }

        // Access control
        if (!isAdmin)
        {
            var hasAccess = await _context.Set<BlockCurator>()
                .AnyAsync(bc => bc.BlockId == interaction.Contact.BlockId && bc.UserId == userId);

            if (!hasAccess)
            {
                throw new UnauthorizedAccessException("User does not have access to this interaction");
            }
        }

        interaction.InteractionDate = interactionDate ?? interaction.InteractionDate;
        interaction.InteractionTypeId = interactionTypeId;
        interaction.ResultId = resultId;
        interaction.CommentEncrypted = !string.IsNullOrEmpty(comment)
            ? _encryptionService.Encrypt(comment)
            : null;
        interaction.StatusChangeJson = statusChangeJson;
        interaction.NextTouchDate = nextTouchDate;
        interaction.UpdatedAt = DateTime.UtcNow;
        interaction.UpdatedBy = userId;

        // Update contact if needed
        if (nextTouchDate.HasValue)
        {
            interaction.Contact.NextTouchDate = nextTouchDate.Value;
            interaction.Contact.UpdatedAt = DateTime.UtcNow;
            interaction.Contact.UpdatedBy = userId;
        }

        var auditLog = new AuditLog
        {
            UserId = userId,
            Action = AuditActionType.Update,
            EntityType = "Interaction",
            EntityId = interaction.Id.ToString(),
            Timestamp = DateTime.UtcNow,
            NewValuesJson = $"Updated interaction for {interaction.Contact.ContactId}"
        };
        _context.Set<AuditLog>().Add(auditLog);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Interaction {Id} updated by user {UserId}", id, userId);
    }

    public async Task DeactivateInteractionAsync(int id, int userId)
    {
        var interaction = await _context.Set<Interaction>()
            .Include(i => i.Contact)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (interaction == null)
        {
            throw new ArgumentException("Interaction not found", nameof(id));
        }

        // Soft delete
        interaction.IsActive = false;
        interaction.UpdatedAt = DateTime.UtcNow;
        interaction.UpdatedBy = userId;

        var auditLog = new AuditLog
        {
            UserId = userId,
            Action = AuditActionType.Delete,
            EntityType = "Interaction",
            EntityId = interaction.Id.ToString(),
            Timestamp = DateTime.UtcNow,
            OldValuesJson = $"Contact: {interaction.Contact.ContactId}"
        };
        _context.Set<AuditLog>().Add(auditLog);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Interaction {Id} deactivated by user {UserId}", id, userId);
    }

    public async Task<IEnumerable<Interaction>> GetRecentInteractionsAsync(int userId, bool isAdmin, int count = 5)
    {
        var query = _context.Set<Interaction>()
            .Include(i => i.Contact)
                .ThenInclude(c => c.Block)
            .Include(i => i.Curator)
            .Where(i => i.IsActive)
            .AsQueryable();

        if (!isAdmin)
        {
            var userBlockIds = await _context.Set<BlockCurator>()
                .Where(bc => bc.UserId == userId)
                .Select(bc => bc.BlockId)
                .Distinct()
                .ToListAsync();

            query = query.Where(i => userBlockIds.Contains(i.Contact.BlockId));
        }

        return await query
            .OrderByDescending(i => i.InteractionDate)
            .Take(count)
            .ToListAsync();
    }

    public async Task RecordStatusChangeAsync(int contactId, int userId, string statusChangeJson)
    {
        var contact = await _context.Set<Contact>().FindAsync(contactId);
        if (contact == null)
        {
            throw new ArgumentException("Contact not found", nameof(contactId));
        }

        try
        {
            var statusChange = System.Text.Json.JsonDocument.Parse(statusChangeJson);
            if (statusChange.RootElement.TryGetProperty("newStatus", out var newStatusElement))
            {
                var newStatusStr = newStatusElement.GetString();
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
                    _context.Set<InfluenceStatusHistory>().Add(statusHistory);

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
                    _context.Set<AuditLog>().Add(auditLog);
                }
            }
        }
        catch (System.Text.Json.JsonException ex)
        {
            _logger.LogWarning("Invalid status change JSON: {StatusChangeJson}. Error: {Error}",
                statusChangeJson, ex.Message);
        }
    }
}
