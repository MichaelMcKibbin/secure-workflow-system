# Deployment Secrets Inventory

This document outlines all secrets used in the Secure Workflow System deployment pipeline and where each should be stored and declared.

## 🔐 Overview

The application uses secrets across three environments:
- **GitHub Actions** - CI/CD pipeline for building and deploying
- **VPS Production** - Runtime environment variables for production containers
- **Local Development** - Local Docker Compose for development

---

## GitHub Actions Secrets

**Location:** `https://github.com/MichaelMcKibbin/secure-workflow-system/settings/secrets/actions`

These secrets are used in `.github/workflows/deploy.yml` and referenced as `${{ secrets.SECRET_NAME }}`.

### Required Secrets

| Secret Name | Purpose | Format | Example | Sensitivity |
|---|---|---|---|---|
| `GHCR_PAT` | GitHub Container Registry Personal Access Token | String | `ghp_xxxxxxxxxxxxxxxxxxxx` | 🔴 **HIGH** |
| `VPS_HOST` | VPS IP address or hostname | String | `123.45.67.89` or `vps.example.com` | 🔴 **HIGH** |
| `VPS_USER` | SSH user on VPS | String | `root` or `deploy` | 🟡 **MEDIUM** |
| `VPS_SSH_KEY` | Private SSH key for VPS access | PEM (multi-line) | `-----BEGIN OPENSSH PRIVATE KEY-----...` | 🔴 **HIGH** |
| `POSTGRES_PASSWORD` | PostgreSQL root password (production) | String | `SecurePassword123!Xyz` | 🔴 **HIGH** |
| `WORKFLOW_CONNECTION_STRING` | Full database connection string | PostgreSQL connection string | `Host=postgres;Port=5432;Database=workflow;Username=postgres;Password=...` | 🔴 **HIGH** |
| `SEED_ADMIN_EMAIL` | Initial admin account email | Email | `admin@yourcompany.com` | 🟡 **MEDIUM** |
| `SEED_ADMIN_PASSWORD` | Initial admin account password | String | `AdminPassword123!Xyz` | 🔴 **HIGH** |

### How to Add GitHub Secrets

1. Navigate to repository Settings
2. Go to **Secrets and variables** → **Actions**
3. Click **New repository secret**
4. Enter secret name and value
5. Click **Add secret**

### Usage in Workflow

```yaml
# In .github/workflows/deploy.yml (lines 58-67)
- name: Deploy to VPS via SSH
  uses: appleboy/ssh-action@v1.2.0
  with:
	host: ${{ secrets.VPS_HOST }}
	username: ${{ secrets.VPS_USER }}
	key: ${{ secrets.VPS_SSH_KEY }}
	script: |
	  cd /docker/secure-workflow-system
	  echo "IMAGE_SHA=${{ env.IMAGE_SHA }}" > workflow.env
	  echo "POSTGRES_PASSWORD=${{ secrets.POSTGRES_PASSWORD }}" >> workflow.env
	  echo "ConnectionStrings__DefaultConnection=${{ secrets.WORKFLOW_CONNECTION_STRING }}" >> workflow.env
	  echo "SeedAdmin__Email=${{ secrets.SEED_ADMIN_EMAIL }}" >> workflow.env
	  echo "SeedAdmin__Password=${{ secrets.SEED_ADMIN_PASSWORD }}" >> workflow.env
```

---

## VPS Production Environment

**Location:** `/docker/secure-workflow-system/workflow.env` (on VPS)

This file is **generated automatically** during deployment and should **never** be committed to git.

### Generated Variables

The deploy workflow writes to `workflow.env`:

```sh
# Generated from GitHub secrets during deploy step
IMAGE_SHA=abc123def456...
POSTGRES_PASSWORD=<from secrets.POSTGRES_PASSWORD>
ConnectionStrings__DefaultConnection=<from secrets.WORKFLOW_CONNECTION_STRING>
SeedAdmin__Email=<from secrets.SEED_ADMIN_EMAIL>
SeedAdmin__Password=<from secrets.SEED_ADMIN_PASSWORD>
```

