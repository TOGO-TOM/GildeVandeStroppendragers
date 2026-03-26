# Deployment Guide for AdminMembers Application

## Prerequisites
- .NET 8 SDK installed
- SQL Server LocalDB (comes with Visual Studio)

## Quick Start Commands

### 1. Navigate to Project Directory
```powershell
cd C:\Temp\AdminMembers\AdminMembers
```

### 2. Restore Dependencies
```powershell
dotnet restore
```

### 3. Apply Database Migrations
```powershell
dotnet ef database update
```

If you don't have EF Core tools installed globally, run:
```powershell
dotnet tool install --global dotnet-ef
```

### 4. Build the Application
```powershell
dotnet build
```

### 5. Run the Application
```powershell
dotnet run
```

The application will start and display URLs like:
```
Now listening on: https://localhost:5001
Now listening on: http://localhost:5000
```

### 6. Access the Application

Open your browser and navigate to:
- **Main Application**: `https://localhost:5001/index.html`
- **Swagger API Docs**: `https://localhost:5001/swagger`

## Application Features

### Pages Available:
- `/index.html` - Landing page
- `/home.html` - Dashboard
- `/members.html` - Member management (CRUD operations)
- `/export.html` - Export data to Excel/PDF
- `/settings.html` - Settings and custom fields
- `/status.html` - System status

### Sample CSV Files:
Located in `wwwroot/` folder:
- `sample_members_comma.csv` - Comma-delimited sample
- `sample_members_semicolon.csv` - Semicolon-delimited sample
- `sample_members_special_chars.csv` - With special characters

## Troubleshooting

### If LocalDB is not available:
1. Check if SQL Server LocalDB is installed:
   ```powershell
   sqllocaldb info
   ```

2. If not installed, download SQL Server Express with LocalDB:
   https://www.microsoft.com/en-us/sql-server/sql-server-downloads

### If migrations fail:
1. Delete the database and recreate:
   ```powershell
   dotnet ef database drop
   dotnet ef database update
   ```

### If port is already in use:
- Edit `Properties/launchSettings.json` to change the port numbers

## Testing the Application

1. **Import Members**:
   - Go to Members page
   - Click "Import CSV"
   - Use one of the sample CSV files

2. **Create Custom Fields**:
   - Go to Settings page
   - Add custom fields for members

3. **Export Data**:
   - Go to Export page
   - Choose Excel or PDF format
   - Export filtered or all data

4. **Bulk Operations**:
   - Select multiple members
   - Use bulk update or delete features

## Default Database
- **Type**: SQL Server LocalDB
- **Name**: AdminMembersDb
- **Location**: Auto-managed by LocalDB
- **Connection String**: See `appsettings.json`

## Stop the Application
- Press `Ctrl+C` in the terminal/command prompt
