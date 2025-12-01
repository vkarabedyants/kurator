using System;
using System.Collections.Generic;

namespace Kurator.Core.DTOs;

public class ContactListDto
{
    public int Id { get; set; }
    public string ContactId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public int BlockId { get; set; }
    public string BlockName { get; set; } = string.Empty;
    public string BlockCode { get; set; } = string.Empty;
    public int? OrganizationId { get; set; }
    public string? Position { get; set; }
    public int? InfluenceStatusId { get; set; }
    public int? InfluenceTypeId { get; set; }
    public DateTime? LastInteractionDate { get; set; }
    public DateTime? NextTouchDate { get; set; }
    public int? ResponsibleCuratorId { get; set; }
    public string? ResponsibleCuratorLogin { get; set; }
    public bool IsOverdue => NextTouchDate.HasValue && NextTouchDate.Value < DateTime.UtcNow;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class ContactDetailDto
{
    public int Id { get; set; }
    public string ContactId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public int BlockId { get; set; }
    public string BlockName { get; set; } = string.Empty;
    public string BlockCode { get; set; } = string.Empty;
    public int? OrganizationId { get; set; }
    public string? Position { get; set; }
    public int? InfluenceStatusId { get; set; }
    public int? InfluenceTypeId { get; set; }
    public string? UsefulnessDescription { get; set; }
    public int? CommunicationChannelId { get; set; }
    public int? ContactSourceId { get; set; }
    public DateTime? LastInteractionDate { get; set; }
    public DateTime? NextTouchDate { get; set; }
    public string? Notes { get; set; }
    public int? ResponsibleCuratorId { get; set; }
    public string? ResponsibleCuratorLogin { get; set; }
    public List<InteractionListDto> RecentInteractions { get; set; } = new List<InteractionListDto>();
    public List<StatusHistoryDto> StatusHistory { get; set; } = new List<StatusHistoryDto>();
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}

public class CreateContactRequest
{
    public int BlockId { get; set; }
    public EncryptedFieldDto FullNameEncrypted { get; set; } = new EncryptedFieldDto();
    public int? OrganizationId { get; set; }
    public string? Position { get; set; }
    public int? InfluenceStatusId { get; set; }
    public int? InfluenceTypeId { get; set; }
    public string? UsefulnessDescription { get; set; }
    public int? CommunicationChannelId { get; set; }
    public int? ContactSourceId { get; set; }
    public DateTime? NextTouchDate { get; set; }
    public EncryptedFieldDto? NotesEncrypted { get; set; }
    public int? ResponsibleCuratorId { get; set; }
}

public class UpdateContactRequest
{
    public EncryptedFieldDto? FullNameEncrypted { get; set; }
    public int? OrganizationId { get; set; }
    public string? Position { get; set; }
    public int? InfluenceStatusId { get; set; }
    public int? InfluenceTypeId { get; set; }
    public string? UsefulnessDescription { get; set; }
    public int? CommunicationChannelId { get; set; }
    public int? ContactSourceId { get; set; }
    public DateTime? NextTouchDate { get; set; }
    public EncryptedFieldDto? NotesEncrypted { get; set; }
    public int? ResponsibleCuratorId { get; set; }
}

public class InteractionListDto
{
    public int Id { get; set; }
    public DateTime InteractionDate { get; set; }
    public int? InteractionTypeId { get; set; }
    public int? ResultId { get; set; }
    public string? Comment { get; set; }
    public int CuratorId { get; set; }
    public string CuratorLogin { get; set; } = string.Empty;
}

public class StatusHistoryDto
{
    public int Id { get; set; }
    public string? PreviousStatus { get; set; }
    public string NewStatus { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
    public int ChangedByUserId { get; set; }
    public string ChangedByLogin { get; set; } = string.Empty;
}