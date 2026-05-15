# CaseStatusHistory Feature Implementation

## Overview
A clean, minimal implementation of case status history tracking that records every status change for a case while maintaining the existing architecture.

## Changes Made

### 1. New Entity: CaseStatusHistory
**File:** `Data/CaseStatusHistory.cs`
- `Id` (int, primary key)
- `CaseId` (int, foreign key to Case)
- `Case` (navigation property)
- `OldStatus` (string, required, max 50)
- `NewStatus` (string, required, max 50)
- `ChangedByUserId` (string, required, foreign key to ApplicationUser)
- `ChangedByUser` (navigation property)
- `ChangedAtUtc` (DateTime, defaults to UTC now)

### 2. Database Context Updates
**File:** `Data/ApplicationDbContext.cs`
- Added `DbSet<CaseStatusHistory> CaseStatusHistories`
- Configured relationships:
  - Case → CaseStatusHistory: One-to-many with cascade delete (Case deletion deletes history)
  - ApplicationUser → CaseStatusHistory: Many-to-one with restrict delete (User deletion does not delete history)
- Added indexes for performance:
  - `CaseId`
  - `ChangedByUserId`
  - `CaseId + ChangedAtUtc` (optimizes history ordering)

### 3. Service Layer Updates
**Files:** `Services/ICaseService.cs`, `Services/CaseService.cs`

#### New Method: `GetCaseStatusHistoryAsync(int caseId)`
- Retrieves all status history records for a case
- Returns in descending chronological order (newest first)
- Uses `AsNoTracking()` for read-only queries

#### Enhanced Method: `UpdateCaseStatusAndAssignmentAsync` (Overload)
- New signature: `UpdateCaseStatusAndAssignmentAsync(int caseId, string status, string? assignedToUserId, string changedByUserId)`
- Automatically creates `CaseStatusHistory` record when status changes
- Only records history if status actually changed (no-op if same status)
- Original parameterless overload preserved for backward compatibility

### 4. EF Core Migration
**File:** `Migrations/20260412195934_AddCaseStatusHistory.cs`
- Creates `CaseStatusHistories` table with all required columns and constraints
- Adds three indexes for query optimization
- Includes proper foreign key relationships
- `Down()` method for rollback support

### 5. UI Enhancement
**File:** `Components/Pages/CaseDetails.razor`
- Added status history display section
- Shows status changes in a table with columns:
  - **From** (OldStatus)
  - **To** (NewStatus)
  - **Changed By** (ChangedByUserId)
  - **Changed At (UTC)** (ChangedAtUtc, formatted as ISO 8601)
- Only loads history for authorized users (existing authorization check applies)
- Updates history display after each successful case update
- Displays "No status changes recorded yet" when empty

## Key Design Decisions

1. **Method Overloading**: Preserved backward compatibility by keeping the original `UpdateCaseStatusAndAssignmentAsync` signature and adding a new overload with `changedByUserId` parameter.

2. **No-op Status Changes**: If a case is updated with the same status, no history record is created (clean history, prevents noise).

3. **Delete Behavior**:
   - Case deletion cascades to history (cleanup)
   - User deletion does NOT delete history (audit trail preserved with user ID)

4. **Authorization**: Status history is only visible to users who can already view the case (Staff, Admin, or case creator/assignee).

5. **Read-Only Queries**: History retrieval uses `AsNoTracking()` for performance since it's never updated.

6. **Chronological Order**: History displayed newest-first for UX (most recent changes visible first).

## Usage

### Recording a Status Change
```csharp
// New method with user tracking
await CaseService.UpdateCaseStatusAndAssignmentAsync(
    caseId: 1,
    status: "In Progress",
    assignedToUserId: "user123",
    changedByUserId: currentUserId);  // Current user recorded as who made change
```

### Retrieving History
```csharp
var history = await CaseService.GetCaseStatusHistoryAsync(caseId: 1);
// Returns List<CaseStatusHistory> ordered by ChangedAtUtc descending
```

## Migration Notes

The migration was automatically generated and includes:
- Table creation with proper data types (PostgreSQL)
- Nullable datetime handling
- Cascade/Restrict delete behaviors
- Index creation for optimal query performance

**Status**: Generated but not applied (ready for deployment)

## Future Enhancements (Not Implemented)

- Add user email/name display in UI (currently shows ID)
- Export history as CSV
- Filter history by date range
- Track other field changes (not just status)
- Email notifications on status changes
- Pagination for high-volume history

## Testing Recommendations

1. Create a case and change its status multiple times
2. Verify history appears in chronological order
3. Test authorization (verify only authorized users see history)
4. Delete a user and verify their name is still shown in history
5. Delete a case and verify history is cleaned up
6. Test no-op updates (same status twice) - should only create one record
