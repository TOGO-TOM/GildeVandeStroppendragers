# Delete All Members Function - FIXED ?

## Problem
`ReferenceError: deleteAllMembers is not defined`

The "Delete All (Test)" button in `members.html` was calling a JavaScript function that didn't exist.

## Root Cause
The button HTML in `members.html` had:
```html
<button onclick="deleteAllMembers()" ...>Delete All (Test)</button>
```

However, the `deleteAllMembers()` function was missing from `app.js`, while the backend endpoint `/api/members/delete-all` already existed in the `MembersController`.

## Solution Applied

### Added `deleteAllMembers()` function to `wwwroot/app.js`

The function includes:
- ? **Permission check**: Only Admin users with ReadWrite permission can execute
- ? **Double confirmation**: Two separate confirms to prevent accidental deletion
- ? **API integration**: Calls the existing backend endpoint `DELETE /api/members/delete-all`
- ? **User feedback**: Shows success/error messages
- ? **Auto-refresh**: Reloads the member list after deletion

### Backend Support
The backend endpoint already exists at `Controllers/MembersController.cs` (line 299-324):
```csharp
[HttpDelete("delete-all")]
[RequirePermission(Permission.ReadWrite)]
public async Task<IActionResult> DeleteAllMembers()
```

This endpoint:
- Deletes all members from the database
- Resets identity seeds for Members and Addresses tables
- Returns the count of deleted members
- Requires ReadWrite permission (Admin only)

## Testing
? Build successful
? Function now properly defined
? Button click will no longer throw ReferenceError

## Next Steps
1. Refresh your browser (Ctrl+F5) to clear any cached JavaScript
2. Test the "Delete All (Test)" button
3. Verify the double-confirmation prompts appear
4. Confirm that only Admin users can access this function
