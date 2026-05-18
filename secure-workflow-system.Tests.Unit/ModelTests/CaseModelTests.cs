using Xunit;
using secure_workflow_system.Data;

namespace secure_workflow_system.Tests.Unit.ModelTests;

public class CaseModelTests
{
    #region IsValidTransition Tests

    [Theory]
    [InlineData(WorkflowState.New, WorkflowState.Assigned, true)]
    [InlineData(WorkflowState.Assigned, WorkflowState.InProgress, true)]
    [InlineData(WorkflowState.InProgress, WorkflowState.Resolved, true)]
    [InlineData(WorkflowState.Resolved, WorkflowState.Closed, true)]
    [InlineData(WorkflowState.Resolved, WorkflowState.InProgress, true)]
    public void IsValidTransition_WithValidTransitions_ShouldReturnTrue(WorkflowState from, WorkflowState to, bool expected)
    {
        // Act
        var result = Case.IsValidTransition(from, to);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(WorkflowState.New, WorkflowState.InProgress)]
    [InlineData(WorkflowState.New, WorkflowState.Resolved)]
    [InlineData(WorkflowState.New, WorkflowState.Closed)]
    [InlineData(WorkflowState.Assigned, WorkflowState.New)]
    [InlineData(WorkflowState.Assigned, WorkflowState.Closed)]
    [InlineData(WorkflowState.Closed, WorkflowState.InProgress)]
    public void IsValidTransition_WithInvalidTransitions_ShouldReturnFalse(WorkflowState from, WorkflowState to)
    {
        // Act
        var result = Case.IsValidTransition(from, to);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Case Creation Tests

    [Fact]
    public void Case_ShouldHaveDefaultStatus()
    {
        // Act
        var caseObj = new Case();

        // Assert
        Assert.Equal(WorkflowState.New, caseObj.Status);
    }

    [Fact]
    public void Case_ShouldHaveCreatedAtUtcDefaultValue()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow;

        // Act
        var caseObj = new Case();
        var afterCreation = DateTime.UtcNow;

        // Assert
        Assert.True(caseObj.CreatedAtUtc >= beforeCreation && caseObj.CreatedAtUtc <= afterCreation);
    }

    [Fact]
    public void Case_ShouldAllowNullAssignment()
    {
        // Act
        var caseObj = new Case { AssignedToUserId = null };

        // Assert
        Assert.Null(caseObj.AssignedToUserId);
    }

    #endregion
}
