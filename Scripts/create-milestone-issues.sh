#!/bin/bash

REPO="MichaelMcKibbin/secure-workflow-system"

echo "Creating Milestone 5 & 6 GitHub issues..."

#
# -----------------------------
# Milestone 5:
# Deployment & Multi-Environment Pipeline
# -----------------------------
#

gh issue create --repo $REPO \
  --title "Add staging environment support" \
  --body "Create a staging deployment environment to validate changes before production release." \
  --label "devops,ci-cd,enhancement"

gh issue create --repo $REPO \
  --title "Configure GitHub Environments" \
  --body "Set up GitHub Environments for develop/staging/production with environment-specific secrets and protection rules." \
  --label "devops,github-actions,security"

gh issue create --repo $REPO \
  --title "Add deployment approval gates" \
  --body "Require manual approval before deployment to production environment." \
  --label "devops,security,ci-cd"

gh issue create --repo $REPO \
  --title "Configure environment-specific secrets" \
  --body "Separate development, staging, and production secrets using GitHub Environments and VPS environment files." \
  --label "security,devops"

gh issue create --repo $REPO \
  --title "Add deployment concurrency controls" \
  --body "Prevent overlapping deployments by configuring GitHub Actions concurrency controls." \
  --label "github-actions,ci-cd"

gh issue create --repo $REPO \
  --title "Add automated smoke testing after deployment" \
  --body "Validate deployment success automatically using health endpoint checks after VPS deployment." \
  --label "testing,ci-cd"

#
# -----------------------------
# Milestone 6:
# Documentation, Metrics & Final Submission
# -----------------------------
#

gh issue create --repo $REPO \
  --title "Improve CI/CD pipeline documentation" \
  --body "Document workflow architecture, deployment flow, rollback process, and DevSecOps implementation for FYP and CA2 report." \
  --label "documentation,devops"

gh issue create --repo $REPO \
  --title "Capture DORA metrics from GitHub Actions" \
  --body "Collect and evaluate deployment frequency, lead time, change failure rate, and MTTR using workflow execution data." \
  --label "metrics,devops"

gh issue create --repo $REPO \
  --title "Create DevOps architecture diagrams" \
  --body "Produce updated diagrams for CI/CD workflows, deployment architecture, and GitOps pipeline." \
  --label "documentation,architecture"

gh issue create --repo $REPO \
  --title "Prepare screencast demonstration" \
  --body "Record a screencast demonstrating GitHub Actions workflows, CI/CD pipeline, project board automation, and deployment process." \
  --label "documentation,presentation"

gh issue create --repo $REPO \
  --title "Finalise DevOps CA2 report" \
  --body "Complete the DevOps assignment report including aims, methodology, evaluation, conclusions, and references." \
  --label "documentation,assignment"

gh issue create --repo $REPO \
  --title "Finalise FYP implementation and testing chapter" \
  --body "Complete implementation/testing documentation for the Secure Workflow System final report." \
  --label "documentation,testing"

gh issue create --repo $REPO \
  --title "Review repository for final submission readiness" \
  --body "Verify branch protection, workflows, documentation, issues, project boards, and deployment stability before submission." \
  --label "devops,documentation"

echo "Milestone 5 & 6 issues created!"