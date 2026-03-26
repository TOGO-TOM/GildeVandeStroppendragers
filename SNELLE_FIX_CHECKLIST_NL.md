# ? SNELLE FIX CHECKLIST - Backup & Import

## ?? EERST DIT DOEN:

### 1. Start Diagnose Tool
```
1. Start applicatie (F5 in Visual Studio)
2. Open: https://localhost:7223/diagnose.html
3. Klik alle "Test" knoppen
4. Screenshot de resultaten
```

**?? Duurt: 2 minuten**

---

## ?? QUICK FIXES (Meest Voorkomend)

### Fix #1: Backend Niet Gestart ? MEEST VOORKOMEND
```
? Symptoom: 
- Niets werkt
- Witte pagina
- Error: "Failed to fetch"
- diagnose.html ? API test is ROOD

?? Fix:
1. Open Visual Studio
2. Druk F5 (Start Debugging)
3. Wacht tot browser automatisch opent
4. Refresh diagnose.html
5. Test opnieuw
```

**?? Duurt: 30 seconden**

---

### Fix #2: Database Bestaat Niet
```
? Symptoom:
- 500 Internal Server Error
- "Cannot open database"
- diagnose.html ? Backup test is ROOD

?? Fix Option A (Visual Studio):
1. Open "Package Manager Console" (View ? Other Windows)
2. Type: Update-Database
3. Druk Enter
4. Wacht op "Done"

?? Fix Option B (Terminal):
cd C:\Temp\AdminMembers\AdminMembers
dotnet ef database update
```

**?? Duurt: 1 minuut**

---

### Fix #3: Browser Cache Probleem
```
? Symptoom:
- Backend draait (Visual Studio toont geen errors)
- API test is groen
- Maar backup/import doen nog steeds niets

?? Fix:
1. In browser: Druk Ctrl + Shift + Delete
2. Selecteer "Cached images and files"
3. Klik "Clear data"
4. Of sneller: Druk Ctrl + F5 (hard refresh)
5. Test opnieuw
```

**?? Duurt: 10 seconden**

---

### Fix #4: HTTPS Certificate Probleem
```
? Symptoom:
- "NET::ERR_CERT_AUTHORITY_INVALID"
- Pagina laadt niet
- Browser waarschuwing over onveilige verbinding

?? Fix:
1. In browser: Klik "Advanced"
2. Klik "Proceed to localhost (unsafe)"
3. Of in terminal:
   dotnet dev-certs https --trust
4. Herstart browser
```

**?? Duurt: 30 seconden**

---

## ?? VERIFIEER DE FIX

Na elke fix, test dit:

### ? Test 1: API Bereikbaar
```
Open in browser: https://localhost:7223/api/members
Verwacht: JSON array (kan leeg zijn: [])
```

### ? Test 2: Backup Werkt
```
1. Open: https://localhost:7223/members.html
2. Klik "Backup" knop
3. Modal opent
4. Klik "Create Backup"
5. File moet downloaden
```

### ? Test 3: Import Werkt
```
1. Klik "Import CSV" knop
2. Modal opent
3. Select any CSV file
4. Bestandsnaam verschijnt
5. Klik "Next: Map Fields"
6. Veld mapping toont
```

---

## ?? ALS HET NOG STEEDS NIET WERKT

### Stap 1: Check Visual Studio Output
```
1. In Visual Studio: View ? Output
2. Selecteer "Debug" uit dropdown
3. Scroll naar beneden
4. Zoek naar RODE error messages
5. Screenshot en stuur door
```

### Stap 2: Check Browser Console
```
1. In browser: Druk F12
2. Ga naar "Console" tab
3. Reproduceer het probleem
4. Screenshot RODE errors
5. Stuur door voor hulp
```

