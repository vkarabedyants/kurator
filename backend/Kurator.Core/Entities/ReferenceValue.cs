namespace Kurator.Core.Entities;

/// <summary>
/// Справочник управляемых значений
/// </summary>
public class ReferenceValue
{
    public int Id { get; set; }

    /// <summary>
    /// Категория справочника (influence_status, influence_type, etc.)
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Код значения (A, B, C, D для статусов влияния)
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Отображаемое название
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Описание значения
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Порядок отображения
    /// </summary>
    public int SortOrder { get; set; } = 0;

    /// <summary>
    /// Статус (активно / деактивировано)
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
}
