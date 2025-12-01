using Kurator.Core.Entities;
using Kurator.Core.Enums;
using Kurator.Core.Interfaces;
using Kurator.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Kurator.Infrastructure.Services;

public class ContactService : IContactService
{
    private readonly ApplicationDbContext _context;
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<ContactService> _logger;

    public ContactService(
        ApplicationDbContext context,
        IEncryptionService encryptionService,
        ILogger<ContactService> logger)
    {
        _context = context;
        _encryptionService = encryptionService;
        _logger = logger;
    }

    public async Task<(IEnumerable<Contact> Contacts, int Total)> GetContactsAsync(
        int userId,
        bool isAdmin,
        int? blockId = null,
        string? search = null,
        int? influenceStatusId = null,
        int? influenceTypeId = null,
        int? organizationId = null,
        int page = 1,
        int pageSize = 50)
    {
        var query = _context.Set<Contact>()
            .Include(c => c.Block)
            .Include(c => c.ResponsibleCurator)
            .Where(c => c.IsActive && c.Block.Status == BlockStatus.Active)
            .AsQueryable();

        // Access control: Curators see only their blocks
        if (!isAdmin)
        {
            var userBlockIds = await _context.Set<BlockCurator>()
                .Where(bc => bc.UserId == userId)
                .Select(bc => bc.BlockId)
                .ToListAsync();

            query = query.Where(c => userBlockIds.Contains(c.BlockId));
        }

        // Apply filters
        if (blockId.HasValue)
            query = query.Where(c => c.BlockId == blockId.Value);

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
            .ToListAsync();

        return (contacts, total);
    }

    public async Task<Contact?> GetContactByIdAsync(int id, int userId, bool isAdmin)
    {
        var contact = await _context.Set<Contact>()
            .Include(c => c.Block)
            .Include(c => c.ResponsibleCurator)
            .Include(c => c.Interactions.OrderByDescending(i => i.InteractionDate))
            .Include(c => c.StatusHistory.OrderByDescending(s => s.ChangedAt))
            .FirstOrDefaultAsync(c => c.Id == id && c.IsActive && c.Block.Status == BlockStatus.Active);

        if (contact == null)
            return null;

        // Access control
        if (!isAdmin)
        {
            var hasAccess = await HasAccessToContactAsync(id, userId, isAdmin);
            if (!hasAccess)
                return null;
        }

        return contact;
    }

