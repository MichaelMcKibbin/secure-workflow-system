using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using secure_workflow_system.Components.Pages;
using secure_workflow_system.Data;
using secure_workflow_system.Services;
using secure_workflow_system.Tests.Components.Infrastructure;
using Xunit;

namespace secure_workflow_system.Tests.Components.Pages;

[Obsolete]
public class CreateCaseTests : TestContext
{
    private readonly Mock<ICaseService> _caseServiceMock = new();

    public CreateCaseTests()
    {
        Services.AddSingleton(_caseServiceMock.Object);
        Services.AddCascadingAuthenticationState();
    }

    #region Rendering

    [Fact]
    public void CreateCase_RendersFormWithTitleAndDescriptionFields()
    {
        // Arrange
        this.AddAuthenticatedUser("user-1", "testuser", "User");

        // Act
        var cut = Render<CreateCase>();

        // Assert
        Assert.NotNull(cut.Find("#title"));
        Assert.NotNull(cut.Find("#description"));
    }

    [Fact]
    public void CreateCase_RendersSubmitButton()
    {
        // Arrange
        this.AddAuthenticatedUser("user-1", "testuser", "User");

        // Act
        var cut = Render<CreateCase>();

        // Assert
        var button = cut.Find("button[type='submit']");
        Assert.Contains("Create Case", button.TextContent);
    }

    #endregion

    #region Validation

    [Fact]
    public void CreateCase_SubmittingEmptyForm_ShowsValidationErrors()
    {
        // Arrange
        this.AddAuthenticatedUser("user-1", "testuser", "User");
        var cut = Render<CreateCase>();

        // Act
        cut.Find("form").Submit();

        // Assert
        Assert.Contains("field is required", cut.Markup.ToLower());
    }

    [Fact]
    public void CreateCase_SubmittingWithOnlyTitle_ShowsValidationError()
    {
        // Arrange
        this.AddAuthenticatedUser("user-1", "testuser", "User");
        var cut = Render<CreateCase>();
        cut.Find("#title").Change("My Title");

        // Act
        cut.Find("form").Submit();

        // Assert — description is still required
        Assert.Contains("field is required", cut.Markup.ToLower());
    }

    #endregion

    #region Submission

    [Fact]
    public async Task CreateCase_WithValidData_CallsCreateCaseAsync()
    {
        // Arrange
        this.AddAuthenticatedUser("user-1", "testuser", "User");
        _caseServiceMock
            .Setup(s => s.CreateCaseAsync("user-1", "My Title", "My Description"))
            .ReturnsAsync(new Case { Id = 1, Title = "My Title", Description = "My Description", CreatedByUserId = "user-1" });

        var cut = Render<CreateCase>();
        cut.Find("#title").Change("My Title");
        cut.Find("#description").Change("My Description");

        // Act
        cut.Find("form").Submit();
        await Task.Delay(50);

        // Assert
        _caseServiceMock.Verify(
            s => s.CreateCaseAsync("user-1", "My Title", "My Description"),
            Times.Once);
    }

    [Fact]
    public async Task CreateCase_WhenServiceThrows_ShowsErrorMessage()
    {
        // Arrange
        this.AddAuthenticatedUser("user-1", "testuser", "User");
        _caseServiceMock
            .Setup(s => s.CreateCaseAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("DB error"));

        var cut = Render<CreateCase>();
        cut.Find("#title").Change("My Title");
        cut.Find("#description").Change("My Description");

        // Act
        cut.Find("form").Submit();
        await Task.Delay(50);

        // Assert
        Assert.Contains("Unable to create the case", cut.Markup);
    }

    [Fact]
    public async Task CreateCase_WhileSubmitting_DisablesSubmitButton()
    {
        // Arrange
        this.AddAuthenticatedUser("user-1", "testuser", "User");
        var tcs = new TaskCompletionSource<Case>();
        _caseServiceMock
            .Setup(s => s.CreateCaseAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(tcs.Task);

        var cut = Render<CreateCase>();
        cut.Find("#title").Change("My Title");
        cut.Find("#description").Change("My Description");

        // Act
        cut.Find("form").Submit();

        // Assert — button should be disabled while saving
        var button = cut.Find("button[type='submit']");
        Assert.True(button.HasAttribute("disabled"));

        // Cleanup
        tcs.SetCanceled();
    }

    #endregion
}
