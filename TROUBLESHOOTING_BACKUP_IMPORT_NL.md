# ?? TROUBLESHOOTING GUIDE - Backup & Import Problemen

## ?? Gerapporteerde Problemen

1. ? **Backup werkt niet**
2. ? **Import CSV werkt niet**
3. ? **Sorteren aangepast** (ID verwijderd, alleen lidnummer behouden)

---

## ?? STAP 1: Diagnose Uitvoeren

### Open de Diagnose Pagina
1. Start de applicatie (F5 in Visual Studio)
2. Open in browser: `https://localhost:7223/diagnose.html`
3. Klik op alle test knoppen
4. Screenshot de resultaten

### Verwachte Resultaten:
- ? **API Connectie**: Moet groen zijn (SUCCESS)
- ? **Backup Endpoint**: Moet groen zijn
- ? **Import Endpoint**: Moet groen zijn
- ? **Sorteer Functie**: Moet groen zijn
- ? **Console**: Moet groen zijn (geen errors)

---

## ?? VEELVOORKOMENDE PROBLEMEN & OPLOSSINGEN

### PROBLEEM 1: Backend is niet gestart
**Symptomen:**
- Rode error: "Failed to fetch"
- API Connectie test faalt
- Witte/lege pagina

**Oplossing:**
```bash
# In Visual Studio:
1. Druk F5 (Start Debugging)
2. Wacht tot browser automatisch opent

# Of via Terminal:
cd C:\Temp\AdminMembers\AdminMembers
dotnet run
```

**Check:**
- Open: https://localhost:7223/api/members
- Moet JSON tonen met leden (kan leeg array zijn: [])

---

### PROBLEEM 2: Database Migratie Ontbreekt
**Symptomen:**
- Backup werkt niet
- Error: "Cannot open database"
- Import faalt

**Oplossing:**
```bash
# In Package Manager Console (Visual Studio):
Update-Database

# Of via Terminal:
dotnet ef database update
```

**Verificatie:**
- Check of `AdminMembers.db` bestand bestaat in project folder
- Grootte moet > 0 KB zijn

---

### PROBLEEM 3: CORS Error (Cross-Origin)
**Symptomen:**
- Console error: "Access to fetch... has been blocked by CORS"
- API calls falen in browser

**Oplossing:**
Check `Program.cs` - CORS moet enabled zijn:
```csharp
var builder = WebApplication.CreateBuilder(args);

// Dit moet aanwezig zijn:
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

var app = builder.Build();

// Dit ook:
app.UseCors();
```

---

### PROBLEEM 4: Backup Service Niet Geregistreerd
**Symptomen:**
- Backup knop doet niets
- Error: "Unable to resolve service for type 'BackupService'"

**Oplossing:**
Check `Program.cs` - Services moeten geregistreerd zijn:
```csharp
builder.Services.AddScoped<BackupService>();
builder.Services.AddScoped<ExportService>();
```

---

### PROBLEEM 5: File Upload Size Limit
**Symptomen:**
- Import werkt met kleine CSV
- Faalt bij grote bestanden
- Error: "Request body too large"

**Oplossing:**
In `Program.cs` toevoegen:
```csharp
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 104857600; // 100 MB
});
```

---

## ?? DEBUGGING STAPPEN

### Stap 1: Check Browser Console
1. Open members.html
2. Druk F12 (Developer Tools)
3. Ga naar "Console" tab
4. Klik op "Backup" knop
5. Check voor rode errors

**Veelvoorkomende Console Errors:**

#### Error: "API_URL is not defined"
```javascript
// app.js moet beginnen met:
const API_URL = '/api/members';
```
? **Dit is al correct in je code!**

#### Error: "Failed to fetch"
**Betekenis:** Backend is niet bereikbaar
**Fix:** Start backend (F5 in Visual Studio)

#### Error: "500 Internal Server Error"
**Betekenis:** Backend error
**Fix:** Check Visual Studio Output venster voor error details

---

### Stap 2: Check Backend Logs
1. In Visual Studio: View ? Output
2. Selecteer "Debug" of "Web Server"
3. Reproduceer het probleem
4. Lees de error logs

