# Gilde Van De Stroppendragers — Member Administration

ASP.NET Core 8 Razor Pages application for managing guild members.

---

## Quick Start

```bash
dotnet run
```

Navigate to **https://localhost:7223** — you are redirected to the login page.

> To create the first admin account run `dotnet ef database update` then sign in and register via `/Register`. Approve the account in **Settings ? Users**.

---

## Pages

| URL | Description |
|---|---|
| `/Login` | Sign in |
| `/Register` | Request a new account (pending admin approval) |
| `/Home` | Dashboard |
| `/Members` | Member list — add, edit, delete, search, filter, export, backup |
| `/Settings` | Approve registrations, manage users, custom fields, general settings |
| `/Account` | Change password, set up 2FA |
| `/Account/TotpSetup` | Authenticator app setup (QR code) |

---

## Member Roles

| Role | Description |
|---|---|
| Kandidaat Stappend Lid | Candidate stepping member |
| Kandidaat | Candidate member |
| Steunend Lid | Supporting member |

---

## System User Roles & Permissions

| Role | Read | Write | Admin |
|---|---|---|---|
| Viewer | ? | ? | ? |
| Editor | ? | ? | ? |
| Admin | ? | ? | ? |

---

## Two-Factor Authentication

- Users can enable TOTP 2FA from **My Account ? Set Up Authenticator**
- Compatible with Google Authenticator, Microsoft Authenticator, and any TOTP app
- Devices can be trusted for **30 days** (skips code prompt)

---

## API Endpoints

All endpoints require `Authorization: Bearer <token>`.

### Members
| Method | URL | Description |
|---|---|---|
| GET | `/api/members` | List members |
| GET | `/api/members/{id}` | Get member |
| POST | `/api/members` | Create member |
| PUT | `/api/members/{id}` | Update member |
| DELETE | `/api/members/{id}` | Delete member |
| GET | `/api/members/export/csv` | Quick CSV export |
| POST | `/api/members/export/pdf` | PDF export (field selection) |
| POST | `/api/members/export/excel` | Excel export (field selection) |
| POST | `/api/members/export/csv-custom` | CSV export (field selection) |
| POST | `/api/members/import/csv` | Import from CSV |

### Settings
| Method | URL | Description |
|---|---|---|
| GET/POST | `/api/settings` | General settings |
| POST | `/api/settings/logo` | Upload logo |
| GET | `/api/settings/custom-fields` | List custom fields |
| POST | `/api/settings/custom-fields` | Create custom field |

### Backup
| Method | URL | Description |
|---|---|---|
| POST | `/api/backup/create` | Download encrypted backup |
| POST | `/api/backup/restore` | Restore from backup file |

---

## Tech Stack

| | |
|---|---|
| Framework | ASP.NET Core 8 Razor Pages |
| Database | SQL Server LocalDB via EF Core |
| Auth | Custom session-based middleware |
| 2FA | TOTP via Otp.NET + QRCoder |
| PDF | iTextSharp |
| Excel | ClosedXML |
