using Bunit;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using secure_workflow_system.Components.Pages;
using secure_workflow_system.Data;
using secure_workflow_system.Services;
using secure_workflow_system.Tests.Components.Infrastructure;
using Xunit;

namespace secure_workflow_system.Tests.Components.Pages;

public class MyCasesTests : TestContext
{
    private readonly Mock<ICaseService> _caseServiceMock = new();

    public MyCasesTests()
    {
        Services.AddSingleton(_caseServiceMock.Object);
        Services.AddCascadingAuthenticationState();
    }

    #region Heading

    [Fact]
    public void MyCases_AsUser_ShowsMysCasesHeading()
    {
        // Arrange
        this.AddAuthenticatedUser("user-1", "testuser", "User");
        _caseServiceMock
            .Setup(s => s.GetCasesForUserAsync("user-1"))
            .ReturnsAsync(new List<Case>());

        // Act
        var cut = Render<MyCases>();

        // Assert
        Assert.Contains("My Cases", cut.Markup);
    }

    [Fact]
    public void MyCases_AsStaff_ShowsAllCasesHeading()
    {
        // Arrange
        this.AddAuthenticatedUser("staff-1", "staffuser", "Staff");
        _caseServiceMock
            .Setup(s => s.GetAllCasesAsync())
            .ReturnsAsync(new List<Case>());

        // Act
        var cut = Render<MyCases>();

        // Assert
        Assert.Contains("All Cases", cut.Markup);
    }

    [Fact]
    public void MyCases_AsAdmin_ShowsAllCasesHeading()
    {
        // Arrange
        this.AddAuthenticatedUser("admin-1", "adminuser", "Admin");
        _caseServiceMock
            .Setup(s => s.GetAllCasesAsync())
            .ReturnsAsync(new List<Case>());

        // Act
        var cut = Render<MyCases>();

        // Assert
        Assert.Contains("All Cases", cut.Markup);
    }

    #endregion

    #region Empty State

    [Fact]
    public void MyCases_WithNoCases_ShowsNoCasesMessage()
    {
        // Arrange
        this.AddAuthenticatedUser("user-1", "testuser", "User");
        _caseServiceMock
            .Setup(s => s.GetCasesForUserAsync("user-1"))
            .ReturnsAsync(new List<Case>());

        // Act
        var cut = Render<MyCases>();

        // Assert
        Assert.Contains("No cases found", cut.Markup);
    }

    [Fact]
    public void MyCases_AsUserWithNoCases_ShowsAssignmentHint()
    {
        // Arrange
        this.AddAuthenticatedUser("user-1", "testuser", "User");
        _caseServiceMock
            .Setup(s => s.GetCasesForUserAsync("user-1"))
            .ReturnsAsync(new List<Case>());

        // Act
        var cut = Render<MyCases>();

        // Assert
        Assert.Contains("assigned to you", cut.Markup);
    }

    #endregion

    #region Data Display

    [Fact]
    public void MyCases_WithCases_DisplaysCaseTitles()
    {
        // Arrange
        this.AddAuthenticatedUser("user-1", "testuser", "User");
        var cases = new List<Case>
        {
            new() { Id = 1, Title = "First Case", Status = WorkflowState.New, CreatedAtUtc = DateTime.UtcNow, CreatedByUserId = "user-1" },
            new() { Id = 2, Title = "Second Case", Status = WorkflowState.Assigned, CreatedAtUtc = DateTime.UtcNow, CreatedByUserId = "user-1" }
        };
        _caseServiceMock
            .Setup(s => s.GetCasesForUserAsync("user-1"))
            .ReturnsAsync(cases);

        // Act
        var cut = Render<MyCases>();

        // Assert
        Assert.Contains("First Case", cut.Markup);
        Assert.Contains("Second Case", cut.Markup);
    }

    [Fact]
    public void MyCases_WithCases_DisplaysCaseIds()
    {
        // Arrange
        this.AddAuthenticatedUser("user-1", "testuser", "User");
        var cases = new List<Case>
        {
            new() { Id = 42, Title = "Case", Status = WorkflowState.New, CreatedAtUtc = DateTime.UtcNow, CreatedByUserId = "user-1" }
        };
        _caseServiceMock
            .Setup(s => s.GetCasesForUserAsync("user-1"))
            .ReturnsAsync(cases);

        // Act
        var cut = Render<MyCases>();

        // Assert
        Assert.Contains("#42", cut.Markup);
    }

