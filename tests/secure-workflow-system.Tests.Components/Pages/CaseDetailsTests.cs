using Bunit;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using secure_workflow_system.Components.Pages;
using secure_workflow_system.Data;
using secure_workflow_system.Services;
using secure_workflow_system.Tests.Components.Infrastructure;
using Xunit;

namespace secure_workflow_system.Tests.Components.Pages;

public class CaseDetailsTests : TestContext
{
    private readonly Mock<ICaseService> _caseServiceMock = new();
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;

    private readonly Case _testCase = new()
    {
        Id = 1,
        Title = "Test Case Title",
        Description = "Test Case Description",
        Status = WorkflowState.New,
        CreatedAtUtc = new DateTime(2025, 1, 15, 10, 0, 0, DateTimeKind.Utc),
        CreatedByUserId = "user-1",
        CreatedByUser = new ApplicationUser { Id = "user-1", UserName = "testuser" }
    };

    public CaseDetailsTests()
    {
        _userManagerMock = UserManagerMockHelper.CreateMock();

        Services.AddSingleton(_caseServiceMock.Object);
        Services.AddSingleton(_userManagerMock.Object);
        Services.AddCascadingAuthenticationState();

        _caseServiceMock
            .Setup(s => s.GetCaseStatusHistoryAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<CaseStatusHistory>());
    }

    #region Case Information Display

    [Fact]
    public void CaseDetails_AsUser_DisplaysCaseTitle()
    {
        // Arrange
        this.AddAuthenticatedUser("user-1", "testuser", "User");
        _caseServiceMock
            .Setup(s => s.GetCaseByIdForUserAsync(1, "user-1"))
            .ReturnsAsync(_testCase);

        // Act
        var cut = Render<CaseDetails>(p => p.Add(c => c.Id, 1));

        // Assert
        Assert.Contains("Test Case Title", cut.Markup);
    }

    [Fact]
    public void CaseDetails_AsUser_DisplaysCaseDescription()
    {
        // Arrange
        this.AddAuthenticatedUser("user-1", "testuser", "User");
        _caseServiceMock
            .Setup(s => s.GetCaseByIdForUserAsync(1, "user-1"))
            .ReturnsAsync(_testCase);

        // Act
        var cut = Render<CaseDetails>(p => p.Add(c => c.Id, 1));

        // Assert
        Assert.Contains("Test Case Description", cut.Markup);
    }

    [Fact]
    public void CaseDetails_AsUser_DisplaysCreatedByUsername()
    {
        // Arrange
        this.AddAuthenticatedUser("user-1", "testuser", "User");
        _caseServiceMock
            .Setup(s => s.GetCaseByIdForUserAsync(1, "user-1"))
            .ReturnsAsync(_testCase);

        // Act
        var cut = Render<CaseDetails>(p => p.Add(c => c.Id, 1));

        // Assert
        Assert.Contains("testuser", cut.Markup);
    }

    [Fact]
    public void CaseDetails_WithNoAssignedUser_DisplaysUnassigned()
    {
        // Arrange
        this.AddAuthenticatedUser("user-1", "testuser", "User");
        _caseServiceMock
            .Setup(s => s.GetCaseByIdForUserAsync(1, "user-1"))
            .ReturnsAsync(_testCase);

        // Act
        var cut = Render<CaseDetails>(p => p.Add(c => c.Id, 1));

        // Assert
        Assert.Contains("Unassigned", cut.Markup);
    }

    [Fact]
    public void CaseDetails_WithAssignedUser_DisplaysAssigneeUsername()
    {
        // Arrange
        this.AddAuthenticatedUser("user-1", "testuser", "User");
        var assignedCase = new Case
        {
            Id = 1,
            Title = "Case",
            Description = "Desc",
            Status = WorkflowState.Assigned,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = "user-1",
            CreatedByUser = new ApplicationUser { Id = "user-1", UserName = "testuser" },
            AssignedToUserId = "staff-1",
            AssignedToUser = new ApplicationUser { Id = "staff-1", UserName = "staffuser" }
        };
        _caseServiceMock
            .Setup(s => s.GetCaseByIdForUserAsync(1, "user-1"))
            .ReturnsAsync(assignedCase);

        // Act
        var cut = Render<CaseDetails>(p => p.Add(c => c.Id, 1));

        // Assert
        Assert.Contains("staffuser", cut.Markup);
    }

