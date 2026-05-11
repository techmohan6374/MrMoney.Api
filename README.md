# MrMoney API — .NET 8 + Google Sheets Backend

A clean-architecture .NET 8 Web API that uses **Google Sheets as the database**.  
Each entity maps to a dedicated sheet tab inside a single Google Spreadsheet.

---

## Google Sheets Schema

| Sheet Tab      | Columns |
|----------------|---------|
| **Users**        | Id · Email · Name · Picture · Currency · EmailNotifications · Theme · CreatedAt · LastLoginAt |
| **Accounts**     | Id · UserId · Name · HolderName · Balance · Type · Color · IsDefault · CreatedAt |
| **Transactions** | Id · UserId · AccountId · Name · Category · Amount · Type · Description · Status · Date · CreatedAt |
| **Categories**   | Id · UserId · Name · Icon · Color · Type · CreatedAt |

> The API creates all sheets and header rows automatically on first startup.

---

## Project Structure

```
MrMoney.Api/
├── Controllers/          # HTTP endpoints (Auth, Accounts, Transactions, Categories, Users, Dashboard)
├── DTOs/                 # Request & Response data transfer objects
├── Infrastructure/       # GoogleSheetsClient — low-level Sheets API wrapper
├── Models/               # Domain entities (Account, Transaction, Category, UserProfile)
├── Repositories/         # Data access layer — one repo per sheet
├── Services/             # Business logic layer
├── Program.cs            # DI wiring, middleware pipeline
└── appsettings.json      # Configuration
```

---

## Setup Guide

### 1. Create a Google Cloud Project & Service Account

1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Create a new project (e.g. `mr-money`)
3. Enable the **Google Sheets API** for the project
4. Go to **IAM & Admin → Service Accounts** → Create a service account
5. Give it a name (e.g. `mrmoney-sheets`)
6. Click **Keys → Add Key → Create new key → JSON**
7. Download the JSON file and save it as `google-service-account.json` in the `MrMoney.Api/` folder (next to the `.sln` file)

### 2. Create a Google Spreadsheet

1. Go to [Google Sheets](https://sheets.google.com) and create a new blank spreadsheet
2. Name it `MrMoney DB`
3. Copy the **Spreadsheet ID** from the URL:  
   `https://docs.google.com/spreadsheets/d/`**`THIS_IS_THE_ID`**`/edit`
4. Share the spreadsheet with the service account email (from the JSON file, field `client_email`) with **Editor** access

### 3. Configure appsettings.json

```json
{
  "GoogleSheets": {
    "SpreadsheetId": "PASTE_YOUR_SPREADSHEET_ID_HERE",
    "ServiceAccountKeyPath": "google-service-account.json"
  },
  "GoogleAuth": {
    "ClientId": "YOUR_GOOGLE_OAUTH_CLIENT_ID"
  },
  "Jwt": {
    "Key": "your-long-secret-key-at-least-32-chars",
    "Issuer": "MrMoneyApi",
    "Audience": "MrMoneyUsers"
  }
}
```

### 4. Run the API

```bash
cd backend/MrMoney.Api
dotnet restore
dotnet run --project MrMoney.Api
```

The API starts at `http://localhost:5000`.  
Swagger UI is available at `http://localhost:5000` (root).

On first startup the API will automatically create the 4 sheet tabs with header rows.

---

## API Endpoints

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/api/Auth/google-login` | ❌ | Google OAuth login, returns JWT |
| GET | `/api/Users/me` | ✅ | Get current user profile |
| PUT | `/api/Users/me` | ✅ | Update profile (name, currency, theme) |
| GET | `/api/Accounts` | ✅ | List all accounts |
| POST | `/api/Accounts` | ✅ | Create account |
| PUT | `/api/Accounts/{id}` | ✅ | Update account |
| DELETE | `/api/Accounts/{id}` | ✅ | Delete account |
| GET | `/api/Transactions` | ✅ | List transactions (filterable, paged) |
| POST | `/api/Transactions` | ✅ | Create transaction |
| PUT | `/api/Transactions/{id}` | ✅ | Update transaction |
| DELETE | `/api/Transactions/{id}` | ✅ | Delete transaction |
| POST | `/api/Transactions/transfer` | ✅ | Transfer between accounts |
| GET | `/api/Categories` | ✅ | List categories |
| POST | `/api/Categories` | ✅ | Create category |
| PUT | `/api/Categories/{id}` | ✅ | Update category |
| DELETE | `/api/Categories/{id}` | ✅ | Delete category |
| GET | `/api/Dashboard/summary` | ✅ | Dashboard metrics + charts |
| GET | `/api/Dashboard/report?period=Weekly` | ✅ | Report analytics |

### Transaction Filter Query Params
`GET /api/Transactions?type=expense&accountId=xxx&category=Food&search=coffee&dateFrom=2024-01-01&dateTo=2024-01-31&page=1&pageSize=50`

---

## Security Notes

- `google-service-account.json` is **gitignored** — never commit it
- JWT tokens expire after 7 days
- All financial endpoints require a valid JWT (`Authorization: Bearer <token>`)
- Each user can only access their own data (filtered by `UserId` in every query)
