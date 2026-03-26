# User Management, Authentication & RBAC System

This document describes the user management, authentication, and Role-Based Access Control (RBAC) system added to AdminMembers.

## ?? Features

### 1. User Authentication
- Username and password-based login
- Secure password hashing using SHA256
- Session token generation
- User activity tracking (last login)

### 2. Role-Based Access Control (RBAC)
Three permission levels:
- **Read** - View data only
- **Write** - Create, update, and delete data
- **ReadWrite** - Full access (combination of Read and Write)

Three default roles:
- **Admin** - Full access (ReadWrite permission)
- **Editor** - Can read and write data (ReadWrite permission)
- **Viewer** - Read-only access (Read permission)

### 3. Audit Logging
All user actions are logged with:
- User who performed the action
- Action type (Create, Update, Delete, Login, etc.)
- Entity type and ID affected
- Details of the change
- IP address
- Timestamp

## ?? Database Schema

### Users Table
```sql
- Id (int, PK)
- Username (string, unique)
- PasswordHash (string)
- Email (string, unique)
- IsActive (bool)
- CreatedAt (DateTime)
- LastLoginAt (DateTime, nullable)
```

### Roles Table
```sql
- Id (int, PK)
- Name (string, unique)
- Description (string)
- Permission (enum: Read=1, Write=2, ReadWrite=3)
```

### UserRoles Table
```sql
- Id (int, PK)
- UserId (int, FK)
- RoleId (int, FK)
```

### AuditLogs Table
```sql
- Id (int, PK)
- UserId (int, FK, nullable)
- Username (string)
- Action (string)
- EntityType (string)
- EntityId (int, nullable)
- Details (string)
- IpAddress (string)
- Timestamp (DateTime)
```

## ?? Getting Started

### 1. Run Database Migration
The migration has already been applied. If you need to reapply:
```powershell
dotnet ef database update
```

### 2. Create Initial Admin User
Run the PowerShell script to create your first admin user:
```powershell
.\create_initial_admin.ps1
```

This will generate SQL that you can run in SQL Server Management Studio or any SQL client.

Alternatively, insert manually:
```sql
USE [AdminMembers]
GO

DECLARE @PasswordHash NVARCHAR(MAX) = '[Your SHA256 hashed password]';
INSERT INTO Users (Username, PasswordHash, Email, IsActive, CreatedAt)
VALUES ('admin', @PasswordHash, 'admin@example.com', 1, GETUTCDATE());

DECLARE @UserId INT = SCOPE_IDENTITY();
INSERT INTO UserRoles (UserId, RoleId) VALUES (@UserId, 1); -- Admin role
GO
```

## ?? API Endpoints

### Authentication Endpoints

#### Login
```http
POST /api/auth/login
Content-Type: application/json

{
  "username": "admin",
  "password": "your-password"
}

Response:
{
  "success": true,
  "message": "Login successful",
  "token": "base64-encoded-token",
  "user": {
    "id": 1,
    "username": "admin",
    "email": "admin@example.com",
    "isActive": true,
    "roles": ["Admin"],
    "permissions": ["ReadWrite"]
  }
}
```

#### Register New User (Admin only)
```http
POST /api/auth/register
Authorization: Bearer {token}
Content-Type: application/json

{
  "username": "newuser",
  "password": "password",
  "email": "user@example.com",
  "roleIds": [2]
}
```

#### Get All Users (Read permission required)
```http
GET /api/auth/users
Authorization: Bearer {token}
```

#### Get User by ID (Read permission required)
```http
GET /api/auth/users/{id}
Authorization: Bearer {token}
```

#### Update User Roles (ReadWrite permission required)
```http
PUT /api/auth/users/{id}/roles
Authorization: Bearer {token}
Content-Type: application/json

[1, 2]  // Array of role IDs
```

#### Change User Password (ReadWrite permission required)
```http
PUT /api/auth/users/{id}/password
Authorization: Bearer {token}
Content-Type: application/json

"newpassword"
```

#### Deactivate User (ReadWrite permission required)
```http
PUT /api/auth/users/{id}/deactivate
Authorization: Bearer {token}
```

### Role Management Endpoints

