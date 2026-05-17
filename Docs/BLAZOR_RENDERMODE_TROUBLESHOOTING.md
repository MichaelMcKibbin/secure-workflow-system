# Blazor Interactive Components Not Responding - Troubleshooting

## Problem
Interactive features in Blazor components (event handlers, state changes, button clicks) are not working. Buttons appear to do nothing when clicked, and UI state is not updating.

## Root Cause
In **Blazor Web App (.NET 10)**, components must explicitly declare their render mode to be interactive. Components without a `@rendermode` directive render as **static (server-side rendered)**, which disables all interactive features:
- `@onclick` event handlers don't fire
- State variables don't update
- UI doesn't re-render

## Solution
Add the `@rendermode` directive to the top of your `.razor` component file, immediately after the `@page` directive:

```razor
@page "/cases"
@rendermode InteractiveServer
@attribute [Authorize(Policy = "CanViewCases")]
@inject AuthenticationStateProvider AuthenticationStateProvider
```

## Common Render Modes

| Render Mode | Description | Use Case |
|------------|-------------|----------|
| `InteractiveServer` | Component runs on the server, updates via WebSocket | Most interactive components that need server-side logic |
| `InteractiveAuto` | Runs on server initially, switches to WebAssembly if available | Hybrid approach for better UX |
| (none) | Static rendering only | Read-only content, no interactivity needed |

## How to Identify This Issue

- Check the browser console for errors
- Verify buttons/event handlers don't respond to clicks
- Look at the component source - if there's no `@rendermode` directive, that's likely the issue
- Compare with other interactive components in your project (e.g., `AdminUsers.razor`, `CaseDetails.razor`)

## Best Practices

1. **Check existing patterns**: Look at similar interactive components in your codebase to match the render mode pattern
2. **For page components**: Always add `@rendermode InteractiveServer` if the page has interactive features
3. **For child components**: If a component is used within an interactive parent, it inherits the parent's render mode (usually doesn't need explicit declaration, but can add for clarity)

## Example - Before and After

### ❌ Before (Not Working)
```razor
@page "/cases"
@attribute [Authorize(Policy = "CanViewCases")]
@inject CaseService CaseService

<button @onclick="() => _isListLayout = true">List View</button>
```

### ✅ After (Working)
```razor
@page "/cases"
@rendermode InteractiveServer
@attribute [Authorize(Policy = "CanViewCases")]
@inject CaseService CaseService

<button @onclick="() => _isListLayout = true">List View</button>
```

## References

- [ASP.NET Core Blazor render modes](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/render-modes)
- [Blazor Web App project structure](https://learn.microsoft.com/en-us/aspnet/core/blazor/hosting-models)
