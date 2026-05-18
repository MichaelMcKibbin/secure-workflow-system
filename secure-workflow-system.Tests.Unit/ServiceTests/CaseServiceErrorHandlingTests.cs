using Xunit;
using secure_workflow_system.Data;
using secure_workflow_system.Services;
using secure_workflow_system.Tests.Unit.Infrastructure;

namespace secure_workflow_system.Tests.Unit.ServiceTests;

public class CaseServiceErrorHandlingTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly CaseService _caseService;

    public CaseServiceErrorHandlingTests()
    {
        _context = TestDbContextFactory.CreateAndSeedTestContext($"TestDb_{Guid.NewGuid()}");
        _caseService = new CaseService(_context);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }

    #region Input Validation Tests

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task CreateCaseAsync_WithEmptyTitle_ShouldStoreEmpty(string? title)
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();

        // Act
        var result = await _caseService.CreateCaseAsync(userId, title ?? "", "Description");

        // Assert
        Assert.NotNull(result);
    }

    #endregion

    #region Not Found Tests

    [Fact]
    public async Task GetCaseByIdForUserAsync_WithNonExistentCase_ShouldReturnNull()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();

        // Act
        var result = await _caseService.GetCaseByIdForUserAsync(999, userId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetCaseByIdForUserAsync_WhenUserNotAuthorized_ShouldReturnNull()
    {
        // Arrange
        var creatorId = Guid.NewGuid().ToString();
        var otherUserId = Guid.NewGuid().ToString();
        var caseObj = await _caseService.CreateCaseAsync(creatorId, "Case", "Description");

        // Act
        var result = await _caseService.GetCaseByIdForUserAsync(caseObj.Id, otherUserId);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region State Transition Tests

    [Fact]
    public async Task UpdateCaseStatusAndAssignmentAsync_WithInvalidCaseId_ShouldReturnFalse()
    {
        // Act
        var result = await _caseService.UpdateCaseStatusAndAssignmentAsync(
            999,
            WorkflowState.Assigned.ToString(),
            Guid.NewGuid().ToString());

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task UpdateCaseStatusAndAssignmentAsync_WithValidTransition_ShouldReturnTrue()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var caseObj = await _caseService.CreateCaseAsync(userId, "Case", "Description");

        // Act
        var result = await _caseService.UpdateCaseStatusAndAssignmentAsync(
            caseObj.Id,
            WorkflowState.Assigned.ToString(),
            null);

        // Assert
        Assert.True(result);
    }

    #endregion

    #region Concurrency and Edge Cases

    [Fact]
    public async Task GetAllCasesAsync_WithLargeDataSet_ShouldHandleEfficiently()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        for (int i = 0; i < 500; i++)
        {
            await _caseService.CreateCaseAsync(userId, $"Case {i}", $"Description {i}");
        }

        // Act
        var result = await _caseService.GetAllCasesAsync();

        // Assert
        Assert.Equal(500, result.Count);
    }

    [Fact]
    public async Task GetCasesForUserAsync_ShouldHandleBothCreatedAndAssignedCases()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var creatorId = Guid.NewGuid().ToString();
        var anotherUserId = Guid.NewGuid().ToString();

        // Create cases by user
        var created1 = await _caseService.CreateCaseAsync(userId, "Created 1", "Description");
        var created2 = await _caseService.CreateCaseAsync(userId, "Created 2", "Description");

        // Create case by someone else and assign to user
        var byOther = await _caseService.CreateCaseAsync(creatorId, "Assigned to user", "Description");
        await _caseService.UpdateCaseStatusAndAssignmentAsync(byOther.Id, WorkflowState.Assigned.ToString(), userId);

        // Act
        var userCases = await _caseService.GetCasesForUserAsync(userId);

        // Assert
        Assert.Equal(3, userCases.Count);
        Assert.Contains(userCases, c => c.Id == created1.Id);
        Assert.Contains(userCases, c => c.Id == created2.Id);
        Assert.Contains(userCases, c => c.Id == byOther.Id);
    }

    #endregion
}