#### Get All Roles (Read permission required)
```http
GET /api/roles
Authorization: Bearer {token}
```

#### Create Role (ReadWrite permission required)
```http
POST /api/roles
Authorization: Bearer {token}
Content-Type: application/json

{
  "name": "Manager",
  "description": "Can manage members",
  "permission": 3  // ReadWrite
}
```

#### Update Role (ReadWrite permission required)
```http
PUT /api/roles/{id}
Authorization: Bearer {token}
```

#### Delete Role (ReadWrite permission required)
```http
DELETE /api/roles/{id}
Authorization: Bearer {token}
```

### Audit Log Endpoints

#### Get All Logs (ReadWrite permission required)
```http
GET /api/auditlogs?pageNumber=1&pageSize=50
Authorization: Bearer {token}
```

#### Get Logs by User (Read permission required)
```http
GET /api/auditlogs/user/{userId}?pageNumber=1&pageSize=50
Authorization: Bearer {token}
```

#### Get Logs by Entity (Read permission required)
```http
GET /api/auditlogs/entity/{entityType}?entityId={id}&pageNumber=1&pageSize=50
Authorization: Bearer {token}
```

## ?? Protected Endpoints

All Member endpoints are now protected with RBAC:

- **GET** `/api/members` - Requires **Read** permission
- **GET** `/api/members/{id}` - Requires **Read** permission
- **POST** `/api/members` - Requires **Write** permission
- **PUT** `/api/members/{id}` - Requires **Write** permission
- **DELETE** `/api/members/{id}` - Requires **Write** permission
- **DELETE** `/api/members/delete-all` - Requires **ReadWrite** permission
- **Export endpoints** - Require **Read** permission

## ?? Usage Example

### JavaScript/Frontend Example

```javascript
// Login
const loginResponse = await fetch('https://localhost:7223/api/auth/login', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    username: 'admin',
    password: 'password'
  })
});

const loginData = await loginResponse.json();
const token = loginData.token;

// Store token for subsequent requests
localStorage.setItem('authToken', token);

// Make authenticated request
const membersResponse = await fetch('https://localhost:7223/api/members', {
  method: 'GET',
  headers: {
    'Authorization': `Bearer ${token}`
  }
});

const members = await membersResponse.json();
```

## ??? Security Notes

1. **Password Security**: Currently using SHA256 for hashing. For production, consider upgrading to bcrypt or Argon2.
2. **Token Security**: Current implementation uses basic Base64 encoding. For production, implement JWT (JSON Web Tokens) with proper signing.
3. **HTTPS**: Always use HTTPS in production to protect credentials in transit.
4. **Token Expiration**: Implement token expiration and refresh mechanisms for production use.

## ?? Audit Log Actions

Common actions logged:
- `Login Success` / `Login Failed`
- `User Created`
- `User Updated`
- `User Roles Updated`
- `Password Changed`
- `User Deactivated`
- `Role Created` / `Role Updated` / `Role Deleted`
- `Member Created` / `Member Updated` / `Member Deleted`

## ?? Permission Levels Explained

### Permission.Read (Value: 1)
Users with this permission can:
- View members
- View their own user information
- View audit logs related to their own actions
- Export data

### Permission.Write (Value: 2)
Users with this permission can:
- Create new members
- Update existing members
- Delete members
- All Read permissions

### Permission.ReadWrite (Value: 3)
Users with this permission can:
- Manage users (create, update, deactivate)
- Manage roles
- View all audit logs
- Delete all members
- All Read and Write permissions

## ?? Migration Information

Migration Name: `AddUserManagementAndRBAC`

Created tables:
- Users
- Roles (with 3 default roles)
- UserRoles
- AuditLogs

To rollback this migration:
```powershell
dotnet ef migrations remove
```

## ?? Support

For issues or questions regarding the authentication and RBAC system, please refer to the source code in:
- `Models/` - User, Role, UserRole, AuditLog, AuthModels
- `Services/` - AuthService, AuditLogService
- `Controllers/` - AuthController, RolesController, AuditLogsController
- `Attributes/` - RequirePermissionAttribute
- `Middleware/` - AuthenticationMiddleware