    [Fact]
    public void MyCases_WithCases_DisplaysStatusBadge()
    {
        // Arrange
        this.AddAuthenticatedUser("user-1", "testuser", "User");
        var cases = new List<Case>
        {
            new() { Id = 1, Title = "Case", Status = WorkflowState.InProgress, CreatedAtUtc = DateTime.UtcNow, CreatedByUserId = "user-1" }
        };
        _caseServiceMock
            .Setup(s => s.GetCasesForUserAsync("user-1"))
            .ReturnsAsync(cases);

        // Act
        var cut = Render<MyCases>();

        // Assert
        Assert.Contains("In Progress", cut.Markup);
    }

    [Fact]
    public void MyCases_AsStaff_CallsGetAllCasesAsync()
    {
        // Arrange
        this.AddAuthenticatedUser("staff-1", "staffuser", "Staff");
        _caseServiceMock
            .Setup(s => s.GetAllCasesAsync())
            .ReturnsAsync(new List<Case>());

        // Act
        Render<MyCases>();

        // Assert
        _caseServiceMock.Verify(s => s.GetAllCasesAsync(), Times.Once);
        _caseServiceMock.Verify(s => s.GetCasesForUserAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void MyCases_AsUser_CallsGetCasesForUserAsync()
    {
        // Arrange
        this.AddAuthenticatedUser("user-1", "testuser", "User");
        _caseServiceMock
            .Setup(s => s.GetCasesForUserAsync("user-1"))
            .ReturnsAsync(new List<Case>());

        // Act
        Render<MyCases>();

        // Assert
        _caseServiceMock.Verify(s => s.GetCasesForUserAsync("user-1"), Times.Once);
        _caseServiceMock.Verify(s => s.GetAllCasesAsync(), Times.Never);
    }

    #endregion

    #region Layout Toggle

    [Fact]
    public void MyCases_DefaultLayout_IsListView()
    {
        // Arrange
        this.AddAuthenticatedUser("user-1", "testuser", "User");
        var cases = new List<Case>
        {
            new() { Id = 1, Title = "Case", Status = WorkflowState.New, CreatedAtUtc = DateTime.UtcNow, CreatedByUserId = "user-1" }
        };
        _caseServiceMock
            .Setup(s => s.GetCasesForUserAsync("user-1"))
            .ReturnsAsync(cases);

        // Act
        var cut = Render<MyCases>();

        // Assert — list view renders a table
        Assert.Contains("<table", cut.Markup);
    }

    [Fact]
    public void MyCases_AfterClickingCardsButton_SwitchesToCardView()
    {
        // Arrange
        this.AddAuthenticatedUser("user-1", "testuser", "User");
        var cases = new List<Case>
        {
            new() { Id = 1, Title = "Case", Status = WorkflowState.New, CreatedAtUtc = DateTime.UtcNow, CreatedByUserId = "user-1" }
        };
        _caseServiceMock
            .Setup(s => s.GetCasesForUserAsync("user-1"))
            .ReturnsAsync(cases);

        var cut = Render<MyCases>();

        // Act
        cut.Find("button[title='Card view']").Click();

        // Assert — card view renders divs not a table
        Assert.DoesNotContain("<table", cut.Markup);
        Assert.Contains("case-card", cut.Markup);
    }

    [Fact]
    public void MyCases_AfterSwitchingToCards_CanSwitchBackToList()
    {
        // Arrange
        this.AddAuthenticatedUser("user-1", "testuser", "User");
        var cases = new List<Case>
        {
            new() { Id = 1, Title = "Case", Status = WorkflowState.New, CreatedAtUtc = DateTime.UtcNow, CreatedByUserId = "user-1" }
        };
        _caseServiceMock
            .Setup(s => s.GetCasesForUserAsync("user-1"))
            .ReturnsAsync(cases);

        var cut = Render<MyCases>();
        cut.Find("button[title='Card view']").Click();

        // Act
        cut.Find("button[title='List view']").Click();

        // Assert
        Assert.Contains("<table", cut.Markup);
    }

    #endregion

    #region Error Handling

    [Fact]
    public void MyCases_WhenServiceThrows_ShowsErrorMessage()
    {
        // Arrange
        this.AddAuthenticatedUser("user-1", "testuser", "User");
        _caseServiceMock
            .Setup(s => s.GetCasesForUserAsync("user-1"))
            .ThrowsAsync(new Exception("DB error"));

        // Act
        var cut = Render<MyCases>();

        // Assert
        Assert.Contains("Unable to load cases", cut.Markup);
    }

    #endregion
}
