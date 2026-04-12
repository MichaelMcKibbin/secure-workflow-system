# Secure Workflow System

A Blazor Web App (Server) built with ASP.NET Core Identity, Entity Framework Core, and PostgreSQL.

## Current Status

Implemented so far:

- Authentication (register/login/logout) with ASP.NET Core Identity
- Role-based access control (RBAC) with `User`, `Staff`, and `Admin`
- Case workflow slice:
  - Create case
  - View case list
  - View case details
  - Assign case (Staff/Admin)
  - Update case status (Staff/Admin)
- Admin user role management page

## Tech Stack

- .NET 10
- Blazor Web App (Server)
- ASP.NET Core Identity
- Entity Framework Core
- Npgsql / PostgreSQL

## Prerequisites

- .NET SDK 10.x
- PostgreSQL running locally (default expected: `localhost:5432`)
- A PostgreSQL database/user matching your connection string

## Configuration

Connection string is configured in `appsettings.json`:

- `ConnectionStrings:DefaultConnection`

Default value currently in project:

`Host=localhost;Port=5432;Database=secure_workflow_system;Username=postgres;Password=postgres`

Update this for your local environment as needed.

## First Run Setup

1. Ensure PostgreSQL is running.
2. Ensure the database exists and credentials are valid.
3. Run the app.

In Development, the app currently:

- Applies pending EF Core migrations automatically at startup
- Seeds roles: `Admin`, `Staff`, `User`
- Seeds a default admin user if missing:
  - Email: `admin@local.test`
  - Password: `Admin123!`

## RBAC Model

### User

- Can create cases
- Can view:
  - cases they created
  - cases assigned to them

### Staff

- Can view all cases
- Can assign cases
- Can update case status

### Admin

- Full case access (same management capabilities as Staff)
- Can manage user roles

## Implemented Pages

### Public / Auth

- `/Account/Register`
- `/Account/Login`

### Cases

- `/cases`  
  - User: own + assigned cases  
  - Staff/Admin: all cases
- `/cases/create` (User/Admin)
- `/cases/{id:int}`  
  - User: allowed for own or assigned case  
  - Staff/Admin: full access + status/assignment update

### Admin

- `/admin/users` (Admin only)
  - View users
  - Set role (`User`, `Staff`, `Admin`)

## Navigation Behavior

Navigation links are role-aware:

- Cases: `User`, `Staff`, `Admin`
- Create Case: `User`, `Admin`
- Manage Users: `Admin`

## Database / Migrations

Migrations are in `Migrations/`.

If you need to manually apply migrations:

```powershell
dotnet ef database update
```

If you need to add a new migration:

```powershell
dotnet ef migrations add <MigrationName>
```

## Notes

- Case status options currently available in UI:
  - `New`, `Assigned`, `In Progress`, `Resolved`, `Closed`
- Error handling is intentionally simple and user-friendly at this stage.
