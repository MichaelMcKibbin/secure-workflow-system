#!/bin/bash

REPO="MichaelMcKibbin/secure-workflow-system"

echo "Creating app development milestones..."

create_milestone() {
  TITLE="$1"
  DESCRIPTION="$2"

  gh api repos/$REPO/milestones \
    -f title="$TITLE" \
    -f description="$DESCRIPTION" \
    >/dev/null 2>&1 || echo "Milestone may already exist: $TITLE"
}

create_issue() {
  TITLE="$1"
  BODY="$2"
  MILESTONE="$3"

  echo "Creating issue: $TITLE"

  gh issue create \
    --repo "$REPO" \
    --title "$TITLE" \
    --body "$BODY" \
    --milestone "$MILESTONE"
}

create_milestone "Core Workflow Functionality" "Build the main case/ticket workflow features for the secure workflow system."
create_milestone "Authentication, Roles & Admin" "Complete registration, approval, role management, and admin functions."
create_milestone "User Interface & Styling" "Improve layout, usability, navigation, and CSS styling."
create_milestone "Testing & Final Polish" "Add tests, validation, bug fixes, and final project clean-up."

echo "Creating issues..."

# Core Workflow Functionality
create_issue "Create case workflow dashboard" \
"Build a dashboard showing cases by status, assigned user, and recent activity." \
"Core Workflow Functionality"

create_issue "Improve case creation form" \
"Enhance the create case form with validation, clearer fields, and user-friendly feedback." \
"Core Workflow Functionality"

create_issue "Add case status transition rules" \
"Define and enforce allowed workflow transitions such as New, Assigned, In Progress, Resolved, and Closed." \
"Core Workflow Functionality"

create_issue "Add case assignment functionality" \
"Allow Staff/Admin users to assign cases to staff members." \
"Core Workflow Functionality"

create_issue "Add case history/audit trail" \
"Record important case events such as creation, assignment, status changes, and closure." \
"Core Workflow Functionality"

create_issue "Add filtering and search to case list" \
"Allow users to filter cases by status, assigned user, creator, and keyword." \
"Core Workflow Functionality"

# Authentication, Roles & Admin
create_issue "Complete external user registration flow" \
"Allow users to register on the live deployment while preventing access until approved." \
"Authentication, Roles & Admin"

create_issue "Implement pending user approval workflow" \
"Add a pending approval process so Admin users can approve or reject newly registered users." \
"Authentication, Roles & Admin"

create_issue "Create admin user management page" \
"Build an Admin-only page for viewing users, approval status, and assigned roles." \
"Authentication, Roles & Admin"

create_issue "Add role assignment controls" \
"Allow Admin users to assign User, Staff, and Admin roles safely." \
"Authentication, Roles & Admin"

create_issue "Prevent removal of last admin account" \
"Add a safeguard so the system cannot be left without at least one Admin user." \
"Authentication, Roles & Admin"

create_issue "Add registration notification mechanism" \
"Provide a simple notification method or admin dashboard indicator when new users are awaiting approval." \
"Authentication, Roles & Admin"

# User Interface & Styling
create_issue "Improve main navigation layout" \
"Update navigation so users can easily access cases, admin functions, and account options." \
"User Interface & Styling"

create_issue "Add role-aware navigation links" \
"Show or hide navigation items based on whether the user is Admin, Staff, User, or unauthenticated." \
"User Interface & Styling"

create_issue "Improve CSS styling for forms and tables" \
"Polish forms, tables, buttons, validation messages, and status badges for a more professional look." \
"User Interface & Styling"

create_issue "Add case status badges" \
"Display case status using clear visual badges for New, Assigned, In Progress, Resolved, and Closed." \
"User Interface & Styling"

create_issue "Improve mobile/responsive layout" \
"Check key pages on smaller screens and improve layout where necessary." \
"User Interface & Styling"

# Testing & Final Polish
create_issue "Add unit tests for workflow services" \
"Test core workflow logic including case creation, assignment, and status transitions." \
"Testing & Final Polish"

create_issue "Add tests for role-based access rules" \
"Verify that User, Staff, and Admin accounts can only access permitted functions." \
"Testing & Final Polish"

create_issue "Add validation tests for registration approval" \
"Test pending registration, approval, and approved-user access behaviour." \
"Testing & Final Polish"

create_issue "Review app for final demo readiness" \
"Check the deployed app end-to-end using test accounts and document any final fixes required." \
"Testing & Final Polish"

echo "Done creating app development issues."