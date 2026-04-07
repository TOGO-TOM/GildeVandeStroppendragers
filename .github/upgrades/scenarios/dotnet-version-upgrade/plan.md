# .NET Version Upgrade Plan

## Overview

**Target**: Upgrade AdminMembers.csproj from net8.0 to net10.0
**Scope**: Single ASP.NET Core project, 11 NuGet packages, 46 identified issues (11 mandatory, 34 potential, 1 optional)

### Selected Strategy
**All-At-Once** — Single project upgraded in one operation, organized by concern.
**Rationale**: 1 project, currently on net8.0, clear upgrade path to net10.0.

## Tasks

### 01-update-project: Update target framework and NuGet packages

Update the AdminMembers.csproj target framework from `net8.0` to `net10.0` and upgrade all NuGet packages that require version bumps:
- Microsoft.EntityFrameworkCore.SqlServer 8.0.0 → 10.0.5
- Microsoft.EntityFrameworkCore.InMemory 8.0.0 → 10.0.5
- Microsoft.EntityFrameworkCore.Tools 8.0.0 → 10.0.5

Address the deprecated Azure.Identity package — check for a replacement or updated version.

**Done when**: Project targets net10.0, all packages are updated to compatible versions, `dotnet restore` succeeds.

---

### 02-fix-breaking-changes: Resolve API breaking changes and code issues

Fix the 10 binary-incompatible and 16 source-incompatible API changes identified in the assessment across 10 affected files. Address the 15 behavioral changes where runtime behavior may differ.

Key areas: API signature changes, removed/obsolete APIs, EF Core behavioral differences between 8.0 and 10.0.

**Done when**: Solution builds with zero errors, no unresolved breaking change warnings.

---

### 03-run-tests: Validate with tests

Run the full test suite to confirm the upgrade did not break existing functionality. Fix any test failures caused by behavioral changes or updated APIs.

**Done when**: All tests pass.
