namespace Kurator.Core.Entities;

/// <summary>
/// FAQ / Правила работы
/// </summary>
public class FAQ
{
    public int Id { get; set; }

    /// <summary>
    /// Заголовок пункта FAQ
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Содержание (rich text или markdown)
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Порядок отображения
    /// </summary>
    public int SortOrder { get; set; } = 0;

    /// <summary>
    /// Статус (активный / деактивированный)
    /// Все активные FAQ видны всем пользователям
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
    public int? UpdatedBy { get; set; }

    // Navigation properties
    public virtual User? UpdatedByUser { get; set; }
}
