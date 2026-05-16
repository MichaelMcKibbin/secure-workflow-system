# Seeding Troubleshooting (VPS/Cloud)

This document explains a production seeding issue where the initial seeded accounts were not created, how to diagnose it quickly, and the fix that resolved it.

## Symptoms

- App deploys and runs, but login with seed credentials fails.
- Querying Identity users shows no seeded accounts:

```sql
select "UserName", "Email", "IsApproved" from "AspNetUsers";
```

- No initial admin, user, or staff accounts appear after deployment.

## Root Cause

The deployment environment variable names did not match what `docker-compose.yml` and `Program.cs` expect.

### What happened

The app now seeds three accounts from environment values:

- `SEED_ADMIN_EMAIL` / `SEED_ADMIN_PASSWORD`
- `SEED_USER_EMAIL` / `SEED_USER_PASSWORD`
- `SEED_STAFF_EMAIL` / `SEED_STAFF_PASSWORD`

The GitHub Actions deploy step must write those names into `workflow.env`, and `docker-compose.yml` must pass them into the app container unchanged. If any of those values are missing, the corresponding account is skipped.

## Fix Applied

Updated `.github/workflows/deploy.yml` to write the correct variables:

- `SEED_ADMIN_EMAIL=${{ secrets.SEED_ADMIN_EMAIL }}`
- `SEED_ADMIN_PASSWORD=${{ secrets.SEED_ADMIN_PASSWORD }}`
- `SEED_USER_EMAIL=${{ secrets.SEED_USER_EMAIL }}`
- `SEED_USER_PASSWORD=${{ secrets.SEED_USER_PASSWORD }}`
- `SEED_STAFF_EMAIL=${{ secrets.SEED_STAFF_EMAIL }}`
- `SEED_STAFF_PASSWORD=${{ secrets.SEED_STAFF_PASSWORD }}`

`docker-compose.yml` then maps them into the workflow container environment, and `Program.cs` uses them to seed the `Admin`, `User`, and `Staff` roles/accounts.

## Verification Checklist

1. Confirm GitHub repository secrets exist:
   - `SEED_ADMIN_EMAIL`
   - `SEED_ADMIN_PASSWORD`
   - `SEED_USER_EMAIL`
   - `SEED_USER_PASSWORD`
   - `SEED_STAFF_EMAIL`
   - `SEED_STAFF_PASSWORD`
2. After deploy, SSH into VPS and inspect `/docker/workflow/workflow.env`.
3. Confirm the values are present and non-empty.
4. Confirm container is running:

```bash
docker compose -p workflow --env-file workflow.env ps
```

5. Check app logs for startup/seeding messages:

```bash
docker compose -p workflow --env-file workflow.env logs --tail=200 workflow
```

6. Verify users exist in DB:

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
  - App config key mapping
- If possible, add a startup warning log when seed values are missing in Production.
- Add a deployment validation step that fails if required env vars are empty.
