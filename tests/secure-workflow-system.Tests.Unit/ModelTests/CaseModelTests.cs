using secure_workflow_system.Data;
using Xunit;

namespace secure_workflow_system.Tests.Unit.ModelTests;

public class CaseModelTests
{
    #region IsValidTransition - Valid

    [Theory]
    [InlineData(WorkflowState.New, WorkflowState.Assigned)]
    [InlineData(WorkflowState.Assigned, WorkflowState.InProgress)]
    [InlineData(WorkflowState.InProgress, WorkflowState.Resolved)]
    [InlineData(WorkflowState.Resolved, WorkflowState.Closed)]
    [InlineData(WorkflowState.Resolved, WorkflowState.InProgress)]
    public void IsValidTransition_WithValidTransition_ReturnsTrue(WorkflowState from, WorkflowState to)
    {
        // Act
        var result = Case.IsValidTransition(from, to);

        // Assert
        Assert.True(result);
    }

    #endregion

    #region IsValidTransition - Invalid

    [Theory]
    [InlineData(WorkflowState.New, WorkflowState.New)]
    [InlineData(WorkflowState.New, WorkflowState.InProgress)]
    [InlineData(WorkflowState.New, WorkflowState.Resolved)]
    [InlineData(WorkflowState.New, WorkflowState.Closed)]
    [InlineData(WorkflowState.Assigned, WorkflowState.New)]
    [InlineData(WorkflowState.Assigned, WorkflowState.Assigned)]
    [InlineData(WorkflowState.Assigned, WorkflowState.Resolved)]
    [InlineData(WorkflowState.Assigned, WorkflowState.Closed)]
    [InlineData(WorkflowState.InProgress, WorkflowState.New)]
    [InlineData(WorkflowState.InProgress, WorkflowState.Assigned)]
    [InlineData(WorkflowState.InProgress, WorkflowState.InProgress)]
    [InlineData(WorkflowState.InProgress, WorkflowState.Closed)]
    [InlineData(WorkflowState.Resolved, WorkflowState.New)]
    [InlineData(WorkflowState.Resolved, WorkflowState.Assigned)]
    [InlineData(WorkflowState.Resolved, WorkflowState.Resolved)]
    [InlineData(WorkflowState.Closed, WorkflowState.New)]
    [InlineData(WorkflowState.Closed, WorkflowState.Assigned)]
    [InlineData(WorkflowState.Closed, WorkflowState.InProgress)]
    [InlineData(WorkflowState.Closed, WorkflowState.Resolved)]
    [InlineData(WorkflowState.Closed, WorkflowState.Closed)]
    public void IsValidTransition_WithInvalidTransition_ReturnsFalse(WorkflowState from, WorkflowState to)
    {
        // Act
        var result = Case.IsValidTransition(from, to);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Case Default Property Values

    [Fact]
    public void Case_DefaultStatus_IsNew()
    {
        // Act
        var caseObj = new Case();

        // Assert
        Assert.Equal(WorkflowState.New, caseObj.Status);
    }

    [Fact]
    public void Case_DefaultTitle_IsEmptyString()
    {
        // Act
        var caseObj = new Case();

        // Assert
        Assert.Equal(string.Empty, caseObj.Title);
    }

    [Fact]
    public void Case_DefaultDescription_IsEmptyString()
    {
        // Act
        var caseObj = new Case();

        // Assert
        Assert.Equal(string.Empty, caseObj.Description);
    }

    [Fact]
    public void Case_DefaultAssignedToUserId_IsNull()
    {
        // Act
        var caseObj = new Case();

        // Assert
        Assert.Null(caseObj.AssignedToUserId);
    }

    [Fact]
    public void Case_DefaultUpdatedAtUtc_IsNull()
    {
        // Act
        var caseObj = new Case();

        // Assert
        Assert.Null(caseObj.UpdatedAtUtc);
    }

    [Fact]
    public void Case_DefaultCreatedAtUtc_IsApproximatelyNow()
    {
        // Arrange
        var before = DateTime.UtcNow;

        // Act
        var caseObj = new Case();
        var after = DateTime.UtcNow;

        // Assert
        Assert.InRange(caseObj.CreatedAtUtc, before, after);
    }

    #endregion
}