    [Fact]
    public void CaseDetails_WithUpdatedAtUtc_DisplaysLastUpdated()
    {
        // Arrange
        this.AddAuthenticatedUser("user-1", "testuser", "User");
        var updatedCase = new Case
        {
            Id = 1,
            Title = "Case",
            Description = "Desc",
            Status = WorkflowState.New,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = new DateTime(2025, 6, 1, 12, 0, 0, DateTimeKind.Utc),
            CreatedByUserId = "user-1",
            CreatedByUser = new ApplicationUser { Id = "user-1", UserName = "testuser" }
        };
        _caseServiceMock
            .Setup(s => s.GetCaseByIdForUserAsync(1, "user-1"))
            .ReturnsAsync(updatedCase);

        // Act
        var cut = Render<CaseDetails>(p => p.Add(c => c.Id, 1));

        // Assert
        Assert.Contains("Last Updated", cut.Markup);
    }

    [Fact]
    public void CaseDetails_DisplaysStatusBadge()
    {
        // Arrange
        this.AddAuthenticatedUser("user-1", "testuser", "User");
        _caseServiceMock
            .Setup(s => s.GetCaseByIdForUserAsync(1, "user-1"))
            .ReturnsAsync(_testCase);

        // Act
        var cut = Render<CaseDetails>(p => p.Add(c => c.Id, 1));

        // Assert
        Assert.Contains("New", cut.Markup);
    }

    #endregion

    #region Not Found / Error States

    [Fact]
    public void CaseDetails_WhenCaseNotFound_ShowsNotFoundMessage()
    {
        // Arrange
        this.AddAuthenticatedUser("user-1", "testuser", "User");
        _caseServiceMock
            .Setup(s => s.GetCaseByIdForUserAsync(999, "user-1"))
            .ReturnsAsync((Case?)null);

        // Act
        var cut = Render<CaseDetails>(p => p.Add(c => c.Id, 999));

        // Assert
        Assert.Contains("Case not found", cut.Markup);
    }

    [Fact]
    public void CaseDetails_WhenServiceThrows_ShowsErrorMessage()
    {
        // Arrange
        this.AddAuthenticatedUser("user-1", "testuser", "User");
        _caseServiceMock
            .Setup(s => s.GetCaseByIdForUserAsync(1, "user-1"))
            .ThrowsAsync(new Exception("DB error"));

        // Act
        var cut = Render<CaseDetails>(p => p.Add(c => c.Id, 1));

        // Assert
        Assert.Contains("Unable to load this case", cut.Markup);
    }

    #endregion

    #region Role-Based Access

    [Fact]
    public void CaseDetails_AsUser_DoesNotShowManagePanel()
    {
        // Arrange
        this.AddAuthenticatedUser("user-1", "testuser", "User");
        _caseServiceMock
            .Setup(s => s.GetCaseByIdForUserAsync(1, "user-1"))
            .ReturnsAsync(_testCase);

        // Act
        var cut = Render<CaseDetails>(p => p.Add(c => c.Id, 1));

        // Assert
        Assert.DoesNotContain("Manage Case", cut.Markup);
    }

    [Fact]
    public void CaseDetails_AsStaff_ShowsManagePanel()
    {
        // Arrange
        this.AddAuthenticatedUser("staff-1", "staffuser", "Staff");
        _userManagerMock.WithNoUsersInAnyRole();
        _caseServiceMock
            .Setup(s => s.GetCaseByIdAsync(1))
            .ReturnsAsync(_testCase);

        // Act
        var cut = Render<CaseDetails>(p => p.Add(c => c.Id, 1));

        // Assert
        Assert.Contains("Manage Case", cut.Markup);
    }

    [Fact]
    public void CaseDetails_AsAdmin_ShowsManagePanel()
    {
        // Arrange
        this.AddAuthenticatedUser("admin-1", "adminuser", "Admin");
        _userManagerMock.WithNoUsersInAnyRole();
        _caseServiceMock
            .Setup(s => s.GetCaseByIdAsync(1))
            .ReturnsAsync(_testCase);

        // Act
        var cut = Render<CaseDetails>(p => p.Add(c => c.Id, 1));

        // Assert
        Assert.Contains("Manage Case", cut.Markup);
    }

