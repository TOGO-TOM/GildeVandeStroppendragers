# ?? Quick Start - Authentication System

## Login Now!

1. **Start the application:**
   ```bash
   dotnet run
   ```
   or press `F5` in Visual Studio

2. **Open your browser:**
   ```
   https://localhost:7223/
   ```

3. **Login with default credentials:**
   ```
   Username: admin
   Password: katoennatie
   ```

## Add Your First User

1. Login as admin
2. Click **Settings** in the menu
3. Scroll to **"?? User Management & Roles"**
4. Click **"+ Add New User"**
5. Fill in:
   - Username (e.g., "john")
   - Email (e.g., "john@example.com")
   - Password (minimum 6 characters)
   - Check role(s): Admin / Editor / Viewer
6. Click **"Save User"**

## Role Permissions

| Action | Admin | Editor | Viewer |
|--------|-------|--------|--------|
| View Members | ? | ? | ? |
| Add/Edit Members | ? | ? | ? |
| Delete Members | ? | ? | ? |
| Manage Users | ? | ? | ? |
| Configure Settings | ? | ? | ? |

## Common Tasks

### Change a User's Role
1. Settings ? User Management
2. Click **"Edit Roles"** on the user
3. Check/uncheck roles
4. Click **"Update Roles"**

### Reset a User's Password
1. Settings ? User Management
2. Click **"Change Password"** on the user
3. Enter new password
4. Click OK

### Deactivate a User
1. Settings ? User Management
2. Click **"Deactivate"** on the user
3. Confirm deactivation
4. User can no longer login

## Files Structure

```
wwwroot/
??? login.html          # Login page (entry point)
??? auth.js             # Authentication utilities
??? home.html           # Dashboard (protected)
??? members.html        # Members page (protected)
??? settings.html       # Settings + User Management (protected)
??? export.html         # Export page (protected)
```

## Troubleshooting

**Can't login?**
- Username: `admin` (lowercase)
- Password: `katoennatie` (exactly as shown)
- Clear browser cache: Ctrl+Shift+Delete

**User Management not visible?**
- Only **Admin** role can see this section
- Make sure you're logged in as admin

**Build errors?**
- Run: `dotnet clean`
- Run: `dotnet build`
- Delete `bin/` and `obj/` folders if needed

## Next Steps

1. ? Login with admin account
2. ? Create a user for yourself (if not admin)
3. ? Test different role permissions
4. ? Change default admin password for security
5. ?? Start managing your members!

---

**Need help?** Check `AUTHENTICATION_IMPLEMENTATION.md` for detailed documentation.
