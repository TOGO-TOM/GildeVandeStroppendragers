# ?? DIAGNOSTICS - Stats Counter Showing 0

## Issue
Overview/home page showing 0 (or -) for member counts instead of actual numbers.

## ?? Diagnostic Steps

### Step 1: Check Browser Console
1. Open the application: `https://localhost:7223/`
2. Login with: `admin` / `katoennatie`
3. When home page loads, press `F12` to open Developer Tools
4. Go to **Console** tab
5. Look for these messages:

#### Expected Output (Working)
```
=== Home Page Loading ===
auth.js loaded: true
fetchWithAuth available: true
User from checkAuth: {username: 'admin', roles: ['Admin'], ...}
User authenticated: admin
User header elements: {userHeader: div, userName: div, userRole: div}
Loading stats...
Loading stats from: /api/members
Stats response status: 200
Members loaded for stats: Array(25)
Members count: 25
Calculated stats: {total: 25, alive: 20, deceased: 5}
Updating elements: {totalEl: span, aliveEl: span, deceasedEl: span}
Stats updated successfully!
=== Home Page Loaded ===
```

#### Error Output (Not Working)
```
=== Home Page Loading ===
auth.js loaded: true
fetchWithAuth available: false   ? PROBLEM!
// OR
Stats response status: 401        ? AUTH PROBLEM!
// OR
Error loading stats: TypeError    ? CODE PROBLEM!
```

### Step 2: Check Network Tab
1. Stay in Developer Tools (F12)
2. Go to **Network** tab
3. Refresh page (F5)
4. Look for request to `/api/members`
5. Click on it to see details

#### Expected (Working)
- **Status:** 200 OK
- **Request Headers:** Contains `Authorization: Bearer {token}`
- **Response:** JSON array with member objects

#### Error Cases
- **Status:** 401 Unauthorized ? Token not sent or invalid
- **Status:** 403 Forbidden ? Token valid but no permission
- **Status:** 404 Not Found ? API not running

### Step 3: Check LocalStorage
```javascript
// In console:
localStorage.getItem('authToken')
// Should return: "base64string..."

localStorage.getItem('currentUser')
// Should return: '{"id":1,"username":"admin","roles":["Admin"],...}'

// If null or undefined, token wasn't stored at login
```

### Step 4: Manual API Test
```javascript
// In console, after login:
const token = localStorage.getItem('authToken');
console.log('Token:', token);

fetch('/api/members', {
    headers: {
        'Authorization': `Bearer ${token}`
    }
})
.then(r => r.json())
.then(data => console.log('Members:', data))
.catch(err => console.error('Error:', err));
```

## ?? Common Causes & Fixes

### Cause 1: auth.js Not Loaded
**Symptom:** `fetchWithAuth available: false`

**Fix:** Ensure `<script src="auth.js"></script>` is in the HTML **before** the inline script:
```html
<script src="auth.js"></script>
<script>
    // Your code that uses fetchWithAuth
</script>
```

**Already Fixed:** ? home.html has correct order

### Cause 2: Token Not Stored
**Symptom:** `localStorage.getItem('authToken')` returns `null`

**Fix:** Check login.html stores token correctly
```javascript
localStorage.setItem('authToken', data.token);
localStorage.setItem('currentUser', JSON.stringify(data.user));
```

**Already Fixed:** ? login.html stores token

### Cause 3: fetchWithAuth Not Working
**Symptom:** API returns 401, but token exists

**Fix:** Check fetchWithAuth implementation
```javascript
function fetchWithAuth(url, options = {}) {
    const token = getAuthToken();

    if (!options.headers) {
        options.headers = {};
    }

    if (token) {
        options.headers['Authorization'] = `Bearer ${token}`;
    }

    return fetch(url, options);
}
```

**Already Fixed:** ? fetchWithAuth is correct

### Cause 4: Members Endpoint Returns Empty Array
**Symptom:** `Members count: 0` in console

**Fix:** Check if members exist in database
```sql
-- Run in SQL Server
SELECT COUNT(*) FROM Members;
```

**How to Add Test Members:**
- Go to Members page after login
- Add a few test members manually
- Or import from CSV

### Cause 5: Script Timing Issue
**Symptom:** Stats load before page is ready

**Fix:** Already implemented with DOMContentLoaded, but add fallback

