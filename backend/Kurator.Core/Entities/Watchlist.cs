using Kurator.Core.Enums;

namespace Kurator.Core.Entities;

/// <summary>
/// Реестр потенциальных угроз (Watchlist)
/// ВАЖНО: Данные НЕ шифруются (доступ только у администраторов и аналитиков угроз)
/// </summary>
public class Watchlist
{
    public int Id { get; set; }

    /// <summary>
    /// ФИО / псевдоним (НЕ шифруется)
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Роль / статус (например: журналист, бывший партнёр)
    /// </summary>
    public string? RoleStatus { get; set; }

    /// <summary>
    /// ID сферы риска из справочника
    /// </summary>
    public int? RiskSphereId { get; set; }

    /// <summary>
    /// Источник угрозы
    /// </summary>
    public string? ThreatSource { get; set; }

    /// <summary>
    /// Дата возникновения конфликта
    /// </summary>
    public DateTime? ConflictDate { get; set; }

    /// <summary>
    /// Уровень риска
    /// </summary>
    public RiskLevel RiskLevel { get; set; }

    /// <summary>
    /// Периодичность мониторинга
    /// </summary>
    public MonitoringFrequency MonitoringFrequency { get; set; }

    /// <summary>
    /// Дата последней проверки
    /// </summary>
    public DateTime? LastCheckDate { get; set; }

    /// <summary>
    /// Дата следующей проверки
    /// </summary>
    public DateTime? NextCheckDate { get; set; }

    /// <summary>
    /// Ход / динамика (текущая ситуация)
    /// </summary>
    public string? DynamicsDescription { get; set; }

    /// <summary>
    /// Ответственный наблюдатель (аналитик угроз)
    /// </summary>
    public int? WatchOwnerId { get; set; }

    /// <summary>
    /// JSON массив с путями к прикрепленным материалам (НЕ шифруются)
    /// </summary>
    public string? AttachmentsJson { get; set; }

    /// <summary>
    /// Статус записи (активный / деактивированный)
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Дата создания
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Дата последнего обновления
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// ID пользователя, внёсшего последнее изменение
    /// </summary>
    public int UpdatedBy { get; set; }

    // Navigation properties
    public virtual User? WatchOwner { get; set; }
    public virtual User UpdatedByUser { get; set; } = null!;
    public virtual ReferenceValue? RiskSphere { get; set; }
    public virtual ICollection<WatchlistHistory> History { get; set; } = new List<WatchlistHistory>();
}
