# ?? AUTOMATISCHE TEST INSTRUCTIES

## ? SNELSTART

### Stap 1: Start de Applicatie
```bash
# In Visual Studio:
Druk F5

# Of via Terminal:
cd C:\Temp\AdminMembers\AdminMembers
dotnet run
```

### Stap 2: Open de Test Tool
```
https://localhost:7223/autotest.html
```

### Stap 3: Run Tests
Klik op de **"?? Run Alle Tests"** knop

?? **Duurt:** 30-60 seconden voor volledige test

---

## ?? Wat Wordt Getest?

De automatische test suite test **12 componenten**:

### ?? Backend API Tests (7 tests)
1. ? **API Connectie** - Is backend bereikbaar?
2. ? **GET Members** - Kan leden ophalen?
3. ? **POST Member** - Kan lid aanmaken?
4. ? **PUT Member** - Kan lid updaten?
5. ? **DELETE Member** - Kan lid verwijderen?
6. ? **Backup Endpoint** - Werkt backup?
7. ? **Import CSV Endpoint** - Werkt import?

### ?? Sorteer Functionaliteit (2 tests)
8. ? **Sorteer Lidnummer** - Numerieke sortering correct?
9. ? **Sorteer Achternaam** - Alfabetische sortering correct?

### ?? UI Functionaliteit (3 tests)
10. ? **Modal Functies** - Bestaan alle modal functies?
11. ? **Formulier Validatie** - Formulier checks
12. ? **Check Member Number** - Duplicate check werkt?

---

## ?? Verwachte Resultaten

### ? ALLES GROEN = PERFECT!
```
?? Test Resultaten
Totaal: 12
Geslaagd: 12 ?
Gefaald: 0
Tijd: ~45s
```

**Dit betekent:**
- ? Backend werkt perfect
- ? Backup werkt
- ? Import CSV werkt
- ? Sorteren werkt correct (geen ID, alleen lidnummer)
- ? Alle UI functies werken

### ?? SOMMIGE ROOD = SPECIFIEK PROBLEEM

**Als Test 1 (API Connectie) ROOD is:**
```
? API Connectie - FAIL
```
**Oplossing:** Backend is niet gestart
1. Ga naar Visual Studio
2. Druk F5
3. Wacht tot browser opent
4. Refresh autotest.html
5. Klik "Run Alle Tests" opnieuw

**Als Test 6 (Backup) ROOD is:**
```
? Backup Functionaliteit - FAIL
```
**Oplossing:** Database probleem
```bash
# In Package Manager Console:
Update-Database

# Of Terminal:
dotnet ef database update
```

**Als Test 7 (Import CSV) ROOD is:**
```
? Import CSV Functionaliteit - FAIL
```
**Oplossing:** Import endpoint probleem
- Check Visual Studio Output voor errors
- Mogelijk missing Services registration

---

## ?? Screenshots Van Resultaten

### ALLES WERKT (Ideaal):
```
?? Automatische Test Suite
Progress: ???????????????????? 100%

? API Connectie Test - PASS
   ? API bereikbaar
   Status: 200
   Data type: Array

? Backup Functionaliteit - PASS
   ? Backup Endpoint
   Status: 200 OK
   File size: 45.23 KB

? Import CSV Functionaliteit - PASS
   ? Import CSV Endpoint
   Imported: 1
   Skipped: 0

? Sorteer Lidnummer - PASS
   ? Sorteer Lidnummer
   Ascending: 1, 5, 10, 50, 100
   Descending: 100, 50, 10, 5, 1
   Beide correct!

?? Test Resultaten
Totaal: 12
Geslaagd: 12 ?
Gefaald: 0
```

### BACKEND NIET GESTART:
```
? API Connectie Test - FAIL
   ? ERROR: Failed to fetch

? GET Members Endpoint - FAIL
   ? ERROR: Failed to fetch

(Alle andere tests falen ook)

?? Test Resultaten
Totaal: 12
Geslaagd: 0
Gefaald: 12 ?
```

**Fix:** Start backend (F5 in Visual Studio)

---

## ?? Test Opties

### 1. ?? Run Alle Tests
- Test ALLE 12 componenten
- Duurt: 45-60 seconden
- Meest grondig

### 2. ? Quick Test
- Test alleen eerste 5 tests
- Duurt: 10-15 seconden
- Snelle check

### 3. ?? Reset
- Reset alle resultaten
- Klaar voor nieuwe test

### 4. ?? Export Rapport
- Download JSON rapport
- Bevat alle test details
- Handig voor debugging

---

## ?? Test Details Per Component

### Test 1: API Connectie
**Wat doet het:**
- Probeert `/api/members` te bereiken
- Check response status
- Verifieert data type

**Als het faalt:**
```
? HTTP 404 ? Backend draait niet op juiste port
? Failed to fetch ? Backend is niet gestart
? CORS error ? CORS niet enabled
```

### Test 6: Backup Functionaliteit
**Wat doet het:**
- Roept `POST /api/members/backup` aan
- Check of file gedownload kan worden
- Verifieert file size

**Als het faalt:**
```
? HTTP 500 ? Database error
? HTTP 404 ? Endpoint bestaat niet
? Service error ? BackupService niet geregistreerd
```

### Test 7: Import CSV Functionaliteit
**Wat doet het:**
- Creëert test CSV bestand
- Upload naar `POST /api/members/import/csv`
- Verifieert import resultaat
- Cleanup test data

