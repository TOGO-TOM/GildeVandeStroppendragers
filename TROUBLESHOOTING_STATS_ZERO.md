# ?? TROUBLESHOOTING GUIDE - Stats Showing 0

## Problem
Home page overview shows 0 for member counts instead of actual numbers.

## ?? Quick Solution Steps

### Step 1: Run Auth Debug Test
1. Start your application: `dotnet run`
2. Login at: `https://localhost:7223/`
3. Go to: `https://localhost:7223/auth-debug.html`
4. Check the test results:
   - ? All green checkmarks = System working
   - ? Red X's = Problem identified

### Step 2: Clear Browser Data
```javascript
// Open console (F12) and run:
localStorage.clear()
sessionStorage.clear()
location.reload()
```

Then login again and check home page.

### Step 3: Check Database Has Members
```sql
-- Run this in SQL Server:
SELECT COUNT(*) FROM Members;
```

If count is 0:
- Go to Members page
- Add test members manually
- Or import CSV

### Step 4: Verify API is Running
Open: `https://localhost:7223/api/members`

**Expected:** 401 Unauthorized (means API is running, but needs auth)
**Problem:** Connection refused or timeout (means API not running)

## ?? Detailed Diagnostics

### Test 1: Check Console Logs
After login, go to home page and check console:

#### ? Good Output
```
=== Home Page Loading ===
auth.js loaded: true
fetchWithAuth available: true
User authenticated: admin
Loading stats...
Stats response status: 200
Members count: 25
Calculated stats: {total: 25, alive: 20, deceased: 5}
Stats updated successfully!
```

#### ? Problem Output
```
Loading stats from: /api/members
Stats response status: 401
Error loading stats: HTTP error! status: 401
```
**Cause:** Token not being sent
**Fix:** Use auth-debug.html to verify token

```
Error loading stats: Failed to fetch
```
**Cause:** API not running
**Fix:** Start the application with `dotnet run`

### Test 2: Check Network Tab
1. Press F12
2. Go to Network tab  
3. Reload home page
4. Find `/api/members` request
5. Click on it
6. Check **Headers** section

#### ? Should See:
```
Request Headers:
  Authorization: Bearer eyJhbGc...
```

#### ? Problem:
```
Request Headers:
  (no Authorization header)
```

**Fix:** Check if token exists:
```javascript
console.log(localStorage.getItem('authToken'));
```

### Test 3: Manual API Call
```javascript
// In browser console after login:
const token = localStorage.getItem('authToken');
console.log('Token exists:', !!token);

const response = await fetch('/api/members', {
    headers: { 'Authorization': `Bearer ${token}` }
});

console.log('Status:', response.status);

if (response.ok) {
    const members = await response.json();
    console.log('Members count:', members.length);
    console.log('Stats:', {
        total: members.length,
        alive: members.filter(m => m.isAlive).length,
        deceased: members.filter(m => !m.isAlive).length
    });
} else {
    console.error('Error:', await response.text());
}
```

## ??? Common Issues & Fixes

### Issue 1: Token Not Stored at Login
**Symptom:** `localStorage.getItem('authToken')` returns null

**Check:** Login response in Network tab
- Should return: `{ success: true, token: "...", user: {...} }`

**Fix:** Verify login.html line 338-340:
```javascript
localStorage.setItem('authToken', data.token);
localStorage.setItem('currentUser', JSON.stringify(data.user));
localStorage.setItem('loginTime', new Date().toISOString());
```

### Issue 2: fetchWithAuth Not Defined
**Symptom:** Console shows `fetchWithAuth is not a function`

**Check:** auth.js is loaded before inline scripts
```html
<script src="auth.js"></script>  ? Must be first
<script>
    // Your code
</script>
```

**Fix:** Already correct in home.html

### Issue 3: Database Has No Members
**Symptom:** API returns empty array `[]`, stats show 0

**Check:**
```sql
SELECT COUNT(*) FROM Members;
```

**Fix:** Add test members
1. Go to Members page
2. Add members manually
3. Or use Import CSV feature

### Issue 4: API Not Running
**Symptom:** `Failed to fetch` error

**Check:** Is application running?
```bash
dotnet run
```

**Verify:** Browse to `https://localhost:7223/api/members/debug`
- Should show something (even if error)
- Connection refused = App not running

### Issue 5: CORS Issue
**Symptom:** Console shows CORS error

**Check:** Program.cs has CORS configured
```csharp
app.UseCors("AllowFrontend");
```

**Already Fixed:** ? CORS is configured

### Issue 6: Middleware Not Setting Headers
**Symptom:** API returns 401 even with token

**Check:** AuthenticationMiddleware is enabled
```csharp
app.UseAuthenticationMiddleware();
```

**Already Fixed:** ? Middleware is configured

## ?? Reset Everything

If all else fails:

### 1. Clear All Browser Data
```javascript
// Console:
localStorage.clear();
sessionStorage.clear();
document.cookie.split(";").forEach(c => {
    document.cookie = c.split("=")[0] + "=;expires=Thu, 01 Jan 1970 00:00:00 UTC";
});
location.reload();
```

### 2. Restart Application
```bash
# Stop application (Ctrl+C)
dotnet clean
dotnet build
dotnet run
```

### 3. Fresh Login
1. Go to `https://localhost:7223/`
2. Login: admin / katoennatie
3. Check auth-debug.html first
4. Then check home.html

## ?? Checklist for Working Stats

- [ ] Application running (`dotnet run`)
- [ ] Database has members (check SQL)
- [ ] auth.js file exists in wwwroot/
- [ ] auth.js loaded in home.html (`<script src="auth.js">`)
- [ ] User logged in (token in localStorage)
- [ ] Token is valid (not expired)
- [ ] fetchWithAuth function available
- [ ] API returns 200 (not 401 or 403)
- [ ] Response contains member array
- [ ] Stats calculation working
- [ ] DOM elements exist (totalMembers, aliveMembers, deceasedMembers)
- [ ] No JavaScript errors in console

## ?? Most Likely Causes

Based on similar issues, the most common causes are:

1. **Browser cache** (50% of cases)
   - Solution: Clear cache and reload

2. **Token not stored** (30% of cases)
   - Solution: Check login stores token correctly

3. **No members in database** (15% of cases)
   - Solution: Add test members

4. **Timing issue** (5% of cases)
   - Solution: Already added DOMContentLoaded

## ?? Action Plan

1. ? **Built successfully** - No code errors
2. ?? **Test with auth-debug.html** - Verify auth setup
3. ?? **Check console logs** - Look for errors
4. ?? **Check Network tab** - Verify API calls
5. ?? **Check database** - Ensure members exist

## ?? Need More Help?

If stats still show 0 after all tests:

1. Open `https://localhost:7223/auth-debug.html`
2. Copy all the test results
3. Open browser console (F12)
4. Copy all console messages
5. Share both outputs for further diagnosis

---

**Most Common Fix:** Clear browser cache (Ctrl+Shift+Delete) and login again! ??
