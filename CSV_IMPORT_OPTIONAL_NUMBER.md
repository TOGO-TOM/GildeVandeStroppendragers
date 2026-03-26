# ? CSV IMPORT - Member Number Now Optional

## Changes Made

Made member number **completely optional** in CSV import functionality.

---

## ?? What Changed

### 1. Client-Side (wwwroot/app.js)

**Field Mapping (Line 788):**
```javascript
// BEFORE
{ name: 'MemberNumber', label: 'Member Number *', required: true }

// AFTER
{ name: 'MemberNumber', label: 'Member Number', required: false }
```

**Validation (Line 881):**
```javascript
// BEFORE
if (!mapping.MemberNumber || !mapping.FirstName || !mapping.LastName) {
    showMessage('Please map required fields: Member Number, First Name, and Last Name', 'error');
    return;
}

// AFTER
if (!mapping.FirstName || !mapping.LastName) {
    showMessage('Please map required fields: First Name and Last Name', 'error');
    return;
}
```

### 2. Server-Side (Controllers/MembersController.cs)

**Parsing Member Number (Line 661):**
```csharp
// BEFORE
MemberNumber = int.TryParse(GetMappedValue(row, mapping, "MemberNumber"), out var memberNum) ? memberNum : 0,

// AFTER
MemberNumber = int.TryParse(GetMappedValue(row, mapping, "MemberNumber"), out var memberNum) && memberNum > 0 
    ? (int?)memberNum 
    : null,
```

**Validation (Lines 707-720):**
```csharp
// BEFORE
if (member.MemberNumber == 0 || 
    string.IsNullOrWhiteSpace(member.FirstName) || 
    string.IsNullOrWhiteSpace(member.LastName))
{
    errors.Add($"Row {i + 1}: Missing required fields (Member Number, First Name, or Last Name)");
    continue;
}

// Check if member number already exists
if (await _context.Members.AnyAsync(m => m.MemberNumber == member.MemberNumber))
{
    skippedCount++;
    continue;
}

// AFTER
if (string.IsNullOrWhiteSpace(member.FirstName) || 
    string.IsNullOrWhiteSpace(member.LastName))
{
    errors.Add($"Row {i + 1}: Missing required fields (First Name or Last Name)");
    continue;
}

// Check if member number already exists (only if provided)
if (member.MemberNumber.HasValue && 
    await _context.Members.AnyAsync(m => m.MemberNumber == member.MemberNumber))
{
    errors.Add($"Row {i + 1}: Member number {member.MemberNumber} already exists");
    skippedCount++;
    continue;
}
```

---

## ?? CSV Import Behavior

### Scenario 1: CSV Without Member Number Column

**CSV File:**
```csv
FirstName,LastName,Gender,Role,Street,City,PostalCode
John,Doe,Man,Kandidaat,Main St,Brussels,1000
Jane,Smith,Vrouw,Stappend lid,Oak St,Antwerp,2000
```

**Import Process:**
```
1. Map fields:
   - Member Number: [Not mapped] ?
   - First Name ? FirstName
   - Last Name ? LastName
   - (other fields)

2. Import:
   - Row 1: John Doe, MemberNumber = NULL ?
   - Row 2: Jane Smith, MemberNumber = NULL ?

3. Result:
   ? 2 members imported
   ? Both have NULL member numbers
   ? Display as "No #"
```

### Scenario 2: CSV With Member Number Column

**CSV File:**
```csv
MemberNumber,FirstName,LastName,Street,City,PostalCode
150,John,Doe,Main St,Brussels,1000
,Jane,Smith,Oak St,Antwerp,2000
200,Bob,Jones,Elm St,Ghent,9000
```

**Import Process:**
```
1. Map fields:
   - Member Number ? MemberNumber
   - First Name ? FirstName
   - (other fields)

2. Import:
   - Row 1: John Doe, MemberNumber = 150 ?
   - Row 2: Jane Smith, MemberNumber = NULL ? (empty in CSV)
   - Row 3: Bob Jones, MemberNumber = 200 ?

3. Result:
   ? 3 members imported
   ? 2 with numbers (150, 200)
   ? 1 without number (NULL)
```

### Scenario 3: CSV With Duplicate Member Numbers

**CSV File:**
```csv
MemberNumber,FirstName,LastName,Street,City,PostalCode
150,John,Doe,Main St,Brussels,1000
150,Jane,Smith,Oak St,Antwerp,2000
,Bob,Jones,Elm St,Ghent,9000
```

**Import Process:**
```
1. Row 1: John Doe #150
   - Check: 150 doesn't exist ?
   - Import: Success ?

2. Row 2: Jane Smith #150
   - Check: 150 already exists ?
   - Skip: Duplicate number
   - Error: "Row 2: Member number 150 already exists"

3. Row 3: Bob Jones (no number)
   - Check: NULL is allowed ?
   - Import: Success ?

Result:
? 2 members imported (John, Bob)
?? 1 member skipped (Jane - duplicate #150)
?? Error log shown
```

---

## ?? Required vs Optional Fields

### Required Fields (Must Be in CSV)
- ? **First Name** - Cannot be empty
- ? **Last Name** - Cannot be empty

### Optional Fields (Can Be Empty/Missing)
- ? Member Number - Can be NULL
- ? Gender - Defaults to "Man"
- ? Role - Defaults to "Kandidaat"
- ? Email
- ? Phone Number
- ? Birth Date
- ? Seniority Date
- ? Status - Defaults to Alive
- ? Address fields (Street, City, etc.)

---

## ?? CSV File Examples

### Example 1: Minimal CSV (No Numbers)

