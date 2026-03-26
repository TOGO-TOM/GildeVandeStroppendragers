# ?? TEST CHECKLIST - AdminMembers

## Voor het Testen
- [ ] Zorg dat de applicatie draait: `dotnet run`
- [ ] Open browser: `https://localhost:7223/members.html`

---

## 1?? Custom Fields Test

### Settings Pagina
- [ ] Ga naar Settings (`https://localhost:7223/settings.html`)
- [ ] Klik "Add Custom Field"
- [ ] Maak Text field aan: "Loge Naam"
- [ ] Maak Number field aan: "Lidnummer Loge"
- [ ] Maak Date field aan: "Inwijdingsdatum"
- [ ] Maak Checkbox field aan: "Actief in Loge"
- [ ] Alle fields zijn Active ?

### Members Pagina - Nieuw Lid
- [ ] Ga naar Members
- [ ] Klik "Add New Member" (of scroll naar formulier)
- [ ] Vul basis gegevens in:
  - Member Number: `1`
  - First Name: `Jan`
  - Last Name: `Janssen`
- [ ] **Scroll naar beneden** ? Zie "Custom Fields" sectie ?
- [ ] Vul custom fields in:
  - Loge Naam: `De Vriendschap`
  - Lidnummer Loge: `123`
  - Inwijdingsdatum: `2020-01-15`
  - Actief in Loge: ? (check)
- [ ] Klik "Save Member" ? Success message ?
- [ ] Klik op member card ? Open details
- [ ] **Scroll in card** ? Zie "Additional Information" sectie ?
- [ ] Alle custom field waarden zijn zichtbaar ?

### Members Pagina - Edit Lid
- [ ] Klik "Edit" op het lid
- [ ] Formulier opent met alle data
- [ ] **Scroll naar Custom Fields** sectie
- [ ] Alle custom field waarden zijn geladen ?
- [ ] Wijzig "Loge Naam" naar `Het Kompas`
- [ ] Wijzig "Lidnummer Loge" naar `456`
- [ ] Klik "Save Member"
- [ ] Open member card opnieuw
- [ ] Gewijzigde waarden zijn zichtbaar ?

---

## 2?? Export CSV Test

### Quick CSV Export
- [ ] Ga naar Members pagina
- [ ] Voeg nog 2 members toe met numbers: `2`, `10`
- [ ] Klik "Quick CSV" button in header
- [ ] CSV bestand wordt gedownload ?
- [ ] Open CSV in Excel/Notepad
- [ ] **Check Member Numbers:**
  - Moet zijn: `1`, `2`, `10` (numeriek) ?
  - NIET: `"1"`, `"10"`, `"2"` (string)
- [ ] Alle andere data is correct ?

---

## 3?? Sorting Test

### Member Number Sortering
- [ ] Ga naar Members pagina
- [ ] Voeg extra members toe met numbers: `20`, `100`, `5`
- [ ] Open Sort dropdown
- [ ] **Check labels:**
  - "Member Number (1-999)" ? (niet A-Z)
  - "Member Number (999-1)" ? (niet Z-A)
- [ ] Selecteer "Member Number (1-999)"
- [ ] **Volgorde moet zijn:** 1, 2, 5, 10, 20, 100 ?
- [ ] Selecteer "Member Number (999-1)"
- [ ] **Volgorde moet zijn:** 100, 20, 10, 5, 2, 1 ?

---

## 4?? Import CSV Test

### Voorbereiding
- [ ] Download/gebruik `sample_members_comma.csv`
- [ ] Of maak eigen CSV:
```csv
MemberNumber,FirstName,LastName,Gender,Role,Email
200,Piet,Pietersen,Man,Stappend lid,piet@test.com
201,Marie,Jansen,Vrouw,Kandidaat,marie@test.com
```

### Import Proces
- [ ] Ga naar Members pagina
- [ ] Klik "Import CSV" button
- [ ] **Stap 1:** File selecteren
  - [ ] Klik "Choose CSV File"
  - [ ] Selecteer CSV bestand
  - [ ] Bestandsnaam verschijnt met grootte ?
  - [ ] Klik "Next: Map Fields"

- [ ] **Stap 2:** Field Mapping
  - [ ] Velden zijn automatisch gemapped ?
  - [ ] MemberNumber ? MemberNumber ?
  - [ ] FirstName ? FirstName ?
  - [ ] LastName ? LastName ?
  - [ ] Check andere mappings
  - [ ] Klik "Import Members"

- [ ] **Stap 3:** Resultaten
  - [ ] Zie success message met groene vink ?
  - [ ] Aantal geïmporteerd wordt getoond ?
  - [ ] Eventueel: Aantal overgeslagen (duplicaten)
  - [ ] Klik "Done"
  - [ ] Members lijst is ververst ?
  - [ ] Nieuwe members zijn zichtbaar ?

### Import Edge Cases
- [ ] Import dezelfde CSV nogmaals
- [ ] Alle records worden overgeslagen (duplicaten) ?
- [ ] Message toont: "X members skipped" ?

---

## 5?? Member Number Validatie

