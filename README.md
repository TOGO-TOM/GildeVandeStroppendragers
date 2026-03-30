# AdminMembers - Member Administration System

A modern ASP.NET Core 8 application for managing member information with full CRUD operations, authentication, and role-based access control.

## ?? Features

### Core Functionality
- **Member Management** - Add, edit, delete, and view members
- **Address Management** - Complete address information for each member
- **Custom Fields** - Define custom fields for additional member data
- **Photo Upload** - Store member photos (base64, max 5MB)
- **Search & Sort** - Find members quickly with advanced filtering
- **Import/Export** - CSV import and Excel/PDF/CSV export
- **Backup & Restore** - Encrypted database backups

### Authentication & Security
- **Role-Based Access Control (RBAC)** - Admin, Editor, Viewer roles
- **Session Management** - 15-minute timeout with activity tracking
- **Audit Logging** - Track all system changes
- **Secure Password Hashing** - BCrypt with salt
- **CSRF Protection** - Built-in anti-forgery tokens
- **HttpOnly Cookies** - XSS-safe authentication

### Technology Stack
- **Frontend**: ASP.NET Core Razor Pages
- **Backend**: .NET 8 with Entity Framework Core
- **Database**: SQL Server LocalDB (Development) / SQL Server (Production)
- **Authentication**: Custom session-based auth
- **UI**: Modern CSS with responsive design

## ?? Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server LocalDB or SQL Server
- Visual Studio 2022 or VS Code

## ??? Installation

### 1. Clone the Repository
```bash
git clone https://github.com/Goderis-ToGo/GildeVanDeStroppendragers
cd GildeVanDeStroppendragers
```

### 2. Restore Dependencies
```bash
dotnet restore
```

### 3. Update Database
```bash
dotnet ef database update
```

### 4. Run the Application
```bash
dotnet run
```

The application will be available at `https://localhost:7223`

## ?? Default Admin Account

Create an admin user:
```bash
dotnet run -- create-admin
```

Or use the CreateAdminUser tool:
```bash
cd Tools
dotnet run --project CreateAdminTool
```

## ?? Project Structure

```
AdminMembers/
??? Controllers/              # API Controllers
??? Pages/                    # Razor Pages (Login, Home, Settings, Members)
??? Models/                   # Data Models
??? Services/                 # Business Logic
??? Data/                     # Database Context
??? Middleware/              # Custom Middleware
??? Migrations/              # EF Core Migrations
??? wwwroot/                 # Static Files (CSS)
```

## ?? Quick Start

1. **Login**: Navigate to `https://localhost:7223` ? redirects to `/Login`
2. **Dashboard**: After login, view the home dashboard
3. **Members**: Click "View Members" to see the member list
4. **Settings**: Access settings to configure the application

## ?? Key API Endpoints

### Authentication
- `POST /api/auth/login` - Login
- `POST /api/auth/register` - Register user

### Members
- `GET /api/members` - Get all members
- `POST /api/members` - Create member
- `PUT /api/members/{id}` - Update member
- `DELETE /api/members/{id}` - Delete member
- `POST /api/members/import/csv` - Import CSV
- `GET /api/members/export/excel` - Export to Excel

### Settings
- `GET /api/settings` - Get settings
- `POST /api/settings/custom-fields` - Create custom field

## ?? Security & Roles

| Role | Permissions |
|------|-------------|
| **Admin** | Full access to all features |
| **Editor** | Read and write access to members |
| **Viewer** | Read-only access |

**Session**: 15-minute timeout, HttpOnly cookies

## ?? Deployment

### Production Build
```bash
dotnet publish -c Release -o ./publish
```

### Run in Production
```bash
cd publish
ASPNETCORE_ENVIRONMENT=Production dotnet AdminMembers.dll
```

## ?? Additional Documentation

- **COMPLETE_RAZOR_CONVERSION.md** - Full Razor Pages architecture
- **AUTHENTICATION_RBAC_README.md** - Authentication details
- **SESSION_TIMEOUT_IMPLEMENTATION.md** - Session management
- **DEPLOYMENT_GUIDE.md** - Deployment guide

## ?? License

This project is private and proprietary.

## ?? Authors

- **Goderis-ToGo**

---

**Built with ?? using .NET 8**