**Als het faalt:**
```
? HTTP 400 ? Invalid CSV format
? HTTP 500 ? Import logic error
? Validation error ? Required fields missing
```

### Test 8: Sorteer Lidnummer
**Wat doet het:**
- Test data: [100, 1, 50, 10, 5]
- Test ascending sort
- Verwacht: [1, 5, 10, 50, 100]
- Test descending sort
- Verwacht: [100, 50, 10, 5, 1]

**Als het faalt:**
```
? Ascending sort fout: 1, 10, 100, 5, 50
   ? String-based sorting in plaats van numeric

Fix: Use parseInt() in sort function
```

---

## ?? Troubleshooting

### Alle Tests Falen
**Symptoom:**
```
? API Connectie Test - FAIL: Failed to fetch
? GET Members Endpoint - FAIL: Failed to fetch
...
Geslaagd: 0
Gefaald: 12
```

**Diagnose:**
- Backend is NIET gestart

**Fix:**
1. Open Visual Studio
2. Druk F5 (Start Debugging)
3. Wacht tot browser automatisch opent
4. Ga terug naar autotest.html
5. Click "Run Alle Tests"

---

### Alleen Backend Tests Falen (1-7)
**Symptoom:**
```
? API tests: FAIL
? Sorting tests: PASS
? UI tests: PASS
```

**Diagnose:**
- Backend heeft probleem
- Database niet bereikbaar
- Services niet geregistreerd

**Fix:**
```bash
# Check database:
Update-Database

# Check Visual Studio Output voor errors
```

---

### Alleen Backup/Import Falen (6-7)
**Symptoom:**
```
? API Connectie: PASS
? GET/POST/PUT/DELETE: PASS
? Backup: FAIL
? Import: FAIL
```

**Diagnose:**
- BackupService niet geregistreerd
- ExportService niet geregistreerd
- Database permissions

**Fix:**
Check `Program.cs`:
```csharp
builder.Services.AddScoped<BackupService>();
builder.Services.AddScoped<ExportService>();
```

---

### Sorteer Test Faalt (8)
**Symptoom:**
```
? Sorteer Lidnummer - FAIL
   Ascending sort fout: 1, 10, 100, 5, 50
```

**Diagnose:**
- String-based sorting i.p.v. numeric

**Fix:**
Check `app.js` sortMembers() functie:
```javascript
case 'memberNumber-asc':
    sortedMembers.sort((a, b) => {
        const aNum = parseInt(a.memberNumber, 10);
        const bNum = parseInt(b.memberNumber, 10);
        return aNum - bNum;
    });
```

---

## ?? Log Sectie

De test tool toont een live log:

```
[14:32:15] ?? Start Volledige Test Suite
[14:32:16] ? Running: API Connectie Test
[14:32:16] ? API Connectie Test - PASS
[14:32:17] ? Running: GET Members Endpoint
[14:32:17] ? GET Members Endpoint - PASS
[14:32:18] ? Running: POST Member Endpoint
[14:32:18] ? POST Member Endpoint - PASS
...
[14:32:45] ?? ALLE TESTS GESLAAGD! (12/12)
```

**Kleuren:**
- ?? Blauw = Info
- ?? Groen = Success
- ?? Rood = Error
- ?? Geel = Warning

---

## ?? Tips

### Tip 1: Test Na Elke Code Wijziging
```
1. Wijzig code
2. Save (Ctrl+S)
3. Rebuild (Ctrl+Shift+B)
4. Refresh autotest.html (Ctrl+F5)
5. Run tests
```

### Tip 2: Gebruik Quick Test Voor Snelle Check
- Quick Test = 10 seconden
- Test belangrijkste functionaliteit
- Perfect voor tijdens development

### Tip 3: Export Rapport Voor Debugging
- Download JSON rapport
- Bevat alle details
- Stuur naar collega's of voor later

### Tip 4: Check Log Voor Details
- Scroll door log sectie
- Zie exacte volgorde van tests
- Zie timing van elke test

---

## ? Success Criteria

### Minimaal Voor Productie:
```
? API Connectie: PASS
? GET Members: PASS
? POST Member: PASS
? Backup: PASS
? Import CSV: PASS
? Sorteer Lidnummer: PASS
```

### Ideaal (Alles):
```
? Alle 12 tests: PASS
Geslaagd: 12/12 (100%)
```

---

## ?? Volgende Stappen

### Als ALLES GROEN is:
1. ? Applicatie is productie-ready
2. ? Backup werkt perfect
3. ? Import CSV werkt perfect
4. ? Sorteren werkt correct (numeriek, geen ID)
5. ?? **KLAAR VOOR GEBRUIK!**

### Als IETS ROOD is:
1. Check welke test faalt
2. Lees error message
3. Volg fix instructies in deze guide
4. Run tests opnieuw

---

## ?? Hulp Nodig?

Als tests blijven falen:

1. **Screenshot autotest.html** (volledige pagina met resultaten)
2. **Screenshot Browser Console** (F12 ? Console tab)
3. **Screenshot Visual Studio Output** (Debug sectie)
4. **Export test rapport** (JSON file)

Met deze info kan direct gezien worden wat het probleem is!

---

**Laatste Update:** ${new Date().toLocaleDateString('nl-NL')}
**Test Suite Versie:** 1.0
**Aantal Tests:** 12
**Geschatte Duur:** 45-60 seconden