    [Fact]
    public void CaseDetails_AsUser_CallsGetCaseByIdForUserAsync()
    {
        // Arrange
        this.AddAuthenticatedUser("user-1", "testuser", "User");
        _caseServiceMock
            .Setup(s => s.GetCaseByIdForUserAsync(1, "user-1"))
            .ReturnsAsync(_testCase);

        // Act
        Render<CaseDetails>(p => p.Add(c => c.Id, 1));

        // Assert
        _caseServiceMock.Verify(s => s.GetCaseByIdForUserAsync(1, "user-1"), Times.Once);
        _caseServiceMock.Verify(s => s.GetCaseByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public void CaseDetails_AsStaff_CallsGetCaseByIdAsync()
    {
        // Arrange
        this.AddAuthenticatedUser("staff-1", "staffuser", "Staff");
        _userManagerMock.WithNoUsersInAnyRole();
        _caseServiceMock
            .Setup(s => s.GetCaseByIdAsync(1))
            .ReturnsAsync(_testCase);

        // Act
        Render<CaseDetails>(p => p.Add(c => c.Id, 1));

        // Assert
        _caseServiceMock.Verify(s => s.GetCaseByIdAsync(1), Times.Once);
        _caseServiceMock.Verify(s => s.GetCaseByIdForUserAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    #endregion

    #region Manage Panel

    [Fact]
    public void CaseDetails_AsStaff_ManagePanel_ShowsAssignableUsers()
    {
        // Arrange
        this.AddAuthenticatedUser("staff-1", "staffuser", "Staff");
        var assignableUser = new ApplicationUser { Id = "user-2", UserName = "assignable@test.com", Email = "assignable@test.com" };
        _userManagerMock
            .WithUsersInRole("User", new List<ApplicationUser> { assignableUser })
            .WithUsersInRole("Staff", new List<ApplicationUser>())
            .WithUsersInRole("Admin", new List<ApplicationUser>());
        _caseServiceMock
            .Setup(s => s.GetCaseByIdAsync(1))
            .ReturnsAsync(_testCase);

        // Act
        var cut = Render<CaseDetails>(p => p.Add(c => c.Id, 1));

        // Assert
        Assert.Contains("assignable@test.com", cut.Markup);
    }

    [Fact]
    public void CaseDetails_AsStaff_ManagePanel_ShowsValidTransitionsOnly()
    {
        // Arrange — case is New, so only Assigned should be a valid transition
        this.AddAuthenticatedUser("staff-1", "staffuser", "Staff");
        _userManagerMock.WithNoUsersInAnyRole();
        _caseServiceMock
            .Setup(s => s.GetCaseByIdAsync(1))
            .ReturnsAsync(_testCase);

        // Act
        var cut = Render<CaseDetails>(p => p.Add(c => c.Id, 1));

        // Assert
        Assert.Contains("Assigned", cut.Markup);
        Assert.DoesNotContain("In Progress", cut.Markup);
    }

    [Fact]
    public async Task CaseDetails_AsStaff_SubmittingManageForm_CallsUpdateCaseStatusAndAssignmentAsync()
    {
        // Arrange
        this.AddAuthenticatedUser("staff-1", "staffuser", "Staff");
        _userManagerMock.WithNoUsersInAnyRole();
        _caseServiceMock
            .Setup(s => s.GetCaseByIdAsync(1))
            .ReturnsAsync(_testCase);
        _caseServiceMock
            .Setup(s => s.UpdateCaseStatusAndAssignmentAsync(1, "Assigned", null, "staff-1"))
            .ReturnsAsync(true);

        var cut = Render<CaseDetails>(p => p.Add(c => c.Id, 1));

        // Act
        cut.Find("form").Submit();
        await Task.Delay(50);

        // Assert
        _caseServiceMock.Verify(
            s => s.UpdateCaseStatusAndAssignmentAsync(1, It.IsAny<string>(), It.IsAny<string?>(), "staff-1"),
            Times.Once);
    }

    [Fact]
    public async Task CaseDetails_AfterSuccessfulUpdate_ShowsSuccessMessage()
    {
        // Arrange
        this.AddAuthenticatedUser("staff-1", "staffuser", "Staff");
        _userManagerMock.WithNoUsersInAnyRole();

        var updatedCase = new Case
        {
            Id = 1,
            Title = "Test Case Title",
            Description = "Desc",
            Status = WorkflowState.Assigned,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = "user-1",
            CreatedByUser = new ApplicationUser { Id = "user-1", UserName = "testuser" }
        };

        _caseServiceMock
            .SetupSequence(s => s.GetCaseByIdAsync(1))
            .ReturnsAsync(_testCase)
            .ReturnsAsync(updatedCase);
        _caseServiceMock
            .Setup(s => s.UpdateCaseStatusAndAssignmentAsync(1, It.IsAny<string>(), It.IsAny<string?>(), "staff-1"))
            .ReturnsAsync(true);

        var cut = Render<CaseDetails>(p => p.Add(c => c.Id, 1));

        // Act
        cut.Find("form").Submit();
        await Task.Delay(50);

        // Assert
        Assert.Contains("Case updated successfully", cut.Markup);
    }

    [Fact]
    public async Task CaseDetails_WhenUpdateReturnsFalse_ShowsErrorMessage()
    {
        // Arrange
        this.AddAuthenticatedUser("staff-1", "staffuser", "Staff");
        _userManagerMock.WithNoUsersInAnyRole();
        _caseServiceMock
            .Setup(s => s.GetCaseByIdAsync(1))
            .ReturnsAsync(_testCase);
        _caseServiceMock
            .Setup(s => s.UpdateCaseStatusAndAssignmentAsync(1, It.IsAny<string>(), It.IsAny<string?>(), "staff-1"))
            .ReturnsAsync(false);

        var cut = Render<CaseDetails>(p => p.Add(c => c.Id, 1));

        // Act
        cut.Find("form").Submit();
        await Task.Delay(50);

        // Assert
        Assert.Contains("invalid status transition", cut.Markup);
    }

    #endregion

    #region Status History

    [Fact]
    public void CaseDetails_WithNoHistory_ShowsNoChangesMessage()
    {
        // Arrange
        this.AddAuthenticatedUser("user-1", "testuser", "User");
        _caseServiceMock
            .Setup(s => s.GetCaseByIdForUserAsync(1, "user-1"))
            .ReturnsAsync(_testCase);

        // Act
        var cut = Render<CaseDetails>(p => p.Add(c => c.Id, 1));

        // Assert
        Assert.Contains("No status changes recorded yet", cut.Markup);
    }

    [Fact]
    public void CaseDetails_WithHistory_DisplaysHistoryTable()
    {
        // Arrange
        this.AddAuthenticatedUser("user-1", "testuser", "User");
        var history = new List<CaseStatusHistory>
        {
            new()
            {
                Id = 1, CaseId = 1,
                OldStatus = WorkflowState.New.ToString(),
                NewStatus = WorkflowState.Assigned.ToString(),
                ChangedByUserId = "staff-1",
                ChangedByUser = new ApplicationUser { Id = "staff-1", UserName = "staffuser" },
                ChangedAtUtc = DateTime.UtcNow
            }
        };
        _caseServiceMock
            .Setup(s => s.GetCaseByIdForUserAsync(1, "user-1"))
            .ReturnsAsync(_testCase);
        _caseServiceMock
            .Setup(s => s.GetCaseStatusHistoryAsync(1))
            .ReturnsAsync(history);

        // Act
        var cut = Render<CaseDetails>(p => p.Add(c => c.Id, 1));

        // Assert
        Assert.Contains("staffuser", cut.Markup);
        Assert.Contains("Assigned", cut.Markup);
    }

    [Fact]
    public void CaseDetails_WithHistory_DisplaysChangedByUsername()
    {
        // Arrange
        this.AddAuthenticatedUser("user-1", "testuser", "User");
        var history = new List<CaseStatusHistory>
        {
            new()
            {
                Id = 1, CaseId = 1,
                OldStatus = WorkflowState.New.ToString(),
                NewStatus = WorkflowState.Assigned.ToString(),
                ChangedByUserId = "staff-1",
                ChangedByUser = new ApplicationUser { Id = "staff-1", UserName = "the-changer" },
                ChangedAtUtc = DateTime.UtcNow
            }
        };
        _caseServiceMock
            .Setup(s => s.GetCaseByIdForUserAsync(1, "user-1"))
            .ReturnsAsync(_testCase);
        _caseServiceMock
            .Setup(s => s.GetCaseStatusHistoryAsync(1))
            .ReturnsAsync(history);

        // Act
        var cut = Render<CaseDetails>(p => p.Add(c => c.Id, 1));

        // Assert
        Assert.Contains("the-changer", cut.Markup);
    }

    #endregion
}
