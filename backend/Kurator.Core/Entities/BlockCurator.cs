using Kurator.Core.Enums;

namespace Kurator.Core.Entities;

/// <summary>
/// Таблица связи кураторов с блоками.
/// Определяет статус назначения куратора в блоке (основной или резервный).
/// </summary>
public class BlockCurator
{
    public int Id { get; set; }

    /// <summary>
    /// ID блока
    /// </summary>
    public int BlockId { get; set; }

    /// <summary>
    /// ID пользователя (куратора)
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Тип куратора (основной или резервный)
    /// </summary>
    public CuratorType CuratorType { get; set; }

    /// <summary>
    /// Дата назначения
    /// </summary>
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Кто назначил (ID администратора)
    /// </summary>
    public int? AssignedBy { get; set; }

    // Navigation properties
    public virtual Block Block { get; set; } = null!;
    public virtual User User { get; set; } = null!;
    public virtual User? AssignedByUser { get; set; }
}
