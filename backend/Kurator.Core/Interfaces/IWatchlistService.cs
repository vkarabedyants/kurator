using Kurator.Core.Entities;
using Kurator.Core.Enums;

namespace Kurator.Core.Interfaces;

public interface IWatchlistService
{
    Task<(IEnumerable<Watchlist> Items, int Total)> GetWatchlistItemsAsync(
        RiskLevel? riskLevel = null,
        int? riskSphereId = null,
        MonitoringFrequency? monitoringFrequency = null,
        int? watchOwnerId = null,
        bool? requiresCheck = null,
        int page = 1,
        int pageSize = 50);

    Task<Watchlist?> GetWatchlistItemByIdAsync(int id);

    Task<Watchlist> CreateWatchlistItemAsync(
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
        string? attachmentsJson = null);

    Task UpdateWatchlistItemAsync(
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
        string? attachmentsJson = null);

    Task DeleteWatchlistItemAsync(int id, int userId);

    Task RecordCheckAsync(
        int id,
        int userId,
        DateTime? nextCheckDate = null,
        string? dynamicsUpdate = null,
        RiskLevel? newRiskLevel = null);

    Task<IEnumerable<Watchlist>> GetItemsRequiringCheckAsync();

    Task<WatchlistStatistics> GetStatisticsAsync();
}

public class WatchlistStatistics
{
    public int Total { get; set; }
    public int RequiresCheck { get; set; }
    public Dictionary<string, int> ByRiskLevel { get; set; } = new();
    public Dictionary<int?, int> ByRiskSphere { get; set; } = new();
    public Dictionary<string, int> ByMonitoringFrequency { get; set; } = new();
}
