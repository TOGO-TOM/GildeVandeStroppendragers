# ?? SNELLE TESTGIDS - Member Administration

## ? Checklist: Testen van Fixes

### 1?? Sorteren op Lidnummer Testen

**Voorbereiding:**
1. Start de applicatie (F5 in Visual Studio)
2. Open `https://localhost:7223/members.html`
3. Maak 5 testleden aan met deze lidnummers:
   - Lidnummer: 100
   - Lidnummer: 1
   - Lidnummer: 50
   - Lidnummer: 10
   - Lidnummer: 5

**Test A: Sorteer Laag ? Hoog**
- [ ] Klik op "Sorteer op..." dropdown
- [ ] Selecteer "Lidnummer (Laag ? Hoog)"
- [ ] **Verwacht:** Volgorde is 1, 5, 10, 50, 100
- [ ] **? PASS** / **? FAIL**

**Test B: Sorteer Hoog ? Laag**
- [ ] Selecteer "Lidnummer (Hoog ? Laag)"
- [ ] **Verwacht:** Volgorde is 100, 50, 10, 5, 1
- [ ] **? PASS** / **? FAIL**

---

### 2?? Backup Functie Testen

**Test: Backup Maken**
- [ ] Klik op "Backup" knop (rechts bovenaan)
- [ ] Modal opent
- [ ] (Optioneel) Voer wachtwoord in: `test123`
- [ ] Klik "Create Backup"
- [ ] **Verwacht:** 
  - Bestand downloadt: `members_backup_2025-01-XX.bak`
  - Groene succesmelding: "Backup created and downloaded successfully!"
  - Modal sluit automatisch
- [ ] **? PASS** / **? FAIL**

**Test: Backup Restore**
- [ ] Klik op "Restore" knop
- [ ] Selecteer het zojuist gedownloade backup bestand
- [ ] Voer wachtwoord in (als gebruikt): `test123`
- [ ] Klik "Restore"
- [ ] **Verwacht:** 
  - Succesmelding met aantal geïmporteerde leden
  - Leden lijst ververst
- [ ] **? PASS** / **? FAIL**

---

### 3?? Import CSV Testen

**Voorbereiding:**
Gebruik één van de sample bestanden in de wwwroot folder:
- `sample_members_comma.csv`
- `sample_members_semicolon.csv`
- `sample_members_special_chars.csv`

**Test: CSV Import**
- [ ] Klik op "Import CSV" knop
- [ ] Modal opent met Step 1
- [ ] Klik "Choose CSV File"
- [ ] Selecteer `sample_members_comma.csv`
- [ ] Bestandsnaam verschijnt onder knop
- [ ] Klik "Next: Map Fields"
- [ ] **Verwacht:** 
  - Step 2 toont met veld mapping
  - Velden zijn automatisch gemapped (groen gemarkeerd)
- [ ] Klik "Import Members"
- [ ] **Verwacht:**
  - Step 3 toont met resultaten
  - "? Import Completed!" bericht
  - Aantal geïmporteerde leden getoond
- [ ] Klik "Done"
- [ ] **Verwacht:** Nieuwe leden verschijnen in lijst
- [ ] **? PASS** / **? FAIL**

---

### 4?? Browser Console Check

**Test: Geen JavaScript Errors**
- [ ] Open Browser Developer Tools (F12)
- [ ] Ga naar "Console" tab
- [ ] Ververs de pagina (Ctrl+R)
- [ ] Voer alle bovenstaande tests uit
- [ ] **Verwacht:** 
  - Alleen INFO berichten (blauw/wit)
  - GEEN rode error berichten
  - Eventueel waarschuwingen (geel) zijn OK
- [ ] **? PASS** / **? FAIL**

---

## ?? Probleemoplossing

### Als Sorteren NIET werkt:
```
1. Open Browser Console (F12)
2. Type: currentMembers
3. Druk Enter
4. Check of leden zichtbaar zijn
5. Type: sortMembers()
6. Check voor errors
```

### Als Backup/Import NIET werkt:
```
1. Open Browser Console (F12)
2. Check of je deze error ziet:
   "API_URL is not defined"

3. Als JA:
   - app.js is niet correct geladen
   - Ververs pagina met Ctrl+F5 (hard refresh)

4. Als NEE maar wel andere error:
   - Kopieer de error
   - Check of API draait (backend)
```

### Als API niet draait:
```
1. In Visual Studio: Druk F5
2. Of in terminal: dotnet run
3. Check URL: https://localhost:7223
4. Test API direct: https://localhost:7223/api/members
   (Moet JSON teruggeven met leden)
```

---

## ?? Screenshots Verwacht Gedrag

### ? Correct: Lidnummer Sorteren
```
Sorteer op: Lidnummer (Laag ? Hoog)

#1   - Jan Jansen
#5   - Piet Pietersen  
#10  - Klaas Klaassen
#50  - Marie Marissen
#100 - Hans Hansen
```

### ? Incorrect: (Zou NIET moeten gebeuren)
```
Sorteer op: Lidnummer (Laag ? Hoog)

#1   - Jan Jansen
#10  - Klaas Klaassen    ? FOUT! 10 komt voor 5
#100 - Hans Hansen        ? FOUT! 100 komt voor 5
#5   - Piet Pietersen
#50  - Marie Marissen
```

---

## ?? Snel Checklist Alle Features

| Feature | Werkt? | Notities |
|---------|--------|----------|
| Lid toevoegen | ? | |
| Lid bewerken | ? | |
| Lid verwijderen | ? | |
| Sorteer op Achternaam (A-Z) | ? | |
| Sorteer op Achternaam (Z-A) | ? | |
| Sorteer op Lidnummer (Laag?Hoog) | ? | |
| Sorteer op Lidnummer (Hoog?Laag) | ? | |
| Zoeken in lijst | ? | |
| Backup maken | ? | |
| Backup restore | ? | |
| Import CSV | ? | |
| Export CSV (Quick) | ? | |
| Export Excel | ? | Via export.html |
| Export PDF | ? | Via export.html |
| Bulk Update | ? | |
| Custom Fields | ? | Via settings.html |

---

## ?? Kritieke Checks

### MOET werken (Core Functionaliteit):
- ? Lid toevoegen
- ? Lid bewerken  
- ? Lid verwijderen
- ? Sorteer op Lidnummer
- ? Import CSV
- ? Backup maken

### MAG NIET gebeuren (Breaking Issues):
- ? Rode errors in console
- ? Witte/lege pagina
- ? "API_URL is not defined" error
- ? Sorteer werkt niet
- ? Backup/Import knop doet niets

---

## ?? Test Resultaat

**Datum Test:** _______________  
**Geteste Door:** _______________  
**Browser:** _______________  
**Alle Tests Geslaagd:** ? JA / ? NEE  

**Problemen Gevonden:**
```
(Beschrijf hier eventuele problemen)




```

---

## ? Na Succesvolle Test

Als alle tests slagen:
1. ? Applicatie is klaar voor gebruik
2. ? Je kunt beginnen met echte data invoeren
3. ? Maak een backup voordat je veel data invoert
4. ? Test import met je eigen CSV bestanden

---

**Veel succes met testen! ??**