### Stap 3: Check Database File
```
1. Open File Explorer
2. Ga naar: C:\Temp\AdminMembers\AdminMembers
3. Zoek bestand: AdminMembers.db
4. Right-click ? Properties
5. Check file size (moet > 0 KB zijn)

Als bestand niet bestaat:
? Run: dotnet ef database update
```

---

## ?? VERIFICATIE CHECKLIST

Na alle fixes, check dit:

| Test | Werkt? | Fix Als Niet |
|------|--------|--------------|
| diagnose.html ? API Test | ? | Fix #1 |
| diagnose.html ? Backup Test | ? | Fix #2 |
| diagnose.html ? Import Test | ? | Fix #2 |
| diagnose.html ? Sort Test | ? | (Moet altijd werken) |
| diagnose.html ? Console Test | ? | Fix #3 |
| members.html ? Backup knop | ? | Fix #1 + #3 |
| members.html ? Import CSV knop | ? | Fix #1 + #3 |
| Sorteer dropdown (geen ID) | ? | (Al correct) |

---

## ?? TIPS

### Tip 1: Gebruik Altijd diagnose.html Eerst
Dit bespaart je veel tijd! Het laat direct zien wat het probleem is.

### Tip 2: Check Visual Studio Output
Veel errors worden alleen hier getoond, niet in browser.

### Tip 3: Hard Refresh Na Code Changes
Druk altijd Ctrl + F5 in browser na code wijzigingen.

### Tip 4: Stop/Start Backend Regelmatig
Bij twijfel: Stop (Shift+F5) en Start (F5) opnieuw.

---

## ?? COMPLETE CONFIGURATIE CHECK

Je backend configuratie (Program.cs) is **? CORRECT**:

```csharp
? BackupService is geregistreerd (regel 24)
? ExportService is geregistreerd (regel 27)  
? CORS is enabled (regel 30-38)
? DbContext is geconfigureerd (regel 20-21)
? JSON serialization is correct (regel 10-14)
```

Je frontend configuratie (app.js) is **? CORRECT**:
```javascript
? API_URL is gedefinieerd: const API_URL = '/api/members';
? Backup functie bestaat
? Import functie bestaat
? Sorteer functie is correct (zonder ID)
```

---

## ?? EMERGENCY FIX (Als Niets Werkt)

```bash
# 1. Stop applicatie (Shift + F5)

# 2. Clean solution
dotnet clean

# 3. Rebuild
dotnet build

# 4. Update database
dotnet ef database update

# 5. Start opnieuw
dotnet run
# Of F5 in Visual Studio

# 6. Test diagnose.html
```

**?? Duurt: 2 minuten**

---

## ? SUCCES INDICATOREN

Je weet dat alles werkt als:

1. ? diagnose.html ? ALLE TESTEN GROEN
2. ? https://localhost:7223/api/members ? Toont JSON
3. ? Backup knop ? File download
4. ? Import CSV knop ? Modal opent ? Veld mapping werkt
5. ? Sorteer dropdown ? Geen ID opties ? Sorteren werkt
6. ? F12 Console ? Geen rode errors

---

## ?? HULP NODIG?

Stuur deze info:

1. **Screenshot diagnose.html** (alle 5 test resultaten)
2. **Screenshot F12 Console** (bij klikken op Backup knop)
3. **Screenshot Visual Studio Output** (Debug dropdown)
4. **Beschrijf** exact wat je deed toen het fout ging

---

**Laatste Check:** ${new Date().toLocaleDateString('nl-NL')} ${new Date().toLocaleTimeString('nl-NL')}

---

## ?? MEEST WAARSCHIJNLIJKE OPLOSSING

**90% van de problemen wordt opgelost door:**

```
1. F5 in Visual Studio (start backend)
2. Ctrl + F5 in browser (hard refresh)
3. Test diagnose.html
```

**Als dat niet werkt:**
```
4. Package Manager Console: Update-Database
5. Test diagnose.html opnieuw
```

**Dat zou het moeten fixen!** ??
