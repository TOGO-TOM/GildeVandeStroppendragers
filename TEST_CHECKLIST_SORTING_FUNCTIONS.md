# Testing Checklist - Member Number Sorting & Missing Functions

## Pre-Test Setup
- [ ] Start the application (F5 in Visual Studio or `dotnet run`)
- [ ] Navigate to `https://localhost:7223/members.html`
- [ ] Ensure you have at least 5 members with various member numbers

## 1. Member Number Sorting Tests

### Test Case 1.1: Ascending Sort (Low to High)
**Steps:**
1. If no members exist, create members with these member numbers: 100, 1, 50, 10, 5
2. Click on the "Sort by..." dropdown
3. Select "Member Number (1-999)"

**Expected Result:**
- Members should be displayed in order: 1, 5, 10, 50, 100
- The numerical order should be correct (not string-based sorting like 1, 10, 100, 5, 50)

**Status:** [ ] Pass [ ] Fail

---

### Test Case 1.2: Descending Sort (High to Low)
**Steps:**
1. With the same members from Test 1.1
2. Click on the "Sort by..." dropdown
3. Select "Member Number (999-1)"

**Expected Result:**
- Members should be displayed in order: 100, 50, 10, 5, 1
- The order should be correctly reversed from ascending

**Status:** [ ] Pass [ ] Fail

---

### Test Case 1.3: Edge Case - Large Member Numbers
**Steps:**
1. Create members with numbers: 1, 99, 100, 999, 1000
2. Sort by "Member Number (1-999)"

**Expected Result:**
- Members displayed in order: 1, 99, 100, 999, 1000
- Large numbers should sort correctly

**Status:** [ ] Pass [ ] Fail

---

## 2. Restore Backup Overwrite Function Test

### Test Case 2.1: Restore Without Overwrite
**Steps:**
1. Create 3 test members
2. Click "Backup" button
3. Create backup (no password needed for test)
4. Add 2 more members (total 5 members now)
5. Click "Restore" button
6. Select the backup file
7. **DO NOT** check "Overwrite all existing data"
8. Click "Restore"

**Expected Result:**
- Should get message about skipped members
- Total members should still be 5 (no duplicates added)
- Original 3 members remain unchanged

**Status:** [ ] Pass [ ] Fail

---

### Test Case 2.2: Restore With Overwrite (FIXED)
**Steps:**
1. Ensure you have the backup from Test 2.1 (with 3 members)
2. Verify you currently have 5 members
3. Click "Restore" button
4. Select the backup file  
5. **CHECK** the "Overwrite all existing data" checkbox
6. Click "Restore"

**Expected Result:**
- Warning confirmation about deleting all data
- After restore, should have exactly 3 members (from backup)
- All current members should be replaced with backup data

**Status:** [ ] Pass [ ] Fail

---

## 3. All Sorting Functions Test

### Test Case 3.1: ID Sorting
**Steps:**
1. Create at least 5 members
2. Test "ID (Low to High)" - should sort by database ID ascending
3. Test "ID (High to Low)" - should sort by database ID descending

**Expected Result:**
- Sorting works correctly by internal ID

**Status:** [ ] Pass [ ] Fail

---

### Test Case 3.2: Last Name Sorting
**Steps:**
1. Create members with last names: Smith, Anderson, Brown, Davis, Carter
2. Test "Last Name (A-Z)" 
3. Test "Last Name (Z-A)"

**Expected Result:**
- A-Z: Anderson, Brown, Carter, Davis, Smith
- Z-A: Smith, Davis, Carter, Brown, Anderson

**Status:** [ ] Pass [ ] Fail

---

## 4. General Function Verification

### Test Case 4.1: All Modal Functions
**Steps:**
1. Test "Import CSV" button - modal should open
2. Test close (X) button - modal should close
3. Test "Backup" button - modal should open
4. Test "Restore" button - modal should open
5. Test "Bulk Update" button - modal should open

**Expected Result:**
- All modals open and close without errors
- No console errors

**Status:** [ ] Pass [ ] Fail

---

### Test Case 4.2: Search/Filter Function
**Steps:**
1. Have at least 5 members with different names
2. Type a member's first name in search box
3. Type a member's last name
4. Type a member number
5. Clear search box

**Expected Result:**
- Members are filtered as you type
- Only matching members show
- All members return when search is cleared

**Status:** [ ] Pass [ ] Fail

---

### Test Case 4.3: Member CRUD Operations
**Steps:**
1. Create a new member
2. Click on member to view contact card
3. Edit member from contact card
4. Save changes
5. Delete member

**Expected Result:**
- All operations complete without errors
- UI updates after each operation

**Status:** [ ] Pass [ ] Fail

---

## 5. CSV Import/Export Functions

### Test Case 5.1: CSV Import
**Steps:**
1. Click "Import CSV"
2. Select a CSV file (use sample_members_comma.csv or sample_members_semicolon.csv)
3. Click "Next: Map Fields"
4. Verify field mapping
5. Click "Import Members"
6. View results

**Expected Result:**
- CSV headers detected correctly
- Fields auto-mapped when possible
- Import completes with success message
- New members appear in list

**Status:** [ ] Pass [ ] Fail

---

### Test Case 5.2: Quick CSV Export
**Steps:**
1. Have at least 3 members
2. Click "Quick CSV" button
3. Open downloaded CSV file

**Expected Result:**
- CSV file downloads
- Contains all members with all fields
- Special characters display correctly (UTF-8 BOM present)

**Status:** [ ] Pass [ ] Fail

---

## 6. Console Error Check

### Test Case 6.1: Browser Console
**Steps:**
1. Open browser Developer Tools (F12)
2. Go to Console tab
3. Perform various operations (sort, filter, CRUD)
4. Check for JavaScript errors

**Expected Result:**
- No JavaScript errors in console
- Only info/log messages should appear

**Status:** [ ] Pass [ ] Fail

---

## 7. Bulk Update Function

### Test Case 7.1: Bulk Update Multiple Members
**Steps:**
1. Click "Bulk Update" button
2. Select 3 members from the list
3. Change Gender to "Vrouw"
4. Change Role to "Steunend lid"
5. Click "Update Selected Members"

**Expected Result:**
- Selected members are updated
- Success message appears
- Member list refreshes with changes

**Status:** [ ] Pass [ ] Fail

---

## Critical Issues Found During Testing

| Issue | Severity | Status | Notes |
|-------|----------|--------|-------|
|       |          |        |       |

---

## Test Summary

**Total Tests:** 13
**Passed:** ___
**Failed:** ___
**Not Tested:** ___

**Tester Name:** _______________
**Test Date:** _______________
**Application Version:** .NET 8
**Browser:** _______________

---

## Notes

Additional observations or issues found during testing:

_______________________________________________
_______________________________________________
_______________________________________________
