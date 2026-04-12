namespace secure_workflow_system.Data;

public class CaseStatusHistory
{
    public int Id { get; set; }

    public int CaseId { get; set; }

    public Case Case { get; set; } = default!;

    public string OldStatus { get; set; } = string.Empty;

    public string NewStatus { get; set; } = string.Empty;

    public string ChangedByUserId { get; set; } = string.Empty;

    public ApplicationUser ChangedByUser { get; set; } = default!;

    public DateTime ChangedAtUtc { get; set; } = DateTime.UtcNow;
}
