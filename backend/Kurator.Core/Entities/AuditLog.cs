using Kurator.Core.Enums;

namespace Kurator.Core.Entities;

/// <summary>
/// Журнал аудита всех действий пользователей
/// </summary>
public class AuditLog
{
    public long Id { get; set; }

    /// <summary>
    /// ID пользователя, совершившего действие
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Тип действия (Create, Update, Delete, StatusChange)
    /// </summary>
    public AuditActionType Action { get; set; }

    /// <summary>
    /// Тип сущности (Contact, Interaction, Block, FAQ, User, Watchlist)
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// ID изменяемого объекта
    /// </summary>
    public string EntityId { get; set; } = string.Empty;

    /// <summary>
    /// Старые значения (JSONB формат, NULL для CREATE операций)
    /// </summary>
    public string? OldValuesJson { get; set; }

    /// <summary>
    /// Новые значения (JSONB формат)
    /// </summary>
    public string? NewValuesJson { get; set; }

    /// <summary>
    /// Время действия
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual User User { get; set; } = null!;
}
