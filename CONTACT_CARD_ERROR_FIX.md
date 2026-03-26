# Contact Card Error - Diagnostic & Fix

## Issue
"Failed to load contact details" error when clicking on a member to view their contact card.

## Changes Applied

### Enhanced Error Handling in `showContactCard()` function

1. **Added detailed logging** to track the request flow:
   - Logs the member ID being requested
   - Logs the response status
   - Logs error responses from the API

2. **Improved error messages** to identify specific issues:
   - **404**: "Member not found" - The member ID doesn't exist in the database
   - **401/403**: "You do not have permission to view this member" - Authentication/authorization issue
   - **Other errors**: Shows the HTTP status code

3. **Better error display** - Shows the actual error message instead of generic "Failed to load contact details"

## How to Debug

### Step 1: Clear Browser Cache
1. Press **Ctrl+Shift+Delete** (or **F12** ? Application ? Clear Storage)
2. Select "Cached images and files"
3. Click "Clear data"
4. Refresh the page with **Ctrl+F5** (hard refresh)

### Step 2: Check Browser Console
1. Press **F12** to open Developer Tools
2. Go to the **Console** tab
3. Click on a member to view their contact card
4. Look for error messages:

```
Loading member details for ID: 123
Response status: 200
Member data loaded: {...}
```

### Step 3: Common Issues & Solutions

#### Issue: "Member not found" (404)
**Cause**: The member ID is invalid or the member was deleted
**Solution**: 
- Refresh the members list (F5)
- If the issue persists, check the database

#### Issue: "You do not have permission" (401/403)
**Cause**: Authentication token expired or insufficient permissions
**Solution**:
- Logout and login again
- Check your user role (should have at least "Read" permission)

#### Issue: Network error or "Failed to fetch"
**Cause**: API is not running or connection issue
**Solution**:
- Verify the application is running (check Visual Studio)
- Check the API URL in browser console (should show `/api/members/{id}`)
- Try accessing the API directly: `https://localhost:7223/api/members/debug`

#### Issue: "getCustomFieldValues is not defined"
**Cause**: Missing JavaScript functions (already fixed)
**Solution**: The functions have been added - just refresh the page

### Step 4: Check Network Tab
1. Press **F12** ? **Network** tab
2. Filter by "Fetch/XHR"
3. Click on a member
4. Look for the request to `/api/members/{id}`
5. Click on it to see:
   - **Status**: Should be 200
   - **Response**: Should show member data with customFieldValues
   - **Headers**: Should include Authorization token

### Step 5: Test API Directly
Open in browser:
```
https://localhost:7223/api/members/1
```

If you get a JSON response, the API is working. If you get an error:
- Check if you're logged in
- Check if the member with ID 1 exists

## Expected Console Output (Success)

```
Loading member details for ID: 141
Response status: 200
Member data loaded: {id: 141, memberNumber: 123, firstName: "John", ...}
Custom field values: [{customFieldId: 1, value: "test", ...}]
```

## Expected Console Output (Error)

```
Loading member details for ID: 999
Response status: 404
API error response: {"error": "Member not found"}
Error showing contact card: Error: Member not found
```

## Testing the Fix

1. **Stop the debugger** if it's running
2. Press **F5** to restart
3. Go to the Members page
4. **Clear browser cache** (Ctrl+Shift+Delete)
5. **Hard refresh** (Ctrl+F5)
6. Click on a member to view their contact card
7. Check the browser console (F12) for detailed error messages

## If Issues Persist

Check these files for potential issues:

### 1. Check `auth.js` is loaded
In browser console, type:
```javascript
typeof fetchWithAuth
```
Should return: `"function"`

### 2. Check auth token exists
In browser console, type:
```javascript
localStorage.getItem('authToken')
```
Should return a token string (not null)

### 3. Check API is accessible
```javascript
fetch('/api/members/debug').then(r => r.json()).then(console.log)
```
Should return database connection info

### 4. Check member exists
```javascript
fetch('/api/members').then(r => r.json()).then(console.log)
```
Should return array of members

## Quick Fix Checklist

- [?] Added enhanced error handling with specific error messages
- [?] Added detailed console logging for debugging
- [?] Added `getCustomFieldValues()`, `setCustomFieldValues()`, and `clearCustomFieldValues()` functions
- [ ] Clear browser cache and hard refresh
- [ ] Check browser console for specific error messages
- [ ] Verify authentication token is valid
- [ ] Test the fix

## Next Steps

1. **Restart the debugger** to apply the changes
2. **Clear browser cache** (very important!)
3. **Try clicking on a member** again
4. **Check the console** (F12) for the new detailed error messages
5. **Report the specific error message** if it still fails

The enhanced error messages will tell you exactly what's wrong!
