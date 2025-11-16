namespace Kurator.Core.Entities;

/// <summary>
/// Контакт в системе
/// </summary>
public class Contact
{
    /// <summary>
    /// Технический ID (автоинкремент)
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Идентификатор контакта в формате BLOCKCODE-### (например: OP-001)
    /// </summary>
    public string ContactId { get; set; } = string.Empty;

    /// <summary>
    /// ID блока, к которому принадлежит контакт
    /// </summary>
    public int BlockId { get; set; }

    /// <summary>
    /// ФИО (зашифровано RSA)
    /// </summary>
    public string FullNameEncrypted { get; set; } = string.Empty;

    /// <summary>
    /// ID организации из справочника
    /// </summary>
    public int? OrganizationId { get; set; }

    /// <summary>
    /// Роль / должность
    /// </summary>
    public string? Position { get; set; }

    /// <summary>
    /// ID статуса влияния из справочника (A/B/C/D)
    /// </summary>
    public int? InfluenceStatusId { get; set; }

    /// <summary>
    /// ID типа влияния из справочника
    /// </summary>
    public int? InfluenceTypeId { get; set; }

    /// <summary>
    /// Чем может быть полезен
    /// </summary>
    public string? UsefulnessDescription { get; set; }

    /// <summary>
    /// ID канала коммуникации из справочника
    /// </summary>
    public int? CommunicationChannelId { get; set; }

    /// <summary>
    /// ID источника контакта из справочника
    /// </summary>
    public int? ContactSourceId { get; set; }

    /// <summary>
    /// Дата последнего касания (автоматически обновляется)
    /// </summary>
    public DateTime? LastInteractionDate { get; set; }

    /// <summary>
    /// Дата следующего планового касания
    /// </summary>
    public DateTime? NextTouchDate { get; set; }

    /// <summary>
    /// Примечания (зашифрованы RSA)
    /// </summary>
    public string? NotesEncrypted { get; set; }

    /// <summary>
    /// Ответственный куратор
    /// </summary>
    public int ResponsibleCuratorId { get; set; }

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
    public virtual Block Block { get; set; } = null!;
    public virtual User ResponsibleCurator { get; set; } = null!;
    public virtual User UpdatedByUser { get; set; } = null!;

    public virtual ReferenceValue? Organization { get; set; }
    public virtual ReferenceValue? InfluenceStatus { get; set; }
    public virtual ReferenceValue? InfluenceType { get; set; }
    public virtual ReferenceValue? CommunicationChannel { get; set; }
    public virtual ReferenceValue? ContactSource { get; set; }

    public virtual ICollection<Interaction> Interactions { get; set; } = new List<Interaction>();
    public virtual ICollection<InfluenceStatusHistory> StatusHistory { get; set; } = new List<InfluenceStatusHistory>();
}
