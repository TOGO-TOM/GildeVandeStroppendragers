# ?? Desktop Launch Guide for AdminMembers

## Quick Start - Choose Your Preferred Method

### ? **EASIEST METHOD - Double-Click Any of These Files:**

1. **`Launch-AdminMembers.bat`** ? RECOMMENDED
   - Windows Batch File
   - No security warnings
   - Automatically starts server and opens browser
   - Can be copied to your desktop

2. **`Launch-AdminMembers.vbs`**
   - Visual Basic Script
   - Cleanest interface with message boxes
   - No command window clutter
   - Can be copied to your desktop

3. **`Launch-AdminMembers.ps1`**
   - PowerShell script (most robust)
   - Better error handling
   - May require: Right-click ? "Run with PowerShell"

---

## ?? What These Files Do

All launcher files perform these steps:
1. ? Check if .NET is installed
2. ? Update the database (safe, preserves data)
3. ? Start the AdminMembers server
4. ? Wait for server to be ready
5. ? Automatically open your browser to the app
6. ? Show you helpful information

---

## ??? Creating a Desktop Shortcut

### Method 1: Copy Launcher to Desktop
```
1. Right-click on "Launch-AdminMembers.bat"
2. Select "Copy"
3. Go to your Desktop
4. Right-click and select "Paste"
5. Double-click the file on your desktop to launch
```

### Method 2: Create a Shortcut
```
1. Right-click on "Launch-AdminMembers.bat"
2. Select "Send to" ? "Desktop (create shortcut)"
3. Optionally rename it to "AdminMembers"
4. Double-click the shortcut to launch
```

### Method 3: Create Custom Shortcut (Advanced)
```
1. Right-click on Desktop ? New ? Shortcut
2. Browse to: C:\Temp\AdminMembers\AdminMembers\Launch-AdminMembers.bat
3. Click Next
4. Name it: "AdminMembers Application"
5. Click Finish
6. (Optional) Right-click shortcut ? Properties ? Change Icon
```

---

## ?? Application URLs

After launching, you can access:

| Page | URL | Description |
|------|-----|-------------|
| **Landing Page** | https://localhost:7223/index.html | Start here |
| **Home Dashboard** | https://localhost:7223/home.html | Main dashboard |
| **Members** | https://localhost:7223/members.html | Manage members |
| **Settings** | https://localhost:7223/settings.html | Custom fields & settings |
| **Export** | https://localhost:7223/export.html | Export to Excel/PDF |
| **Status** | https://localhost:7223/status.html | System status |
| **API Docs** | https://localhost:7223/swagger | Swagger API documentation |

Alternative ports (if HTTPS doesn't work):
- http://localhost:5000/index.html

---

## ?? How to Stop the Application

### If you used `.bat` or `.vbs` launcher:
- Find the window titled "AdminMembers Server"
- Close the window, or press `Ctrl+C` in that window

### If you used `.ps1` launcher:
- Press `Ctrl+C` in the PowerShell window
- Or simply close the PowerShell window

---

## ?? Troubleshooting

### Problem: "Cannot find .NET"
**Solution:** Install .NET 8 SDK from:
https://dotnet.microsoft.com/download/dotnet/8.0

### Problem: "Port already in use"
**Solution:** 
1. Close any running instance of AdminMembers
2. Check Task Manager for `dotnet.exe` processes
3. Or edit `Properties\launchSettings.json` to use different ports

### Problem: Browser doesn't open automatically
**Solution:** 
Manually open your browser and go to:
https://localhost:7223/index.html

### Problem: PowerShell script won't run (security warning)
**Solution - Option 1:** Use the `.bat` file instead (no warnings)
**Solution - Option 2:** Right-click ? "Run with PowerShell"
**Solution - Option 3:** Open PowerShell as Admin and run:
```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

### Problem: SSL Certificate Warning in Browser
**Solution:** 
- Click "Advanced" ? "Continue to localhost (unsafe)"
- This is normal for local development
- Or use: http://localhost:5000/index.html

---

## ?? What You Need

- ? .NET 8 SDK (included with Visual Studio)
- ? SQL Server LocalDB (included with Visual Studio)
- ? Windows 10/11
- ? Any modern web browser (Chrome, Edge, Firefox)

---

## ?? Quick Start Steps

1. **Double-click** `Launch-AdminMembers.bat` on your desktop
2. **Wait** 5-10 seconds for the server to start
3. **Browser opens** automatically to the application
4. **Start using** AdminMembers!

---

## ?? Tips

- **Bookmark** https://localhost:7223/home.html for quick access
- **Keep the server window open** while using the application
- **Don't close** the "AdminMembers Server" window while working
- **Your data is safe** - it's stored in SQL Server LocalDB
- **Run launcher again** if you need to restart the server

---

## ?? Getting Help

If you encounter issues:
1. Check the server window for error messages
2. Check your browser's Developer Console (F12)
3. Review the `DEPLOYMENT_GUIDE.md` file
4. Check the Output window in Visual Studio

---

## ?? Security Note

The application runs locally on your computer:
- Only accessible from your machine
- No internet connection required (except for initial .NET restore)
- Database is stored locally
- All data stays on your laptop

---

Enjoy using AdminMembers! ??
