# Scenario Instructions — .NET Version Upgrade

## Scenario Parameters

- **Solution**: AdminMembers.sln
- **Target Framework**: net10.0 (.NET 10.0 LTS)
- **Source Framework**: net8.0 (.NET 8)
- **Source Branch**: main
- **Working Branch**: upgrade-to-NET10-1

## Strategy
**Selected**: All-at-Once
**Rationale**: Single project (AdminMembers.csproj), on net8.0, straightforward upgrade path to net10.0.

### Execution Constraints
- Single atomic upgrade — all TFMs and packages updated together
- Validate full solution build after upgrade
- Testing comes after the atomic upgrade completes

## Preferences

### Flow Mode
**Automatic** — Run end-to-end, only pause when blocked or needing user input.

### Commit Strategy
**Single Commit at End** — One atomic upgrade, one commit.

### Technical Preferences
*(none yet)*

### Execution Style
*(none yet)*

### Custom Instructions
*(none yet)*

## Key Decisions Log
- **2025-07-11**: User confirmed .NET 10.0 (LTS) as target. Pending changes committed to main. Working branch `upgrade-to-NET10-1` created.
- **2025-07-11**: Strategy selected: All-at-Once. Single project, straightforward upgrade.
