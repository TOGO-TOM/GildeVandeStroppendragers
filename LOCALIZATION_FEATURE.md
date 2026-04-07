# Localization Feature

## ? Wat is geïmplementeerd

Deze feature voegt meertalige ondersteuning toe aan de applicatie voor **Nederlands** en **Engels**.

### Onderdelen:

1. **Automatische taaldetectie**
   - De applicatie detecteert automatisch de browsertaal van de gebruiker
   - Ondersteunde talen: Nederlands (nl) en Engels (en)
   - Standaard taal: Nederlands (nl)

2. **Resource bestanden**
   - `Resources/SharedResources.nl.resx` - Nederlandse vertalingen
   - `Resources/SharedResources.en.resx` - Engelse vertalingen
   - `Resources/SharedResources.cs` - Marker class voor resources

3. **Gelocaliseerde pagina's**
   - **Login.cshtml** - Volledige vertaling van login pagina
   - **_Layout.cshtml** - Navigatie en interface elementen
   - Titel, labels, knoppen, placeholders zijn allen vertaald

### Vertaalde elementen:

#### Navigatie:
- Home
- Leden / Members
- Voorraad / Stock
- Instellingen / Settings
- Audit Logs
- Uitloggen / Logout

#### Login pagina:
- Gebruikersnaam / Username
- Wachtwoord / Password
- Aanmelden / Sign In
- Wachtwoord vergeten? / Forgot password?
- Account aanvragen / Request account

#### Algemeen:
- Opslaan / Save
- Annuleren / Cancel
- Bewerken / Edit
- Verwijderen / Delete
- Zoeken / Search
- En meer...

---

## ?? Technische details

### Configuratie (Program.cs):

```csharp
// Localization toegevoegd met:
- AddLocalization() met ResourcesPath
- AddViewLocalization() voor Razor Pages
- AddDataAnnotationsLocalization() voor model validatie
- RequestLocalizationOptions met nl en en cultures
- AcceptLanguageHeaderRequestCultureProvider (browser detectie)
```

### Gebruik in Razor Pages:

```razor
@using Microsoft.Extensions.Localization
@inject IStringLocalizer<AdminMembers.Resources.SharedResources> Localizer

<h1>@Localizer["AppTitle"]</h1>
<button>@Localizer["Save"]</button>
```

---

## ?? Testen

### Browser taal wijzigen:

**Chrome/Edge:**
1. Instellingen ? Talen
2. Voeg Nederlands of Engels toe
3. Zet de gewenste taal bovenaan
4. Herlaad de applicatie

**Firefox:**
1. Instellingen ? Algemeen ? Taal
2. Kies gewenste taal
3. Herlaad de applicatie

**Handmatig testen:**
- Wijzig browser taal naar Nederlands ? Applicatie toont Nederlandse teksten
- Wijzig browser taal naar Engels ? Applicatie toont Engelse teksten

---

## ?? Nieuwe vertalingen toevoegen

Om nieuwe vertalingen toe te voegen:

1. Open `Resources/SharedResources.nl.resx`
2. Voeg een nieuwe `<data>` entry toe:
```xml
<data name="NewKey" xml:space="preserve">
  <value>Nederlandse vertaling</value>
</data>
```

3. Open `Resources/SharedResources.en.resx`
4. Voeg dezelfde key toe met Engelse vertaling:
```xml
<data name="NewKey" xml:space="preserve">
  <value>English translation</value>
</data>
```

5. Gebruik in Razor:
```razor
@Localizer["NewKey"]
```

---

## ?? Volgende stappen

Om de localization verder uit te breiden:

1. **Meer pagina's vertalen:**
   - Register.cshtml
   - ForgotPassword.cshtml
   - ResetPassword.cshtml
   - Home.cshtml
   - Members/Index.cshtml
   - Settings.cshtml
   - Etc.

2. **Error berichten vertalen:**
   - Validatie fouten
   - API error responses
   - Success berichten

3. **Taal switcher toevoegen:**
   - Dropdown in header om manueel taal te kiezen
   - Cookie opslaan voor taal voorkeur

4. **Meer talen toevoegen:**
   - SharedResources.fr.resx (Frans)
   - SharedResources.de.resx (Duits)
   - Update supportedCultures in Program.cs

---

## ?? NuGet Packages

Geen extra packages nodig! Gebruikt alleen:
- `Microsoft.Extensions.Localization` (al aanwezig in ASP.NET Core 8)
- `Microsoft.AspNetCore.Mvc.Localization` (al aanwezig)

---

## ? Build Status

- ? Build succesvol
- ? Geen compile errors
- ? Resource files correct ingeladen
- ? Feature branch gepusht naar GitHub

---

## ?? Pull Request

Maak een Pull Request aan op GitHub:
https://github.com/TOGO-TOM/GildeVandeStroppendragers/pull/new/feature/localization

Merge deze feature branch naar `main` om localization te activeren in productie.
