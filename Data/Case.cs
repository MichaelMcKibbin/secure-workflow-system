using System.ComponentModel.DataAnnotations;

namespace secure_workflow_system.Data;

public class Case
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(4000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public WorkflowState Status { get; set; } = WorkflowState.New;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }

    [Required]
    public string CreatedByUserId { get; set; } = string.Empty;

    public ApplicationUser CreatedByUser { get; set; } = default!;

    public string? AssignedToUserId { get; set; }

    public ApplicationUser? AssignedToUser { get; set; }

    /// <summary>
    /// Validates if a transition from one workflow state to another is allowed.
    /// </summary>
    public static bool IsValidTransition(WorkflowState from, WorkflowState to)
    {
        return (from, to) switch
        {
            (WorkflowState.New, WorkflowState.Assigned) => true,
            (WorkflowState.Assigned, WorkflowState.InProgress) => true,
            (WorkflowState.InProgress, WorkflowState.Resolved) => true,
            (WorkflowState.Resolved, WorkflowState.Closed) => true,
            (WorkflowState.Resolved, WorkflowState.InProgress) => true,
            _ => false
        };
    }
}
