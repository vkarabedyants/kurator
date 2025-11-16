using Kurator.Core.Enums;

namespace Kurator.Core.Entities;

/// <summary>
/// Блок (сектор) системы
/// </summary>
public class Block
{
    public int Id { get; set; }

    /// <summary>
    /// Название блока (например: "ОП", "Медиа")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Код блока для генерации ID контактов (например: OP, MEDIA)
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Описание блока (опционально)
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Статус блока (активный или архивный)
    /// </summary>
    public BlockStatus Status { get; set; } = BlockStatus.Active;

    /// <summary>
    /// Дата создания
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Дата последнего обновления
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    /// <summary>
    /// Связь с кураторами через таблицу BlockCurator
    /// </summary>
    public ICollection<BlockCurator> CuratorAssignments { get; set; } = new List<BlockCurator>();

    public ICollection<Contact> Contacts { get; set; } = new List<Contact>();
}