### Positief Integer
- [ ] Probeer member number `0` ? Error ?
- [ ] Probeer member number `-5` ? Error ?
- [ ] Probeer member number `abc` ? Kan niet invoeren (type=number) ?
- [ ] Gebruik member number `1` ? Werkt ?
- [ ] Gebruik member number `999` ? Werkt ?

### Duplicate Check
- [ ] Voeg member toe met number `1`
- [ ] Probeer nog een member met number `1`
- [ ] Error message: "Member number is already in use" ?
- [ ] Kan niet opslaan ?

---

## 6?? Photo Functies

### Upload Photo
- [ ] Bij Add/Edit member
- [ ] Klik "Choose Photo"
- [ ] Selecteer foto (< 5MB)
- [ ] Preview verschijnt ?
- [ ] Save member
- [ ] Open member card ? Foto zichtbaar ?

### Remove Photo
- [ ] Edit member met foto
- [ ] Foto preview zichtbaar
- [ ] Klik "Remove Photo" button
- [ ] Preview verdwijnt ?
- [ ] Save member
- [ ] Open member card ? Foto is weg, initialen getoond ?

---

## 7?? Reset Form Test

### Reset Functionaliteit
- [ ] Begin Add New Member
- [ ] Vul alle velden in (inclusief custom fields)
- [ ] Klik "Reset" button
- [ ] Alle velden zijn leeg ?
- [ ] Custom fields zijn leeg ?
- [ ] Photo preview is weg ?
- [ ] Form title is "Add New Member" ?

---

## 8?? Bulk Operaties (Bestaande Functionaliteit)

### Bulk Update
- [ ] Selecteer 3 members
- [ ] Klik "Bulk Update"
- [ ] Wijzig Gender of Role
- [ ] Apply
- [ ] Alle 3 members zijn geüpdatet ?

### Delete All (Test Mode)
- [ ] Klik "Delete All (Test)" button
- [ ] Confirm
- [ ] Alle members verwijderd ?
- [ ] Members list is leeg ?

---

## 9?? Contact Card (Member Details)

### Volledige Member Card
- [ ] Voeg complete member toe (alle velden ingevuld)
- [ ] Klik op member in lijst
- [ ] Card opent met alle secties:
  - [ ] Header: Photo/Initialen, Naam, Number, Status ?
  - [ ] Seniority (indien ingevuld) ?
  - [ ] Personal Information (Gender, Role, Age) ?
  - [ ] Contact Information (Email, Phone) ?
  - [ ] Address (volledig adres) ?
  - [ ] **Additional Information (Custom Fields)** ?
- [ ] Custom fields tonen correcte waarden:
  - [ ] Text fields: Tekst ?
  - [ ] Number fields: Nummer ?
  - [ ] Date fields: Geformatteerde datum ?
  - [ ] Checkbox fields: "Yes" of "No" ?

---

## ?? Browser Console Check

### Geen Errors
- [ ] Open Browser DevTools (F12)
- [ ] Ga naar Console tab
- [ ] **Geen rode errors zichtbaar** ?
- [ ] Eventuele warnings zijn normaal

### Network Tab
- [ ] Ga naar Network tab
- [ ] Refresh pagina
- [ ] Alle API calls zijn 200 OK ?
- [ ] `/api/members` ? 200 ?
- [ ] `/api/settings/custom-fields` ? 200 ?

---

## ? Final Checklist

### Functionaliteit
- [ ] ? Custom fields in add form
- [ ] ? Custom fields in edit form
- [ ] ? Custom fields in member card
- [ ] ? CSV export met integer member numbers
- [ ] ? Numerieke sortering (1-999)
- [ ] ? CSV import volledig werkend
- [ ] ? Member number validatie
- [ ] ? Photo upload/remove
- [ ] ? Reset form

### UI/UX
- [ ] ? Intuïtieve labels
- [ ] ? Duidelijke error messages
- [ ] ? Success notifications
- [ ] ? Responsive design

### Data Integriteit
- [ ] ? Custom fields opslaan correct
- [ ] ? Custom fields laden correct bij edit
- [ ] ? Geen data verlies bij update
- [ ] ? Duplicate member numbers worden gedetecteerd

---

## ?? Bug Report Template

Als er toch een probleem is:

```
### Bug Beschrijving
[Wat ging er mis?]

### Stappen om te Reproduceren
1. Ga naar...
2. Klik op...
3. Vul in...
4. Zie fout...

### Verwacht Gedrag
[Wat zou er moeten gebeuren?]

### Actueel Gedrag
[Wat gebeurt er in plaats daarvan?]

### Browser Console Errors
[Plak console errors hier]

### Screenshot
[Voeg screenshot toe indien mogelijk]
```

---

## ? Alles Werkt?

**Gefeliciteerd! ??**

De applicatie is volledig functioneel met:
- ? Custom fields
- ? CSV import/export
- ? Numerieke sorting
- ? Volledige validatie

**Volgende stappen:**
1. Gebruik de applicatie voor echte data
2. Maak backups regelmatig
3. Test deployment naar productie
4. Train gebruikers

---

**Happy testing! ???**