### Optional Variables

You can also add these to `workflow.env` on the VPS if needed:

| Variable | Purpose | Default | Example |
|---|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | ASP.NET Core environment | `Production` | `Production` |
| `POSTGRES_DB` | Database name | `secure_workflow_system` | `secure_workflow_system` |
| `POSTGRES_USER` | Database user | `postgres` | `postgres` |
| `WORKFLOW_PORT` | Port for web app | `8080` | `8080` |
| `POSTGRES_PORT` | Port for PostgreSQL | `5432` | `5432` |

### Deployment Process

```bash
# GitHub Actions runs this during deploy:
cd /docker/secure-workflow-system
echo "IMAGE_SHA=${{ env.IMAGE_SHA }}" > workflow.env
echo "POSTGRES_PASSWORD=${{ secrets.POSTGRES_PASSWORD }}" >> workflow.env
# ... more appends ...

# Then pulls and runs containers
docker compose -p workflow --env-file workflow.env pull
docker compose -p workflow --env-file workflow.env up -d
```

### Manual VPS Setup (if needed)

If you need to manually set up the VPS:

```bash
ssh deploy@your-vps.com
cd /docker/secure-workflow-system

# Create workflow.env manually (not recommended - use GitHub secrets instead)
cat > workflow.env << EOF
IMAGE_SHA=latest
POSTGRES_PASSWORD=YourSecurePassword123!
ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=secure_workflow_system;Username=postgres;Password=YourSecurePassword123!
SeedAdmin__Email=admin@example.com
SeedAdmin__Password=AdminPassword123!
EOF

# Run containers
docker compose -p workflow --env-file workflow.env up -d
```

---

## Local Development Environment

**Location:** `.env` (project root)

Create this file locally for development. **DO NOT COMMIT TO GIT.**

### .gitignore Entry

Ensure `.env` is in your `.gitignore`:

```
.env
.env.local
.env.*.local
workflow.env
```

### Local Development .env Template

```sh
# .env - Local Development Configuration
# DO NOT COMMIT THIS FILE

# Docker Compose settings
ASPNETCORE_ENVIRONMENT=Development

# PostgreSQL
POSTGRES_DB=secure_workflow_system
POSTGRES_USER=postgres
POSTGRES_PASSWORD=postgres
POSTGRES_PORT=5432

# App Database Connection
ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=secure_workflow_system;Username=postgres;Password=postgres

# Seed Admin Account (local development)
SEED_ADMIN_EMAIL=admin@local.test
SEED_ADMIN_PASSWORD=Admin123!

# PgAdmin (optional dev UI)
PGADMIN_DEFAULT_EMAIL=admin@local.test
PGADMIN_DEFAULT_PASSWORD=admin
PGADMIN_PORT=5050

# App Port
WORKFLOW_PORT=8080
```

### Running Local Development

```bash
# With dev profile (includes PgAdmin)
docker compose --env-file .env --profile dev up -d

# Without dev profile (no PgAdmin)
docker compose --env-file .env up -d

# View logs
docker compose logs -f workflow

# Tear down
docker compose down
```

---

## Environment Variable Reference

### docker-compose.yml Usage

The compose file expects these variables to be available:

```yaml
services:
  workflow:
	image: ghcr.io/michaelmckibbin/secure-workflow-system:sha-${IMAGE_SHA:-latest}
	environment:
	  ASPNETCORE_ENVIRONMENT: ${ASPNETCORE_ENVIRONMENT:-Production}
	  ConnectionStrings__DefaultConnection: Host=postgres;...;Password=${POSTGRES_PASSWORD}
	  SeedAdmin__Email: ${SEED_ADMIN_EMAIL}
	  SeedAdmin__Password: ${SEED_ADMIN_PASSWORD}

  postgres:
	environment:
	  POSTGRES_DB: ${POSTGRES_DB:-secure_workflow_system}
	  POSTGRES_USER: ${POSTGRES_USER:-postgres}
	  POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
```

