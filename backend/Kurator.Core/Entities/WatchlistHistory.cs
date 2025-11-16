using Kurator.Core.Enums;

namespace Kurator.Core.Entities;

/// <summary>
/// История изменений уровня риска и других критичных полей в Watchlist
/// </summary>
public class WatchlistHistory
{
    public int Id { get; set; }

    /// <summary>
    /// ID записи в Watchlist
    /// </summary>
    public int WatchlistId { get; set; }

    /// <summary>
    /// Предыдущий уровень риска
    /// </summary>
    public RiskLevel? OldRiskLevel { get; set; }

    /// <summary>
    /// Новый уровень риска
    /// </summary>
    public RiskLevel? NewRiskLevel { get; set; }

    /// <summary>
    /// Кто внес изменение
    /// </summary>
    public int ChangedBy { get; set; }

    /// <summary>
    /// Дата и время изменения
    /// </summary>
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Комментарий к изменению
    /// </summary>
    public string? Comment { get; set; }

    // Navigation properties
    public virtual Watchlist Watchlist { get; set; } = null!;
    public virtual User ChangedByUser { get; set; } = null!;
}
