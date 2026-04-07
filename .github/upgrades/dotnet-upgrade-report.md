# .NET 10.0 Upgrade Report

## Project target framework modifications

| Project name       | Old Target Framework | New Target Framework | Commits                              |
|:-------------------|:--------------------:|:--------------------:|:-------------------------------------|
| AdminMembers.csproj | net8.0              | net10.0              | dc48ed7f, 5f803ee8, 7bda1530        |

## NuGet Packages

| Package Name                              | Old Version | New Version                    | Commit Id        |
|:------------------------------------------|:-----------:|:------------------------------:|:-----------------|
| Azure.Identity                            | 1.14.0      | 1.20.0                         | 5f803ee8         |
| BouncyCastle.Cryptography                 | —           | 2.7.0-beta.98                  | 7bda1530         |
| ClosedXML                                 | —           | 0.104.0                        | 7bda1530         |
| MailKit                                   | 4.12.0      | 4.15.1                         | 7bda1530         |
| Microsoft.EntityFrameworkCore.InMemory    | 8.0.0       | 10.0.5                         | 5f803ee8         |
| Microsoft.EntityFrameworkCore.SqlServer   | 8.0.0       | 10.0.5                         | 5f803ee8         |
| Microsoft.EntityFrameworkCore.Tools       | 8.0.0       | 10.0.5                         | 5f803ee8         |
| Otp.NET                                   | 2.1.0       | 1.4.1                          | 7bda1530         |
| System.IO.Packaging                       | —           | 11.0.0-preview.2.26159.112     | 7bda1530         |

## All commits

| Commit ID  | Description                                                        |
|:-----------|:-------------------------------------------------------------------|
| c497b0d0   | Commit upgrade plan                                                |
| dc48ed7f   | Update target framework to net10.0 in AdminMembers.csproj          |
| 5f803ee8   | Update package versions in AdminMembers.csproj                     |
| 7bda1530   | Update package references in AdminMembers.csproj                   |

## Project feature upgrades

### AdminMembers.csproj

- Target framework changed from `net8.0` to `net10.0`
- Azure.Identity updated from 1.14.0 to 1.20.0 (resolved deprecated MSAL dependency)
- Entity Framework Core packages (InMemory, SqlServer, Tools) updated from 8.0.0 to 10.0.5
- MailKit updated from 4.12.0 to 4.15.1 (resolves MimeKit vulnerability)
- Otp.NET updated to 1.4.1 (latest compatible version for net10.0)
- ClosedXML updated to 0.104.0
- Added BouncyCastle.Cryptography 2.7.0-beta.98 and System.IO.Packaging 11.0.0-preview.2 as explicit dependencies to resolve transitive vulnerability warnings

## Next steps

- Review and test the application to ensure all features work correctly with .NET 10.0
- Consider migrating to Azure for hosting (Azure migration session was started separately)
- Monitor for stable releases of preview/beta packages (BouncyCastle.Cryptography, System.IO.Packaging) and update when GA versions are available
