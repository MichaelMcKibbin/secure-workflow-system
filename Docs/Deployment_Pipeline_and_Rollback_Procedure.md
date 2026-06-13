# Deployment Pipeline and Rollback Procedure

## Overview

The Secure Workflow System uses a GitHub Actions CI/CD pipeline to automatically build, publish, and deploy containerised application updates to a VPS-hosted Docker environment.

The deployment workflow uses immutable container image references stored in GitHub Container Registry (GHCR) to improve deployment reliability, traceability, and rollback capability.

---

# CI/CD Deployment Workflow

## 1. Developer Pushes Changes

Changes are committed and pushed to a feature or development branch.

Example:

```bash
git add .
git commit -m "feat: add role management improvements"
git push
```

2. Pull Request Validation

A Pull Request (PR) is created to merge changes into the develop or main branch.

GitHub Actions automatically performs:

Dependency restore
Application build
Unit tests
Formatting validation
Docker image build validation
Security scanning (where configured)
Example Checks
Build
Test
Format Check
Docker Build
CodeQL
3. Merge into Main

After approval and successful CI validation, the PR is merged into the main branch.

This triggers the production deployment pipeline.

Production Deployment Process
1. Build and Publish Container Image

GitHub Actions builds the production container image and publishes it to GitHub Container Registry (GHCR).

Image Tags

The workflow publishes:

latest
Immutable SHA/digest references

Example:

ghcr.io/michaelmckibbin/secure-workflow-system:latest
ghcr.io/michaelmckibbin/secure-workflow-system@sha256:<digest>
2. Generate VPS Environment File

The deployment workflow generates a workflow.env file on the VPS containing deployment variables and secrets. The Docker Compose configuration expects an immutable image digest (sha256:...) in IMAGE_DIGEST and Postgres runtime variables (POSTGRES_PASSWORD, POSTGRES_USER, POSTGRES_DB). Example workflow.env:

Example Variables
IMAGE_DIGEST=sha256:<digest>
POSTGRES_PASSWORD=<redacted>
POSTGRES_USER=postgres
POSTGRES_DB=secure_workflow_system

SEED_ADMIN_EMAIL=<redacted>
SEED_ADMIN_PASSWORD=<redacted>

SEED_USER_EMAIL=<redacted>
SEED_USER_PASSWORD=<redacted>

SEED_STAFF_EMAIL=<redacted>
SEED_STAFF_PASSWORD=<redacted>

DATA_PROTECTION_KEY_RING_PATH=/home/app/.aspnet/DataProtection-Keys
3. Pull Updated Container Image

Docker Compose pulls the required image from GHCR.

Command
docker compose -p workflow --env-file workflow.env pull
4. Recreate Containers

The application containers are recreated using the updated image.

Command
docker compose -p workflow --env-file workflow.env up -d --force-recreate
5. Deployment Validation

After deployment, the workflow performs a smoke test using the application health endpoint.

Example
curl -f https://workflow.michaelmckibbin.cloud/health

If the endpoint responds successfully, the deployment is considered healthy.

Persistent Data Protection Keys

ASP.NET Core Data Protection keys are persisted using a Docker volume to prevent authentication and antiforgery failures after container recreation.

Docker Compose Volume
volumes:
  - dpkeys:/home/app/.aspnet/DataProtection-Keys
Rollback Procedure

If a deployment fails, the application can be rolled back to a previous immutable image digest.

1. Identify Previous Working Image

Locate the previous successful image digest from:

GitHub Actions deployment logs
GHCR package history
Docker image history on the VPS
Example
sha256:<previous_digest>

Note: Set this value as IMAGE_DIGEST=sha256:<previous_digest> in the workflow.env file (see step 3).
2. SSH into the VPS
ssh <user>@<server>
cd /docker/secure-workflow-system
3. Update Deployment Environment File

Edit the workflow.env file and set IMAGE_DIGEST to the previous digest.

Example
IMAGE_DIGEST=sha256:<previous_digest>
4. Pull and Redeploy Previous Image
docker compose -p workflow --env-file workflow.env pull
docker compose -p workflow --env-file workflow.env up -d --force-recreate
5. Validate Rollback
Check Container Status
docker compose -p workflow --env-file workflow.env ps
Check Logs
docker compose -p workflow --env-file workflow.env logs workflow --tail=100
Validate Health Endpoint
curl -f https://workflow.michaelmckibbin.cloud/health
Benefits of Immutable Deployments

The migration from mutable tags to immutable image references provides:

Deterministic deployments
Improved rollback capability
Stronger deployment traceability
Reduced risk of deployment drift
Better alignment with DevSecOps practices
Improved auditability and reproducibility
Suggested Screenshots

The following screenshots should be added:

GitHub Actions successful deployment workflow
GHCR package showing image digest
VPS deployment logs
Docker Compose running containers
Successful /health endpoint response
Rollback deployment demonstration
GitHub Pull Request with passing CI checks


### Screenshots are in DevOps folder 
- insert them in the appropriate sections of the document to visually illustrate the deployment and rollback processes.
