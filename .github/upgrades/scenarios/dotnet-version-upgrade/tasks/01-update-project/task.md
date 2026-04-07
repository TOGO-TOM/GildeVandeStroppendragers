# 01-update-project: Update target framework and NuGet packages

Update the AdminMembers.csproj target framework from `net8.0` to `net10.0` and upgrade all NuGet packages that require version bumps:
- Microsoft.EntityFrameworkCore.SqlServer 8.0.0 → 10.0.5
- Microsoft.EntityFrameworkCore.InMemory 8.0.0 → 10.0.5
- Microsoft.EntityFrameworkCore.Tools 8.0.0 → 10.0.5

Address the deprecated Azure.Identity package — check for a replacement or updated version.

**Done when**: Project targets net10.0, all packages are updated to compatible versions, `dotnet restore` succeeds.
