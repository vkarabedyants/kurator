namespace Kurator.Core.Interfaces;

public interface IDashboardService
{
    Task<CuratorDashboardMetrics> GetCuratorDashboardAsync(int userId, bool isAdmin);

    Task<AdminDashboardMetrics> GetAdminDashboardAsync();

    Task<InteractionStatistics> GetStatisticsAsync(
        int userId,
        bool isAdmin,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int? blockId = null);
}

public record CuratorDashboardMetrics
{
    public int TotalContacts { get; init; }
    public int InteractionsLastMonth { get; init; }
    public double AverageInteractionInterval { get; init; }
    public int OverdueContacts { get; init; }
    public List<RecentInteractionSummary> RecentInteractions { get; init; } = new();
    public List<AttentionContact> ContactsRequiringAttention { get; init; } = new();
    public Dictionary<string, int> ContactsByInfluenceStatus { get; init; } = new();
    public Dictionary<string, int> InteractionsByType { get; init; } = new();
}

public record AdminDashboardMetrics
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
    public List<AuditLogSummary> RecentAuditLogs { get; init; } = new();
}

public record InteractionStatistics
{
    public DateTime FromDate { get; init; }
    public DateTime ToDate { get; init; }
    public int TotalInteractions { get; init; }
    public int UniqueContacts { get; init; }
    public Dictionary<string, int> ByType { get; init; } = new();
    public Dictionary<string, int> ByResult { get; init; } = new();
}

public record RecentInteractionSummary
{
    public int Id { get; init; }
    public string ContactName { get; init; } = string.Empty;
    public string ContactId { get; init; } = string.Empty;
    public DateTime InteractionDate { get; init; }
    public int? InteractionTypeId { get; init; }
    public int? ResultId { get; init; }
}

public record AttentionContact
{
    public int Id { get; init; }
    public string ContactId { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public DateTime? NextTouchDate { get; init; }
    public int DaysOverdue { get; init; }
    public string InfluenceStatus { get; init; } = string.Empty;
}

public record AuditLogSummary
{
    public int Id { get; init; }
    public string UserLogin { get; init; } = string.Empty;
    public string ActionType { get; init; } = string.Empty;
    public string EntityType { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
}
