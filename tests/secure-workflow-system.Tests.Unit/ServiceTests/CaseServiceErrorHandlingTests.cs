using secure_workflow_system.Data;
using secure_workflow_system.Services;
using secure_workflow_system.Tests.Unit.Infrastructure;
using Xunit;

namespace secure_workflow_system.Tests.Unit.ServiceTests;

public class CaseServiceErrorHandlingTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly CaseService _service;

    public CaseServiceErrorHandlingTests()
    {
        _context = TestDbContextFactory.Create($"TestDb_{Guid.NewGuid()}");
        _service = new CaseService(_context);
    }

    public void Dispose() => _context.Dispose();

    #region Input Validation

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateCaseAsync_WithWhitespaceTitle_StoresEmptyString(string title)
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();

        // Act
        var result = await _service.CreateCaseAsync(userId, title, "Description");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(string.Empty, result.Title);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateCaseAsync_WithWhitespaceDescription_StoresEmptyString(string description)
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();

        // Act
        var result = await _service.CreateCaseAsync(userId, "Title", description);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(string.Empty, result.Description);
    }

    [Fact]
    public async Task CreateCaseAsync_WithWhitespaceUserId_StillCreatesCase()
    {
        // Act
        var result = await _service.CreateCaseAsync("   ", "Title", "Description");

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Id > 0);
    }

    #endregion

    #region Not Found

    [Fact]
    public async Task GetCaseByIdAsync_WithNonExistentId_ReturnsNull()
    {
        // Act
        var result = await _service.GetCaseByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetCaseByIdForUserAsync_WithNonExistentCaseId_ReturnsNull()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();

        // Act
        var result = await _service.GetCaseByIdForUserAsync(999, userId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetCaseByIdForUserAsync_WithUnauthorisedUser_ReturnsNull()
    {
        // Arrange
        var creatorId = Guid.NewGuid().ToString();
        var otherUserId = Guid.NewGuid().ToString();
        var created = await _service.CreateCaseAsync(creatorId, "Case", "Description");

        // Act
        var result = await _service.GetCaseByIdForUserAsync(created.Id, otherUserId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetCasesForUserAsync_WithUserWhoHasNoCases_ReturnsEmptyList()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();

        // Act
        var result = await _service.GetCasesForUserAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllCasesAsync_WithNoCases_ReturnsEmptyList()
    {
        // Act
        var result = await _service.GetAllCasesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetCaseStatusHistoryAsync_WithNonExistentCaseId_ReturnsEmptyList()
    {
        // Act
        var result = await _service.GetCaseStatusHistoryAsync(999);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetCaseStatusHistoryAsync_WithNoStatusChanges_ReturnsEmptyList()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var created = await _service.CreateCaseAsync(userId, "Case", "Description");

        // Act
        var result = await _service.GetCaseStatusHistoryAsync(created.Id);

        // Assert
        Assert.Empty(result);
    }

    #endregion

    #region Invalid State Transitions

    [Fact]
    public async Task UpdateCaseStatusAndAssignmentAsync_WithNonExistentCaseId_ReturnsFalse()
    {
        // Act
        var result = await _service.UpdateCaseStatusAndAssignmentAsync(
            999, WorkflowState.Assigned.ToString(), null);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task UpdateCaseStatusAndAssignmentAsync_WithInvalidStatusString_ReturnsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var created = await _service.CreateCaseAsync(userId, "Case", "Description");

        // Act
        var result = await _service.UpdateCaseStatusAndAssignmentAsync(
            created.Id, "NotAValidStatus", null);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData(WorkflowState.New, WorkflowState.InProgress)]
    [InlineData(WorkflowState.New, WorkflowState.Resolved)]
    [InlineData(WorkflowState.New, WorkflowState.Closed)]
    [InlineData(WorkflowState.Assigned, WorkflowState.New)]
    [InlineData(WorkflowState.Assigned, WorkflowState.Closed)]
    [InlineData(WorkflowState.Closed, WorkflowState.New)]
    [InlineData(WorkflowState.Closed, WorkflowState.InProgress)]
    public async Task UpdateCaseStatusAndAssignmentAsync_WithInvalidTransition_ReturnsFalse(
        WorkflowState fromStatus, WorkflowState toStatus)
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var created = await _service.CreateCaseAsync(userId, "Case", "Description");

        // Advance to fromStatus via valid transitions
        await AdvanceToStatusAsync(created.Id, userId, fromStatus);

        // Act
        var result = await _service.UpdateCaseStatusAndAssignmentAsync(
            created.Id, toStatus.ToString(), null);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData(WorkflowState.New, WorkflowState.Assigned)]
    [InlineData(WorkflowState.Assigned, WorkflowState.InProgress)]
    [InlineData(WorkflowState.InProgress, WorkflowState.Resolved)]
    [InlineData(WorkflowState.Resolved, WorkflowState.Closed)]
    [InlineData(WorkflowState.Resolved, WorkflowState.InProgress)]
    public async Task UpdateCaseStatusAndAssignmentAsync_WithValidTransition_DoesNotChangeStatusOnFalse(
        WorkflowState fromStatus, WorkflowState toStatus)
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var created = await _service.CreateCaseAsync(userId, "Case", "Description");
        await AdvanceToStatusAsync(created.Id, userId, fromStatus);

        // Act
        var result = await _service.UpdateCaseStatusAndAssignmentAsync(
            created.Id, toStatus.ToString(), null);
        var updated = await _service.GetCaseByIdAsync(created.Id);

        // Assert
        Assert.True(result);
        Assert.Equal(toStatus, updated!.Status);
    }

    [Fact]
    public async Task UpdateCaseStatusAndAssignmentAsync_WithSameStatus_ReturnsTrueWithoutAddingHistory()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var created = await _service.CreateCaseAsync(userId, "Case", "Description");

        // Act — update to same status (New -> New)
        var result = await _service.UpdateCaseStatusAndAssignmentAsync(
            created.Id, WorkflowState.New.ToString(), null);
        var history = await _service.GetCaseStatusHistoryAsync(created.Id);

        // Assert
        Assert.True(result);
        Assert.Empty(history);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task UpdateCaseStatusAndAssignmentAsync_RemovingAssignment_ClearsAssignedToUserId()
    {
        // Arrange
        var creatorId = Guid.NewGuid().ToString();
        var assigneeId = Guid.NewGuid().ToString();
        var created = await _service.CreateCaseAsync(creatorId, "Case", "Description");
        await _service.UpdateCaseStatusAndAssignmentAsync(
            created.Id, WorkflowState.Assigned.ToString(), assigneeId);

        // Act — advance to InProgress with no assigned user
        await _service.UpdateCaseStatusAndAssignmentAsync(
            created.Id, WorkflowState.InProgress.ToString(), null);
        var updated = await _service.GetCaseByIdAsync(created.Id);

        // Assert
        Assert.Null(updated!.AssignedToUserId);
    }

    [Fact]
    public async Task UpdateCaseStatusAndAssignmentAsync_MultipleTransitions_TracksFullHistory()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var created = await _service.CreateCaseAsync(userId, "Case", "Description");

        // Act — walk through full workflow
        await _service.UpdateCaseStatusAndAssignmentAsync(created.Id, WorkflowState.Assigned.ToString(), null);
        await _service.UpdateCaseStatusAndAssignmentAsync(created.Id, WorkflowState.InProgress.ToString(), null);
        await _service.UpdateCaseStatusAndAssignmentAsync(created.Id, WorkflowState.Resolved.ToString(), null);
        await _service.UpdateCaseStatusAndAssignmentAsync(created.Id, WorkflowState.Closed.ToString(), null);

        var history = await _service.GetCaseStatusHistoryAsync(created.Id);

        // Assert
        Assert.Equal(4, history.Count);
    }

    [Fact]
    public async Task GetCasesForUserAsync_ReturnsBothCreatedAndAssignedCases()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var creatorId = Guid.NewGuid().ToString();

        var ownCase1 = await _service.CreateCaseAsync(userId, "Own Case 1", "Description");
        var ownCase2 = await _service.CreateCaseAsync(userId, "Own Case 2", "Description");
        var assignedCase = await _service.CreateCaseAsync(creatorId, "Assigned Case", "Description");
        await _service.UpdateCaseStatusAndAssignmentAsync(
            assignedCase.Id, WorkflowState.Assigned.ToString(), userId);

        // Act
        var result = await _service.GetCasesForUserAsync(userId);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Contains(result, c => c.Id == ownCase1.Id);
        Assert.Contains(result, c => c.Id == ownCase2.Id);
        Assert.Contains(result, c => c.Id == assignedCase.Id);
    }

    [Fact]
    public async Task GetCasesForUserAsync_DoesNotReturnOtherUsersCases()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var otherUserId = Guid.NewGuid().ToString();
        await _service.CreateCaseAsync(otherUserId, "Other Case 1", "Description");
        await _service.CreateCaseAsync(otherUserId, "Other Case 2", "Description");

        // Act
        var result = await _service.GetCasesForUserAsync(userId);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllCasesAsync_WithLargeDataset_ReturnsAllCases()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        for (var i = 0; i < 500; i++)
        {
            await _service.CreateCaseAsync(userId, $"Case {i}", $"Description {i}");
        }

        // Act
        var result = await _service.GetAllCasesAsync();

        // Assert
        Assert.Equal(500, result.Count);
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Advances a case to the target status via valid transitions.
    /// </summary>
    private async Task AdvanceToStatusAsync(int caseId, string userId, WorkflowState targetStatus)
    {
        var transitions = new[]
        {
            WorkflowState.Assigned,
            WorkflowState.InProgress,
            WorkflowState.Resolved,
            WorkflowState.Closed
        };

        foreach (var state in transitions)
        {
            if (state > targetStatus) break;
            await _service.UpdateCaseStatusAndAssignmentAsync(caseId, state.ToString(), null);
            if (state == targetStatus) break;
        }
    }

    #endregion
}
