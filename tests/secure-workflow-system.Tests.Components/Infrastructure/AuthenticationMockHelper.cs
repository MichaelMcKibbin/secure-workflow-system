using System.Security.Claims;
using Bunit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace secure_workflow_system.Tests.Components.Infrastructure;

public static class AuthenticationMockHelper
{
    /// <summary>
    /// Registers a fake authenticated user with the given roles into the bUnit test context.
    /// </summary>
    [Obsolete]
    public static void AddAuthenticatedUser(
        this TestContext ctx,
        string userId,
        string userName,
        params string[] roles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Name, userName)
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        var authState = Task.FromResult(new AuthenticationState(principal));

        ctx.Services.AddSingleton<AuthenticationStateProvider>(
            new FakeAuthenticationStateProvider(authState));

        ctx.Services.AddAuthorization(options =>
        {
            options.AddPolicy("RequireAdmin", p => p.RequireRole("Admin"));
            options.AddPolicy("RequireStaff", p => p.RequireRole("Admin", "Staff"));
            options.AddPolicy("CanCreateCase", p => p.RequireRole("User", "Admin"));
            options.AddPolicy("CanViewCases", p => p.RequireRole("User", "Staff", "Admin"));
            options.AddPolicy("CanManageCases", p => p.RequireRole("Staff", "Admin"));
            options.AddPolicy("CanManageUsers", p => p.RequireRole("Admin"));
        });

        ctx.Services.AddSingleton<IAuthorizationService>(sp =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<AuthorizationOptions>>();
            return new DefaultAuthorizationService(
                sp.GetRequiredService<IAuthorizationPolicyProvider>(),
                sp.GetRequiredService<IAuthorizationHandlerProvider>(),
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<DefaultAuthorizationService>>(),
                sp.GetRequiredService<IAuthorizationHandlerContextFactory>(),
                sp.GetRequiredService<IAuthorizationEvaluator>(),
                options);
        });
    }

    /// <summary>
    /// Registers an unauthenticated (anonymous) user into the bUnit test context.
    /// </summary>
    [Obsolete]
    public static void AddAnonymousUser(this TestContext ctx)
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity());
        var authState = Task.FromResult(new AuthenticationState(principal));

        ctx.Services.AddSingleton<AuthenticationStateProvider>(
            new FakeAuthenticationStateProvider(authState));
    }

    private sealed class FakeAuthenticationStateProvider(Task<AuthenticationState> authState)
        : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync() => authState;
    }
}