**Already Fixed:** ? Using DOMContentLoaded

## ??? Quick Fixes to Try

### Fix 1: Hard Refresh
```
Ctrl + Shift + R (Windows)
Cmd + Shift + R (Mac)
```

### Fix 2: Clear Cache & Storage
```javascript
// Console:
localStorage.clear()
sessionStorage.clear()
location.reload()
```

### Fix 3: Check Token Manually
```javascript
// Console:
const token = localStorage.getItem('authToken');
fetch('/api/members', {
    headers: { 'Authorization': `Bearer ${token}` }
})
.then(r => r.json())
.then(d => console.log('Count:', d.length));
```

### Fix 4: Test Without Auth First
To verify the issue is auth-related, temporarily remove the attribute:
```csharp
// In MembersController.cs
[HttpGet]
// [RequirePermission(Permission.Read)]  ? Comment out temporarily
public async Task<ActionResult<IEnumerable<Member>>> GetMembers()
```

Then test if stats load. If they do, it confirms auth header issue.

## ?? Expected vs Actual

### Expected Behavior
```
1. Login successful
2. Redirect to home.html
3. checkAuth() returns user
4. loadStats() calls fetchWithAuth('/api/members')
5. Request includes Authorization header
6. Server returns member array
7. Calculate stats: total=25, alive=20, deceased=5
8. Update DOM elements
9. Display: 25, 20, 5
```

### Current Behavior (If Broken)
```
1. Login successful ?
2. Redirect to home.html ?
3. checkAuth() returns user ?
4. loadStats() calls fetchWithAuth('/api/members') ?
5. Request might be missing Authorization header ?
6. Server returns 401 or empty array ?
7. Stats stay at default: -, -, - or 0, 0, 0
```

## ?? Advanced Debugging

### Enable Verbose Logging
Already added! Check console for:
```
=== Home Page Loading ===
Loading stats from: /api/members
Stats response status: 200
Members loaded for stats: [...]
Calculated stats: {total: X, alive: Y, deceased: Z}
Stats updated successfully!
```

### Check Response Body
```javascript
// In console during stats load:
const response = await fetchWithAuth('/api/members');
const text = await response.text();
console.log('Raw response:', text);
const data = JSON.parse(text);
console.log('Parsed data:', data);
console.log('Count:', data.length);
```

### Verify Token Format
```javascript
const token = localStorage.getItem('authToken');
try {
    const decoded = atob(token);
    console.log('Decoded token:', decoded);
    // Should show: "1:admin:timestamp"
} catch (e) {
    console.error('Invalid token format:', e);
}
```

## ?? Immediate Action Plan

### If Stats Showing 0 or Error:

1. **Open browser console** (F12)
2. **Login** to the system
3. **Read console messages** - Look for errors
4. **Check Network tab** - Verify /api/members request
5. **Copy error message** - Share for further diagnosis

### If No Members in Database:

1. Go to **Members page**
2. Click **"+ Add New Member"**
3. Fill in required fields
4. Click **Save**
5. Go back to **Home** page
6. Stats should update

### If Auth Header Missing:

The code should work, but if it doesn't:
```javascript
// In home.html, replace fetchWithAuth with this test:
const token = localStorage.getItem('authToken');
const response = await fetch(API_URL, {
    headers: { 'Authorization': `Bearer ${token}` }
});
```

## ?? What to Report

If stats still show 0, please share:

1. **Browser Console Output** (all messages)
2. **Network Tab** - /api/members request details
3. **LocalStorage Data:**
   ```javascript
   localStorage.getItem('authToken')
   localStorage.getItem('currentUser')
   ```
4. **Error Messages** - Any red errors in console

## ? Verification Checklist

After applying fixes:
- [ ] Browser console shows "Stats updated successfully!"
- [ ] Network tab shows 200 OK for /api/members
- [ ] Authorization header present in request
- [ ] Response contains member array
- [ ] Stats display actual numbers (not 0 or -)
- [ ] Timer shows in header (e.g., "?? 15m")
- [ ] No errors in console

## ?? Expected Result

After proper fix:
```
Home Page
???????????????????????????????????
?  Total Members: 25               ?
?  Alive: 20                       ?
?  Deceased: 5                     ?
???????????????????????????????????
```

---

**Next Step:** Run the app and check browser console for diagnostic messages!
