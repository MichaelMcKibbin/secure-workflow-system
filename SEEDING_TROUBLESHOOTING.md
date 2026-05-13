# Seeding Troubleshooting (VPS/Cloud)

This document explains a production seeding issue where the first admin user was not created, how to diagnose it quickly, and the fix that resolved it.

## Symptoms

- App deploys and runs, but login with seed admin credentials fails.
- Querying Identity users shows no seeded admin:

```sql
select "UserName", "Email", "IsApproved" from "AspNetUsers";
```

- No initial admin account appears after deployment.

## Root Cause

The deployment environment variable names did not match what `docker-compose.yml` expects.

### What happened

In `.github/workflows/deploy.yml`, the generated `workflow.env` originally used:

- `SeedAdmin__Email`
- `SeedAdmin__Password`

But `docker-compose.yml` maps app config from:

- `${SEED_ADMIN_EMAIL}`
- `${SEED_ADMIN_PASSWORD}`

Because those uppercase variables were missing, compose substituted empty values. In production, `Program.cs` skips admin creation when seed email/password are empty.

## Fix Applied

Updated `.github/workflows/deploy.yml` to write the correct variables:

- `SEED_ADMIN_EMAIL=${{ secrets.SEED_ADMIN_EMAIL }}`
- `SEED_ADMIN_PASSWORD=${{ secrets.SEED_ADMIN_PASSWORD }}`

`docker-compose.yml` then maps them to app configuration:

- `SeedAdmin__Email: ${SEED_ADMIN_EMAIL}`
- `SeedAdmin__Password: ${SEED_ADMIN_PASSWORD}`

This allows `Program.cs` seeding to receive values and create the admin user.

## Verification Checklist

1. Confirm GitHub repository secrets exist:
   - `SEED_ADMIN_EMAIL`
   - `SEED_ADMIN_PASSWORD`
2. After deploy, SSH into VPS and inspect `/docker/workflow/workflow.env`.
3. Confirm both are present and non-empty:
   - `SEED_ADMIN_EMAIL=...`
   - `SEED_ADMIN_PASSWORD=...`
4. Confirm container is running:

```bash
docker compose -p workflow --env-file workflow.env ps
```

5. Check app logs for startup/seeding messages:

```bash
docker compose -p workflow --env-file workflow.env logs --tail=200 workflow
```

6. Verify admin user exists in DB:

```sql
select "UserName", "Email", "IsApproved" from "AspNetUsers";
```

## Quick Troubleshooting Flow

1. **No users in `AspNetUsers`**
   - Check `workflow.env` variable names and values first.
2. **User exists but login fails**
   - Ensure password meets Identity policy.
   - Confirm you are using the credentials currently stored in GitHub secrets.
3. **User exists but not fully usable**
   - Verify `EmailConfirmed` and `IsApproved` are true (seeding sets these).
4. **Seeding still skipped in Production**
   - Confirm app actually receives values via container environment.
   - Inspect startup logs for migration/identity errors.

## Notes

- Local `.env` in the repo is for local development and is not automatically used on VPS unless explicitly passed to compose there.
- VPS runtime values come from the generated `workflow.env` created during GitHub Actions deployment.

## Prevention

- Keep naming consistent across all layers:
  - GitHub Secret name
  - Deploy workflow `echo` line
  - Compose `${VAR_NAME}` substitution
  - App config key mapping (`Section__Key`)
- If possible, add a startup warning log when seed values are missing in Production.
- Add a deployment validation step that fails if required env vars are empty.
