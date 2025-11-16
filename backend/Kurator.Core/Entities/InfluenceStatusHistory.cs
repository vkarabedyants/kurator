namespace Kurator.Core.Entities;

public class InfluenceStatusHistory
{
    public int Id { get; set; }
    public int ContactId { get; set; }
    public string PreviousStatus { get; set; } = string.Empty;
    public string NewStatus { get; set; } = string.Empty;
    public int ChangedByUserId { get; set; }
    public DateTime ChangedAt { get; set; }

    // Navigation properties
    public Contact Contact { get; set; } = null!;
    public User ChangedBy { get; set; } = null!;
}