    public async Task<Contact> CreateContactAsync(
        int blockId,
        string fullName,
        int userId,
        bool isAdmin,
        int? organizationId = null,
        string? position = null,
        int? influenceStatusId = null,
        int? influenceTypeId = null,
        string? usefulnessDescription = null,
        int? communicationChannelId = null,
        int? contactSourceId = null,
        DateTime? nextTouchDate = null,
        string? notes = null)
    {
        // Access control
        if (!isAdmin && !await HasAccessToBlockAsync(blockId, userId, isAdmin))
        {
            throw new UnauthorizedAccessException("User does not have access to this block");
        }

        // Validate block exists
        var block = await _context.Set<Block>().FindAsync(blockId);
        if (block == null)
        {
            throw new ArgumentException("Block not found", nameof(blockId));
        }

        // Generate ContactId
        var contactId = await GenerateContactIdAsync(blockId);

        // Create contact with encrypted fields
        var contact = new Contact
        {
            ContactId = contactId,
            BlockId = blockId,
            FullNameEncrypted = _encryptionService.Encrypt(fullName),
            OrganizationId = organizationId,
            Position = position,
            InfluenceStatusId = influenceStatusId,
            InfluenceTypeId = influenceTypeId,
            UsefulnessDescription = usefulnessDescription,
            CommunicationChannelId = communicationChannelId,
            ContactSourceId = contactSourceId,
            NextTouchDate = nextTouchDate,
            NotesEncrypted = !string.IsNullOrEmpty(notes)
                ? _encryptionService.Encrypt(notes)
                : null,
            ResponsibleCuratorId = userId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = userId
        };

        _context.Set<Contact>().Add(contact);

        // Create audit log
        var auditLog = new AuditLog
        {
            UserId = userId,
            Action = AuditActionType.Create,
            EntityType = "Contact",
            EntityId = contact.Id.ToString(),
            Timestamp = DateTime.UtcNow,
            NewValuesJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                ContactId = contactId,
                InfluenceStatusId = influenceStatusId
            })
        };
        _context.Set<AuditLog>().Add(auditLog);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Contact created: {ContactId} by user {UserId}", contactId, userId);

        return contact;
    }

    public async Task UpdateContactAsync(
        int id,
        int userId,
        bool isAdmin,
        int? organizationId = null,
        string? position = null,
        int? influenceStatusId = null,
        int? influenceTypeId = null,
        string? usefulnessDescription = null,
        int? communicationChannelId = null,
        int? contactSourceId = null,
        DateTime? nextTouchDate = null,
        string? notes = null)
    {
        var contact = await _context.Set<Contact>()
            .Include(c => c.Block)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (contact == null)
        {
            throw new ArgumentException("Contact not found", nameof(id));
        }

        // Access control
        if (!isAdmin && !await HasAccessToContactAsync(id, userId, isAdmin))
        {
            throw new UnauthorizedAccessException("User does not have access to this contact");
        }

        var oldStatusId = contact.InfluenceStatusId;
        var statusChanged = false;

        // Update fields - only update if value is explicitly provided
        if (organizationId.HasValue || organizationId == null)
            contact.OrganizationId = organizationId;
        if (position != null)
            contact.Position = position;
        if (influenceStatusId.HasValue)
        {
            if (contact.InfluenceStatusId != influenceStatusId.Value)
            {
                statusChanged = true;
                contact.InfluenceStatusId = influenceStatusId.Value;
            }
        }
        if (influenceTypeId.HasValue)
            contact.InfluenceTypeId = influenceTypeId.Value;
        if (usefulnessDescription != null)
            contact.UsefulnessDescription = usefulnessDescription;
        if (communicationChannelId.HasValue)
            contact.CommunicationChannelId = communicationChannelId.Value;
        if (contactSourceId.HasValue)
            contact.ContactSourceId = contactSourceId.Value;
        if (nextTouchDate.HasValue)
            contact.NextTouchDate = nextTouchDate.Value;
        if (notes != null)
            contact.NotesEncrypted = !string.IsNullOrEmpty(notes)
                ? _encryptionService.Encrypt(notes)
                : null;
        contact.UpdatedAt = DateTime.UtcNow;
        contact.UpdatedBy = userId;

        // Track influence status change
        if (statusChanged)
        {
            var statusHistory = new InfluenceStatusHistory
            {
                ContactId = contact.Id,
                PreviousStatus = oldStatusId?.ToString() ?? "null",
                NewStatus = influenceStatusId?.ToString() ?? "null",
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
                OldValuesJson = System.Text.Json.JsonSerializer.Serialize(new { InfluenceStatusId = oldStatusId }),
                NewValuesJson = System.Text.Json.JsonSerializer.Serialize(new { InfluenceStatusId = influenceStatusId })
            };
            _context.Set<AuditLog>().Add(auditLog);
        }
        else
        {
            var auditLog = new AuditLog
            {
                UserId = userId,
                Action = AuditActionType.Update,
                EntityType = "Contact",
                EntityId = contact.Id.ToString(),
                Timestamp = DateTime.UtcNow,
                NewValuesJson = System.Text.Json.JsonSerializer.Serialize(new { ContactId = contact.ContactId })
            };
            _context.Set<AuditLog>().Add(auditLog);
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Contact updated: {ContactId} by user {UserId}", contact.ContactId, userId);
    }

    public async Task DeleteContactAsync(int id, int userId)
    {
        var contact = await _context.Set<Contact>().FindAsync(id);
        if (contact == null)
        {
            throw new ArgumentException("Contact not found", nameof(id));
        }

        // Soft delete
        contact.IsActive = false;
        contact.UpdatedAt = DateTime.UtcNow;
        contact.UpdatedBy = userId;

        var auditLog = new AuditLog
        {
            UserId = userId,
            Action = AuditActionType.Delete,
            EntityType = "Contact",
            EntityId = contact.Id.ToString(),
            Timestamp = DateTime.UtcNow,
            OldValuesJson = System.Text.Json.JsonSerializer.Serialize(new { ContactId = contact.ContactId })
        };
        _context.Set<AuditLog>().Add(auditLog);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Contact deactivated: {ContactId} by user {UserId}", contact.ContactId, userId);
    }

    public async Task<IEnumerable<Contact>> GetOverdueContactsAsync(int userId, bool isAdmin)
    {
        var query = _context.Set<Contact>()
            .Include(c => c.Block)
            .Include(c => c.ResponsibleCurator)
            .Where(c => c.IsActive &&
                       c.Block.Status == BlockStatus.Active &&
                       c.NextTouchDate.HasValue &&
                       c.NextTouchDate.Value < DateTime.UtcNow);

        if (!isAdmin)
        {
            var userBlockIds = await _context.Set<BlockCurator>()
                .Where(bc => bc.UserId == userId)
                .Select(bc => bc.BlockId)
                .ToListAsync();

            query = query.Where(c => userBlockIds.Contains(c.BlockId));
        }

        return await query
            .OrderBy(c => c.NextTouchDate)
            .ToListAsync();
    }

    public async Task<bool> HasAccessToContactAsync(int contactId, int userId, bool isAdmin)
    {
        if (isAdmin)
            return true;

        var contact = await _context.Set<Contact>().FindAsync(contactId);
        if (contact == null)
            return false;

        return await HasAccessToBlockAsync(contact.BlockId, userId, isAdmin);
    }

    public async Task<bool> HasAccessToBlockAsync(int blockId, int userId, bool isAdmin)
    {
        if (isAdmin)
            return true;

        return await _context.Set<BlockCurator>()
            .AnyAsync(bc => bc.BlockId == blockId && bc.UserId == userId);
    }

    public async Task<string> GenerateContactIdAsync(int blockId)
    {
        var block = await _context.Set<Block>().FindAsync(blockId);
        if (block == null)
        {
            throw new ArgumentException("Block not found", nameof(blockId));
        }

        var lastContact = await _context.Set<Contact>()
            .Where(c => c.BlockId == blockId)
            .OrderByDescending(c => c.ContactId)
            .FirstOrDefaultAsync();

        int nextNumber = 1;
        if (lastContact != null)
        {
            var lastNumberStr = lastContact.ContactId.Split('-').Last();
            if (int.TryParse(lastNumberStr, out int lastNumber))
                nextNumber = lastNumber + 1;
        }

        return $"{block.Code}-{nextNumber:D3}";
    }
}
