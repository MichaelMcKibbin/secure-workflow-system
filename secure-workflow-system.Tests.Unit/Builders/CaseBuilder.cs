using secure_workflow_system.Data;

namespace secure_workflow_system.Tests.Unit.Builders;

/// <summary>
/// Fluent builder for creating test Case objects
/// </summary>
public class CaseBuilder
{
    private Case _case = new();

    public CaseBuilder()
    {
        _case = new Case
        {
            Id = 1,
            Title = "Test Case",
            Description = "Test Description",
            Status = WorkflowState.New,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = Guid.NewGuid().ToString(),
            CreatedByUser = new ApplicationUser { Id = Guid.NewGuid().ToString(), UserName = "testuser" }
        };
    }

    public CaseBuilder WithId(int id)
    {
        _case.Id = id;
        return this;
    }

    public CaseBuilder WithTitle(string title)
    {
        _case.Title = title;
        return this;
    }

    public CaseBuilder WithDescription(string description)
    {
        _case.Description = description;
        return this;
    }

    public CaseBuilder WithStatus(WorkflowState status)
    {
        _case.Status = status;
        return this;
    }

    public CaseBuilder WithCreatedBy(string userId, string userName = "testuser")
    {
        _case.CreatedByUserId = userId;
        _case.CreatedByUser = new ApplicationUser { Id = userId, UserName = userName };
        return this;
    }

    public CaseBuilder WithAssignedTo(string? userId, string? userName = null)
    {
        _case.AssignedToUserId = userId;
        if (userId != null)
        {
            _case.AssignedToUser = new ApplicationUser { Id = userId, UserName = userName ?? "assigned-user" };
        }
        return this;
    }

    public CaseBuilder WithCreatedAt(DateTime createdAt)
    {
        _case.CreatedAtUtc = createdAt;
        return this;
    }

    public CaseBuilder WithUpdatedAt(DateTime? updatedAt)
    {
        _case.UpdatedAtUtc = updatedAt;
        return this;
    }

    public Case Build()
    {
        return _case;
    }
}