**File: members_minimal.csv**
```csv
FirstName,LastName
John,Doe
Jane,Smith
Bob,Jones
```

**Result:**
```
? 3 members imported
? All with NULL member numbers
? Display as "No #"
```

### Example 2: Mixed Numbers

**File: members_mixed.csv**
```csv
MemberNumber,FirstName,LastName,Email,Street,City,PostalCode
150,John,Doe,john@example.com,Main St,Brussels,1000
,Jane,Smith,jane@example.com,Oak St,Antwerp,2000
200,Bob,Jones,bob@example.com,Elm St,Ghent,9000
```

**Result:**
```
? 3 members imported
? John Doe: #150
? Jane Smith: No #
? Bob Jones: #200
```

### Example 3: Full Data

**File: members_full.csv**
```csv
MemberNumber,FirstName,LastName,Gender,Role,Email,PhoneNumber,BirthDate,SeniorityDate,IsAlive,Street,HouseNumber,City,PostalCode,Country
150,John,Doe,Man,Kandidaat,john@example.com,+32123456789,1990-01-15,2020-06-01,true,Main Street,42,Brussels,1000,Belgium
,Jane,Smith,Vrouw,Stappend lid,jane@example.com,+32987654321,1985-05-20,2019-03-15,true,Oak Avenue,15,Antwerp,2000,Belgium
```

**Result:**
```
? 2 members imported
? John: #150 with full data
? Jane: No # with full data
```

---

## ?? Testing CSV Import

### Test 1: Import Without Member Numbers

**Create CSV file:**
```csv
FirstName,LastName,Street,City,PostalCode
Test1,User1,Test St,Test City,12345
Test2,User2,Main St,Main City,54321
```

**Steps:**
1. Go to Members page
2. Click "Import CSV" or "Backup" button
3. Upload CSV file
4. Map fields:
   - First Name ? FirstName
   - Last Name ? LastName
   - Street ? Street
   - City ? City
   - Postal Code ? PostalCode
   - Member Number ? [Not mapped] ?
5. Click Import

**Expected:**
```
? Import Completed!
? 2 members imported
? Both with NULL member numbers
? Show in list as "No #"
```

### Test 2: Import With Mixed Numbers

**Create CSV file:**
```csv
MemberNumber,FirstName,LastName,Street,City,PostalCode
500,NumberedUser,One,St 1,City 1,11111
,NoNumber,Two,St 2,City 2,22222
600,NumberedUser,Three,St 3,City 3,33333
```

**Expected:**
```
? 3 members imported
? Member 1: #500
? Member 2: No #
? Member 3: #600
```

### Test 3: Import With Duplicates

**Create CSV file:**
```csv
MemberNumber,FirstName,LastName,Street,City,PostalCode
500,Duplicate,One,St 1,City 1,11111
500,Duplicate,Two,St 2,City 2,22222
,NoDuplicate,Three,St 3,City 3,33333
```

**Expected:**
```
? 2 members imported
?? 1 member skipped
?? Error: Row 2: Member number 500 already exists

Imported:
- Duplicate One (#500)
- NoDuplicate Three (No #)

Skipped:
- Duplicate Two (#500 - duplicate)
```

---

## ?? Field Mapping Screen

When importing CSV, you'll see:

```
??????????????????????????????????????????
?  Map CSV Columns to Member Fields      ?
??????????????????????????????????????????
?  First Name *                          ?
?  ?? Select CSV column ?                ?
?                                        ?
?  Last Name *                           ?
?  ?? Select CSV column ?                ?
?                                        ?
?  Member Number                         ?  ? No asterisk (optional)
?  ?? Select CSV column ?                ?
?     [-- Do not import --] ? Can skip   ?
?                                        ?
?  Gender, Role, Email, etc.             ?
?  ?? Optional fields                    ?
??????????????????????????????????????????

* = Required
Other fields are optional
```

---

## ?? Import Rules

### Member Number Handling

**If CSV has Member Number column:**
- Map it ? Numbers imported as provided
- Leave empty cells ? NULL
- Skip mapping ? All NULL

**If CSV has NO Member Number column:**
- Don't map Member Number field
- All imported members ? NULL
- No errors

### Duplicate Handling

**With Numbers:**
```
CSV: #150
DB: #150 exists
Result: Skip with error message
```

**Without Numbers:**
```
CSV: NULL
DB: 5 members already with NULL
Result: Import successfully (NULL allowed multiple times) ?
```

---

## ?? Summary of Changes

**Files Modified:**
1. ? `wwwroot/app.js` (Lines 788, 881)
   - Removed Member Number from required fields
   - Updated validation message

2. ? `Controllers/MembersController.cs` (Lines 661, 707-720)
   - Parse member number as nullable int
   - Removed from required validation
   - Only check duplicates if number provided

**Build Status:** ? Successful

---

## ?? CSV Import Is Now Flexible

**You can import:**
- ? CSV with member numbers
- ? CSV without member numbers
- ? CSV with mixed (some with, some without)
- ? CSV with empty member number cells
- ? Members with NULL numbers (multiple allowed)
- ? Members with unique numbers

**Import will skip:**
- ? Rows missing First Name or Last Name
- ? Rows with duplicate member numbers
- ? Rows with invalid data

---

## ?? Ready to Test

**Restart your app and test CSV import:**

1. Create a simple CSV:
   ```csv
   FirstName,LastName,Street,City,PostalCode
   Test,User,Test St,Test City,12345
   ```

2. Import it (don't map Member Number)

3. Expected: ? Imported with NULL member number

**CSV import now supports optional member numbers!** ??