Default values (with `:-`) are used if the variable is not set.

---

## 🔐 Security Best Practices

### ✅ DO

- [ ] **Rotate credentials regularly**
  - Regenerate `GHCR_PAT` every 90 days
  - Rotate `VPS_SSH_KEY` annually or after team changes
  - Change `POSTGRES_PASSWORD` when onboarding/offboarding users

- [ ] **Use strong passwords**
  - Minimum 16 characters
  - Mix uppercase, lowercase, numbers, and symbols
  - Example: `P@ssw0rd!Secure#2024`

- [ ] **Keep `.env` in `.gitignore`**
  - Never commit local secrets
  - Add `.env*` patterns to `.gitignore`

- [ ] **Use unique credentials per environment**
  - Local development: simple test passwords
  - VPS production: complex, unique passwords
  - Never reuse local creds in production

- [ ] **Audit GitHub secrets access**
  - Review who has access to repository secrets
  - Monitor deploy logs for unauthorized access
  - Keep audit trail of secret changes

- [ ] **Backup SSH keys securely**
  - Store offline in password manager or HSM
  - Keep multiple backups in secure locations
  - Document key rotation procedures

### ❌ DON'T

- [ ] **Commit secrets to git**
  - Don't hardcode passwords in code
  - Don't include `.env` files in commits
  - If accidentally committed, rotate immediately

- [ ] **Share secrets in logs or chat**
  - Never paste credentials in Slack, Discord, etc.
  - Never include secrets in GitHub issues or PRs
  - Sanitize logs before sharing

- [ ] **Reuse secrets across environments**
  - Local, staging, and production need different credentials
  - Compromised local cred shouldn't affect production

- [ ] **Use default/example credentials in production**
  - Change all default passwords immediately
  - Don't use test values like `admin@local.test`

- [ ] **Store SSH keys in plain text files**
  - Use Git credential manager or SSH agent
  - Never commit private keys

---

## Troubleshooting

### "Invalid password" error during deployment

1. Check `POSTGRES_PASSWORD` in GitHub secrets
2. Verify password doesn't contain special shell characters
3. Ensure password matches in both `POSTGRES_PASSWORD` and `ConnectionStrings__DefaultConnection`

### Deploy fails with "authentication failed"

1. Verify `VPS_SSH_KEY` is correct private key (not public)
2. Ensure key is in PEM format, not OpenSSH format
3. Check `VPS_HOST` and `VPS_USER` are correct

### Containers don't start with "missing environment variable"

1. Ensure `workflow.env` exists on VPS
2. Check that GitHub workflow is writing to correct file
3. Verify docker compose command uses `--env-file workflow.env`

### Local development container won't start

1. Ensure `.env` file exists in project root
2. Check that passwords don't contain unescaped shell characters
3. Verify `docker compose --env-file .env up -d` command format

---

## Checklist for New Deployments

- [ ] Create GitHub repository secrets (all 8 required)
- [ ] Generate new SSH key pair for VPS access
- [ ] Create strong, unique passwords for production
- [ ] Verify deploy workflow runs successfully
- [ ] Check VPS logs: `docker compose logs -f workflow`
- [ ] Test admin login with seeded credentials
- [ ] Update database with `dotnet ef database update` if needed
- [ ] Document any environment-specific settings
- [ ] Back up SSH keys in secure location
- [ ] Notify team of deployment completion

---

## References

- [GitHub Actions Secrets Documentation](https://docs.github.com/en/actions/security-guides/using-secrets-in-github-actions)
- [Docker Compose Environment Variables](https://docs.docker.com/compose/environment-variables/)
- [PostgreSQL Environment Variables](https://www.postgresql.org/docs/current/libpq-envars.html)
- [SSH Key Security Best Practices](https://man.openbsd.org/ssh-keygen)

---

**Last Updated:** 2026-04-13  
**Maintained By:** Development Team  
**Questions?** Check the troubleshooting section or contact DevOps
