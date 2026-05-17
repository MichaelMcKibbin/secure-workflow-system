# Entity Framework Core Eager Loading - Navigation Properties Fix

## Problem
In the case details page, user-related fields display GUIDs (memory references) instead of user names:
- **Created By**: `8230729e-44a9-417e-9a66-543a4bb35d77` (should be: `john.doe`)
- **Assigned To**: `7696da96-852f-448c-8ab4-90bbb8f5743e` (should be: `jane.smith`)
- **Changed By** (in status history): Similar GUID display

## Root Cause
Entity Framework Core queries were not eager-loading related entities. The Case and CaseStatusHistory models have navigation properties that reference ApplicationUser:

```csharp
// Case.cs
public string CreatedByUserId { get; set; }
public ApplicationUser CreatedByUser { get; set; }  // Not loaded by default!

public string? AssignedToUserId { get; set; }
public ApplicationUser? AssignedToUser { get; set; }  // Not loaded by default!

// CaseStatusHistory.cs
public string ChangedByUserId { get; set; }
public ApplicationUser ChangedByUser { get; set; }  // Not loaded by default!
```

Without explicitly loading these related entities using `.Include()`, only the ID properties are populated, leaving the navigation properties as `null`.

## Solution
Add `.Include()` statements to EF Core queries to eager-load related user entities:

### Services/CaseService.cs

```csharp
// Before: Only returns IDs
public async Task<Case?> GetCaseByIdAsync(int caseId)
{
	return await dbContext.Cases
		.AsNoTracking()
		.FirstOrDefaultAsync(c => c.Id == caseId);
}

// After: Includes related user data
public async Task<Case?> GetCaseByIdAsync(int caseId)
{
	return await dbContext.Cases
		.AsNoTracking()
		.Include(c => c.CreatedByUser)
		.Include(c => c.AssignedToUser)
		.FirstOrDefaultAsync(c => c.Id == caseId);
}
```

Apply the same pattern to:
- `GetCaseByIdForUserAsync()` - Add includes for CreatedByUser and AssignedToUser
- `GetCaseStatusHistoryAsync()` - Add include for ChangedByUser

### Components/Pages/CaseDetails.razor

```razor
<!-- Before: Displays user ID -->
<p class="text-muted">@_case.CreatedByUserId</p>

<!-- After: Displays user name -->
<p class="text-muted">@_case.CreatedByUser?.UserName</p>
```

Use the same pattern for:
- `Assigned To`: `@_case.AssignedToUser?.UserName ?? "Unassigned"`
- Status History `Changed By`: `@history.ChangedByUser?.UserName`

## Key Concepts

### Eager Loading vs. Lazy Loading

| Method | How It Works | Use Case |
|--------|-------------|----------|
| **Eager Loading** (`.Include()`) | Related entities loaded with main query | When you need related data immediately |
| **Lazy Loading** | Related entities loaded on access | When related data may not be needed |
| **Explicit Loading** | Manually load later with `.Load()` | For selective or deferred loading |

### Why Use `.AsNoTracking()`?
- Improves performance for read-only queries
- EF Core doesn't track changes
- Still works with `.Include()` for eager loading

## Best Practices

1. **Load what you need**: Only include navigation properties you'll actually use
2. **Avoid N+1 queries**: Always use `.Include()` instead of loading in a loop
3. **Be specific**: Include only the related entities needed to avoid unnecessary data transfer
4. **Use null-coalescing**: Handle cases where related entities might be null with `?.` and `??`

## Example - The Fix

```csharp
// Services/CaseService.cs
public async Task<IReadOnlyList<CaseStatusHistory>> GetCaseStatusHistoryAsync(int caseId)
{
	return await dbContext.CaseStatusHistories
		.AsNoTracking()
		.Include(h => h.ChangedByUser)  // ← Load related user
		.Where(h => h.CaseId == caseId)
		.OrderByDescending(h => h.ChangedAtUtc)
		.ToListAsync();
}
```

```razor
<!-- Components/Pages/CaseDetails.razor -->
<td>@history.ChangedByUser?.UserName</td>  <!-- ← Display user name instead of ID -->
```

## Testing

After applying this fix:
1. View a case details page
2. Verify "Created By" shows a user name, not a GUID
3. If a case is assigned, verify "Assigned To" shows a user name
4. Check status history for "Changed By" column showing user names

## References

- [Entity Framework Core: Loading Related Data](https://learn.microsoft.com/en-us/ef/core/querying/related-data)
- [.Include() Method Documentation](https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.entityframeworkqueryableextensions.include)
- [Eager, Lazy, and Explicit Loading](https://learn.microsoft.com/en-us/ef/core/querying/related-data/eager)
