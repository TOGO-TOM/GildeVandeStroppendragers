# ? Admin User Created & Password Management

## ?? Admin User Successfully Created!

Your admin user is now active and ready to use.

### ?? Login Credentials

```
Username: admin
Password: katoennatie
Email: admin@gilde.com
Role: Admin (Full ReadWrite access)
```

## ?? How to Change the Password

### Option 1: PowerShell Script (Easiest)
```powershell
# Change password to "newpassword"
.\set_admin_password.ps1 -Password "newpassword"

# Create a different user
.\set_admin_password.ps1 -Username "john" -Password "secret123" -Email "john@gilde.com" -RoleId 2
```

### Option 2: Edit SQL Script
1. Open `create_admin_user.sql`
2. Find line: `DECLARE @PlainPassword NVARCHAR(MAX) = 'katoennatie';`
3. Change `'katoennatie'` to your new password
4. Run the script:
```powershell
sqlcmd -S "(localdb)\mssqllocaldb" -d AdminMembersDb -i create_admin_user.sql
```

## ?? Test Your Login

### Option 1: Test Page
1. Start your application: `dotnet run`
2. Open browser: https://localhost:7223/auth-test.html
3. Click "Login" (credentials are pre-filled)
4. Test loading members with and without authentication

### Option 2: API Direct (Postman/curl)
```bash
# Login
curl -X POST https://localhost:7223/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"katoennatie"}'

# Response will include token
{
  "success": true,
  "token": "BASE64_TOKEN_HERE",
  "user": { ... }
}

# Use token in subsequent requests
curl -X GET https://localhost:7223/api/members \
  -H "Authorization: Bearer BASE64_TOKEN_HERE"
```

## ?? Troubleshooting Members Not Loading

### If members aren't loading after adding auth:

1. **Check if authentication is required**
   - The middleware has been updated to allow unauthenticated requests
   - The RBAC attributes on endpoints will enforce authorization when needed

2. **Test without authentication first**
   - Try: https://localhost:7223/api/members/debug
   - This should work without authentication

3. **Check the browser console**
   - Open Developer Tools (F12)
   - Look for CORS or network errors

4. **Verify database connection**
   ```powershell
   sqlcmd -S "(localdb)\mssqllocaldb" -d AdminMembersDb -Q "SELECT COUNT(*) FROM Members"
   ```

5. **Check if members exist**
   ```sql
   SELECT * FROM Members
   ```

## ?? Important Files Created

| File | Purpose |
|------|---------|
| `create_admin_user.sql` | SQL script to create/update admin user |
| `set_admin_password.ps1` | PowerShell script for easy password changes |
| `wwwroot/auth-test.html` | Interactive test page for authentication |
| `AUTHENTICATION_RBAC_README.md` | Complete API documentation |
| `IMPLEMENTATION_COMPLETE.md` | Implementation summary |

## ?? Quick Start Commands

```powershell
# 1. Verify admin user exists
sqlcmd -S "(localdb)\mssqllocaldb" -d AdminMembersDb -Q "SELECT * FROM Users"

# 2. Start the application
dotnet run

# 3. Open test page
start https://localhost:7223/auth-test.html

# 4. Or test your main app
start https://localhost:7223/home.html
```

## ?? Available Roles in Database

| ID | Role Name | Permission | Description |
|----|-----------|-----------|-------------|
| 1 | Admin | ReadWrite (3) | Full access to everything |
| 2 | Editor | ReadWrite (3) | Can read and write data |
| 3 | Viewer | Read (1) | Read-only access |

## ?? Next Steps

1. **Update your frontend** (`home.html` or main app):
   - Add login form
   - Store token in localStorage
   - Include token in all API requests

2. **Create additional users**:
   ```powershell
   .\set_admin_password.ps1 -Username "editor1" -Password "pass123" -Email "editor@gilde.com" -RoleId 2
   ```

3. **Test the audit log**:
   - Login: https://localhost:7223/api/auth/login
   - Check logs: https://localhost:7223/api/auditlogs (requires Admin token)

## ?? Need Help?

Check these resources:
- **Full Documentation**: `AUTHENTICATION_RBAC_README.md`
- **Test Page**: https://localhost:7223/auth-test.html
- **Swagger UI**: https://localhost:7223/swagger

---

**Status**: ? Admin user created and active  
**Password**: katoennatie (easily adjustable)  
**Ready to use**: Yes!