**Veelvoorkomende Backend Errors:**

#### "Cannot open database"
```bash
# Fix:
dotnet ef database update
```

#### "Sequence contains no elements"
```csharp
// In Controller: Gebruik FirstOrDefaultAsync() i.p.v. FirstAsync()
var member = await _context.Members.FirstOrDefaultAsync(m => m.Id == id);
```

---

### Stap 3: Test API Direct met Browser

#### Test Backup:
```
POST https://localhost:7223/api/members/backup
```
**Met Postman/Insomnia:**
- Method: POST
- URL: https://localhost:7223/api/members/backup
- Verwacht: Binary file download

#### Test Import:
```
POST https://localhost:7223/api/members/import/csv
```
**Met Postman:**
- Method: POST
- Body ? form-data
- Key: `csvFile` (type: File)
- Key: `fieldMapping` (type: Text)
  Value: `{"MemberNumber":"MemberNumber","FirstName":"FirstName","LastName":"LastName"}`

---

## ?? MANUAL TEST PROCEDURE

### Test 1: Backup Functionaliteit

**Prerequisites:**
- Backend is gestart
- Minimaal 1 lid in database

**Stappen:**
1. Open `https://localhost:7223/members.html`
2. Klik "Backup" knop (rechtsboven)
3. Modal opent
4. (Optioneel) Voer wachtwoord in: `test123`
5. Klik "Create Backup"

**Verwacht Gedrag:**
- ? Groene melding: "Creating backup..."
- ? File download start automatisch
- ? Bestandsnaam: `members_backup_2025-01-XX.bak`
- ? Groene melding: "Backup created and downloaded successfully!"
- ? Modal sluit automatisch

**Als het faalt:**
1. Open F12 Console
2. Klik opnieuw "Create Backup"
3. Kopieer de rode error
4. Check "Veelvoorkomende Errors" sectie hieronder

---

### Test 2: Import CSV Functionaliteit

**Prerequisites:**
- Backend is gestart
- Sample CSV bestand beschikbaar

**Test CSV Content (save as `test.csv`):**
```csv
MemberNumber,FirstName,LastName,Gender,Role,Email,Street,City,PostalCode
1000,Jan,Jansen,Man,Kandidaat,jan@test.nl,Teststraat,Amsterdam,1000AA
1001,Piet,Pietersen,Man,Stappend lid,piet@test.nl,Pietstraat,Rotterdam,2000BB
```

**Stappen:**
1. Open `https://localhost:7223/members.html`
2. Klik "Import CSV" knop
3. Modal opent - Step 1
4. Klik "Choose CSV File"
5. Selecteer `test.csv`
6. Bestandsnaam verschijnt
7. Klik "Next: Map Fields"
8. Step 2 toont met veldmapping
9. Check of velden correct gemapped zijn:
   - MemberNumber ? MemberNumber ?
   - FirstName ? FirstName ?
   - LastName ? LastName ?
10. Klik "Import Members"

**Verwacht Gedrag:**
- ? Step 3 toont
- ? "? Import Completed!" bericht
- ? "2 Members imported successfully"
- ? Geen skipped members
- ? Geen errors
- ? "Done" knop aanwezig
11. Klik "Done"
- ? Leden verschijnen in lijst met lidnummers 1000 en 1001

**Als het faalt:**
1. Open F12 Console
2. Herhaal import process
3. Check Console tab voor errors
4. Check Network tab:
   - Zoek naar `import/csv` request
   - Klik erop
   - Check Response tab voor error details

---

## ?? VEELVOORKOMENDE ERRORS & FIXES

### Error 1: "Failed to create backup"
**Mogelijke Oorzaken:**
1. Database is leeg
2. BackupService niet geregistreerd
3. Geen write permissions

**Debug:**
```csharp
// In BackupService.cs - Add logging:
_logger.LogInformation("Creating backup...");
_logger.LogInformation($"Found {members.Count} members");
```

