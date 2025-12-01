using Kurator.Core.Entities;
using Kurator.Core.Enums;
using Kurator.Core.Interfaces;
using Kurator.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Kurator.Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _context;
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(
        ApplicationDbContext context,
        IEncryptionService encryptionService,
        ILogger<DashboardService> logger)
    {
        _context = context;
        _encryptionService = encryptionService;
        _logger = logger;
    }

    public async Task<CuratorDashboardMetrics> GetCuratorDashboardAsync(int userId, bool isAdmin)
    {
        // Get user's blocks
        var userBlockIds = await _context.Set<BlockCurator>()
            .Where(bc => bc.UserId == userId)
            .Select(bc => bc.BlockId)
            .Distinct()
            .ToListAsync();

        if (userBlockIds.Count == 0 && !isAdmin)
        {
            return new CuratorDashboardMetrics
            {
                TotalContacts = 0,
                InteractionsLastMonth = 0,
                AverageInteractionInterval = 0,
                OverdueContacts = 0,
                RecentInteractions = new List<RecentInteractionSummary>(),
                ContactsRequiringAttention = new List<AttentionContact>(),
                ContactsByInfluenceStatus = new Dictionary<string, int>(),
                InteractionsByType = new Dictionary<string, int>()
            };
        }

        // Total contacts in curator's blocks (only active blocks)
        var totalContacts = await _context.Set<Contact>()
            .Include(c => c.Block)
            .CountAsync(c => userBlockIds.Contains(c.BlockId) &&
                            c.IsActive &&
                            c.Block.Status == BlockStatus.Active);

        // Interactions in last month
        var lastMonth = DateTime.UtcNow.AddMonths(-1);
        var interactionsLastMonth = await _context.Set<Interaction>()
            .Include(i => i.Contact)
                .ThenInclude(c => c.Block)
            .CountAsync(i => userBlockIds.Contains(i.Contact.BlockId) &&
                            i.IsActive &&
                            i.Contact.Block.Status == BlockStatus.Active &&
                            i.InteractionDate >= lastMonth);

        // Average interval between interactions (days)
        var activeContacts = await _context.Set<Contact>()
            .Include(c => c.Block)
            .Where(c => userBlockIds.Contains(c.BlockId) &&
                       c.IsActive &&
                       c.Block.Status == BlockStatus.Active &&
                       c.LastInteractionDate.HasValue)
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
        var overdueContacts = await _context.Set<Contact>()
            .Include(c => c.Block)
            .CountAsync(c => userBlockIds.Contains(c.BlockId) &&
                            c.IsActive &&
                            c.Block.Status == BlockStatus.Active &&
                            c.NextTouchDate.HasValue &&
                            c.NextTouchDate.Value < DateTime.UtcNow);

        // Recent 5 interactions
        var recentInteractions = await _context.Set<Interaction>()
            .Include(i => i.Contact)
                .ThenInclude(c => c.Block)
            .Where(i => userBlockIds.Contains(i.Contact.BlockId) &&
                       i.IsActive &&
                       i.Contact.Block.Status == BlockStatus.Active)
            .OrderByDescending(i => i.InteractionDate)
            .Take(5)
            .Select(i => new RecentInteractionSummary
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
        var attentionContacts = await _context.Set<Contact>()
            .Include(c => c.Block)
            .Where(c => userBlockIds.Contains(c.BlockId) &&
                       c.IsActive &&
                       c.Block.Status == BlockStatus.Active &&
                       c.NextTouchDate.HasValue &&
                       c.NextTouchDate.Value < DateTime.UtcNow)
            .OrderBy(c => c.NextTouchDate)
            .Take(10)
            .Select(c => new AttentionContact
            {
                Id = c.Id,
                ContactId = c.ContactId,
                FullName = _encryptionService.Decrypt(c.FullNameEncrypted),
                NextTouchDate = c.NextTouchDate,
                DaysOverdue = (int)(DateTime.UtcNow - c.NextTouchDate!.Value).TotalDays,
                InfluenceStatus = c.InfluenceStatusId.HasValue
                    ? c.InfluenceStatusId.Value.ToString()
                    : "Unknown"
            })
            .ToListAsync();

        // Contacts by influence status
        var contactsByStatus = await _context.Set<Contact>()
            .Include(c => c.Block)
            .Where(c => userBlockIds.Contains(c.BlockId) &&
                       c.IsActive &&
                       c.Block.Status == BlockStatus.Active &&
                       c.InfluenceStatusId.HasValue)
            .GroupBy(c => c.InfluenceStatusId!.Value)
            .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count);

        // Interactions by type (last month)
        var interactionsByType = await _context.Set<Interaction>()
            .Include(i => i.Contact)
                .ThenInclude(c => c.Block)
            .Where(i => userBlockIds.Contains(i.Contact.BlockId) &&
                       i.IsActive &&
                       i.Contact.Block.Status == BlockStatus.Active &&
                       i.InteractionDate >= lastMonth &&
                       i.InteractionTypeId.HasValue)
            .GroupBy(i => i.InteractionTypeId!.Value)
            .Select(g => new { Type = g.Key.ToString(), Count = g.Count() })
            .ToDictionaryAsync(x => x.Type, x => x.Count);

        return new CuratorDashboardMetrics
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
    }

    public async Task<AdminDashboardMetrics> GetAdminDashboardAsync()
    {
        var totalContacts = await _context.Set<Contact>()
            .Include(c => c.Block)
            .CountAsync(c => c.IsActive && c.Block.Status == BlockStatus.Active);

        var totalInteractions = await _context.Set<Interaction>()
            .Include(i => i.Contact)
                .ThenInclude(c => c.Block)
            .CountAsync(i => i.IsActive && i.Contact.Block.Status == BlockStatus.Active);

        var totalBlocks = await _context.Set<Block>()
            .CountAsync(b => b.Status == BlockStatus.Active);

        var totalUsers = await _context.Set<User>().CountAsync();

        var lastMonth = DateTime.UtcNow.AddMonths(-1);
        var newContactsLastMonth = await _context.Set<Contact>()
            .Include(c => c.Block)
            .CountAsync(c => c.IsActive &&
                            c.Block.Status == BlockStatus.Active &&
                            c.CreatedAt >= lastMonth);

        var interactionsLastMonth = await _context.Set<Interaction>()
            .Include(i => i.Contact)
                .ThenInclude(c => c.Block)
            .CountAsync(i => i.IsActive &&
                            i.Contact.Block.Status == BlockStatus.Active &&
                            i.InteractionDate >= lastMonth);

        // Contacts by block
        var contactsByBlock = await _context.Set<Contact>()
            .Include(c => c.Block)
            .Where(c => c.IsActive && c.Block.Status == BlockStatus.Active)
            .GroupBy(c => c.Block.Name)
            .Select(g => new { Block = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToDictionaryAsync(x => x.Block, x => x.Count);

        // Contacts by influence status (all)
        var contactsByStatus = await _context.Set<Contact>()
            .Include(c => c.Block)
            .Where(c => c.IsActive &&
                       c.Block.Status == BlockStatus.Active &&
                       c.InfluenceStatusId.HasValue)
            .GroupBy(c => c.InfluenceStatusId!.Value)
            .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count);

        // Contacts by influence type
        var contactsByType = await _context.Set<Contact>()
            .Include(c => c.Block)
            .Where(c => c.IsActive &&
                       c.Block.Status == BlockStatus.Active &&
                       c.InfluenceTypeId.HasValue)
            .GroupBy(c => c.InfluenceTypeId!.Value)
            .Select(g => new { Type = g.Key.ToString(), Count = g.Count() })
            .ToDictionaryAsync(x => x.Type, x => x.Count);

        // Interactions by block (last month)
        var interactionsByBlock = await _context.Set<Interaction>()
            .Include(i => i.Contact)
                .ThenInclude(c => c.Block)
            .Where(i => i.IsActive &&
                       i.Contact.Block.Status == BlockStatus.Active &&
                       i.InteractionDate >= lastMonth)
            .GroupBy(i => i.Contact.Block.Name)
            .Select(g => new { Block = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToDictionaryAsync(x => x.Block, x => x.Count);

        // Top curators by activity (last month)
        var topCurators = await _context.Set<Interaction>()
            .Include(i => i.Curator)
            .Include(i => i.Contact)
                .ThenInclude(c => c.Block)
            .Where(i => i.IsActive &&
                       i.Contact.Block.Status == BlockStatus.Active &&
                       i.InteractionDate >= lastMonth)
            .GroupBy(i => i.Curator.Login)
            .Select(g => new { Curator = g.Key, InteractionCount = g.Count() })
            .OrderByDescending(x => x.InteractionCount)
            .Take(5)
            .ToDictionaryAsync(x => x.Curator, x => x.InteractionCount);

        // Status change dynamics (last 3 months)
        var threeMonthsAgo = DateTime.UtcNow.AddMonths(-3);
        var statusChanges = await _context.Set<InfluenceStatusHistory>()
            .Where(h => h.ChangedAt >= threeMonthsAgo)
            .GroupBy(h => new { h.PreviousStatus, h.NewStatus })
            .Select(g => new
            {
                Transition = g.Key.PreviousStatus + "→" + g.Key.NewStatus,
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .Take(10)
            .ToDictionaryAsync(x => x.Transition, x => x.Count);

        // Recent audit activity (last 20 entries)
        var recentAuditLogs = await _context.Set<AuditLog>()
            .Include(a => a.User)
            .OrderByDescending(a => a.Timestamp)
            .Take(20)
            .Select(a => new AuditLogSummary
            {
                Id = (int)a.Id,
                UserLogin = a.User.Login,
                ActionType = a.Action.ToString(),
                EntityType = a.EntityType,
                Timestamp = a.Timestamp
            })
            .ToListAsync();

        return new AdminDashboardMetrics
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
    }

    public async Task<InteractionStatistics> GetStatisticsAsync(
        int userId,
        bool isAdmin,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int? blockId = null)
    {
        fromDate ??= DateTime.UtcNow.AddMonths(-1);
        toDate ??= DateTime.UtcNow;

        var query = _context.Set<Interaction>()
            .Include(i => i.Contact)
            .Where(i => i.InteractionDate >= fromDate && i.InteractionDate <= toDate);

        // Access control
        if (!isAdmin)
        {
            var userBlockIds = await _context.Set<BlockCurator>()
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

        return new InteractionStatistics
        {
            FromDate = fromDate.Value,
            ToDate = toDate.Value,
            TotalInteractions = totalInteractions,
            UniqueContacts = uniqueContacts,
            ByType = byType,
            ByResult = byResult
        };
    }
}
