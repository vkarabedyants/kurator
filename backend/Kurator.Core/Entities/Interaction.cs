namespace Kurator.Core.Entities;

/// <summary>
/// Касание (взаимодействие) с контактом
/// </summary>
public class Interaction
{
    public int Id { get; set; }

    /// <summary>
    /// ID контакта
    /// </summary>
    public int ContactId { get; set; }

    /// <summary>
    /// Дата и время взаимодействия
    /// </summary>
    public DateTime InteractionDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// ID типа касания из справочника
    /// </summary>
    public int? InteractionTypeId { get; set; }

    /// <summary>
    /// ID куратора, создавшего касание
    /// </summary>
    public int CuratorId { get; set; }

    /// <summary>
    /// ID результата из справочника
    /// </summary>
    public int? ResultId { get; set; }

    /// <summary>
    /// Комментарий / детали (зашифровано RSA)
    /// </summary>
    public string? CommentEncrypted { get; set; }

    /// <summary>
    /// JSON с информацией об изменении статуса (если было)
    /// { "oldStatus": "B", "newStatus": "A" }
    /// </summary>
    public string? StatusChangeJson { get; set; }

    /// <summary>
    /// JSON массив с путями к файлам (зашифрованным)
    /// </summary>
    public string? AttachmentsJson { get; set; }

    /// <summary>
    /// Дата следующего планового касания
    /// </summary>
    public DateTime? NextTouchDate { get; set; }

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
    public virtual Contact Contact { get; set; } = null!;
    public virtual User Curator { get; set; } = null!;
    public virtual User UpdatedByUser { get; set; } = null!;
    public virtual ReferenceValue? InteractionType { get; set; }
    public virtual ReferenceValue? Result { get; set; }
}