**Fix:**
```bash
# Check of database bestaat:
ls AdminMembers.db

# Check database inhoud:
dotnet ef database update
```

---

### Error 2: "Import failed" of "No fields selected"
**Mogelijke Oorzaken:**
1. CSV format niet herkend
2. Wrong separator (komma vs puntkomma)
3. Missing required fields

**Debug:**
```javascript
// In app.js - Add console logging in importCSVData():
console.log('Field mapping:', mapping);
console.log('CSV Headers:', csvHeaders);
```

**Fix:**
1. Check CSV separator - moet `,` of `;` zijn
2. Ensure velden MemberNumber, FirstName, LastName zijn gemapped
3. Check CSV encoding - moet UTF-8 zijn

---

### Error 3: "Unexpected end of JSON input"
**Betekenis:** API returned geen geldige JSON

**Debug in Browser:**
1. F12 ? Network tab
2. Reproduceer probleem
3. Find API call (backup of import/csv)
4. Click on it
5. Check Response tab

**Fix:**
- Als leeg: Backend crashed - check Visual Studio Output
- Als HTML: Wrong endpoint - check URL
- Als text: Wrong Content-Type - check Controller

---

## ?? CHECKLIST VOOR VOLLEDIGE FIX

### Backend Checks:
- [ ] Backend draait (https://localhost:7223 toont pagina)
- [ ] Database bestaat (AdminMembers.db file)
- [ ] Migraties zijn uitgevoerd (`dotnet ef database update`)
- [ ] Services zijn geregistreerd (BackupService, ExportService)
- [ ] CORS is enabled in Program.cs
- [ ] Controllers hebben juiste endpoints

### Frontend Checks:
- [ ] API_URL is gedefinieerd in app.js (`const API_URL = '/api/members';`)
- [ ] Geen console errors (F12 ? Console tab)
- [ ] Modals openen correct
- [ ] Buttons hebben onclick handlers

### Functionaliteit Checks:
- [ ] Laden van leden werkt
- [ ] Sorteer dropdown werkt (zonder ID opties)
- [ ] Backup knop reageert
- [ ] Import CSV knop reageert
- [ ] Modals sluiten correct

---

## ?? VOLLEDIGE RESET PROCEDURE

Als niets werkt, probeer complete reset:

```bash
# 1. Stop applicatie
# 2. Delete database
rm AdminMembers.db

# 3. Delete migrations (optioneel)
rm -rf Migrations/*

# 4. Rebuild
dotnet clean
dotnet build

# 5. Create new migration
dotnet ef migrations add InitialCreate

# 6. Create database
dotnet ef database update

# 7. Start app
dotnet run
```

---

## ?? HULP NODIG?

Als je nog steeds problemen hebt:

1. **Run diagnose.html**
   - Screenshot alle test resultaten

2. **Check Console errors**
   - F12 ? Console tab
   - Screenshot rode errors

3. **Check Backend logs**
   - Visual Studio ? Output
   - Screenshot error messages

4. **Verzamel info:**
   - Welke knop werkt niet?
   - Wat gebeurt er precies?
   - Zijn er error messages?
   - Console logs?

---

## ? VERIFICATIE NA FIX

Test alle functionaliteit:

1. **Backup:**
   - [ ] Klik Backup ? Modal opent
   - [ ] Create Backup ? File download
   - [ ] Check file size > 0 KB

2. **Import:**
   - [ ] Klik Import CSV ? Modal opent
   - [ ] Select CSV ? File naam toont
   - [ ] Next ? Veld mapping toont
   - [ ] Import ? Success message
   - [ ] Done ? Leden in lijst

3. **Sorting:**
   - [ ] Dropdown toont:
     - Sorteer op...
     - Achternaam (A-Z)
     - Achternaam (Z-A)
     - Lidnummer (Laag ? Hoog)
     - Lidnummer (Hoog ? Laag)
   - [ ] Geen ID opties
   - [ ] Sorteren werkt correct

---

**Laatste Update:** ${new Date().toLocaleDateString('nl-NL')}
**Status:** Diagnose tools beschikbaar, wachten op test resultaten
