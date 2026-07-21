Local setup: dotnet user-secrets (recommended)

Open a PowerShell terminal in the **project folder** (`BooksGPT/BooksGPT`) and run:

```powershell
dotnet user-secrets init
dotnet user-secrets set "ApiKeys:GoogleBooks" "YOUR_GOOGLE_BOOKS_KEY"
dotnet user-secrets set "ApiKeys:Gemini" "YOUR_GEMINI_KEY"
dotnet user-secrets set "Smtp:Email" "YOUR_SMTP_EMAIL"
dotnet user-secrets set "Smtp:Password" "YOUR_SMTP_PASSWORD"
```

Notes:
- Keys stored with user-secrets are kept in your user profile and are **never** checked into source control.
- For CI or production, set these values as **environment variables** (`APIKEY_GOOGLEBOOKS`, `APIKEY_GEMINI`, `SMTP_EMAIL`, `SMTP_PASSWORD`) or use a secret manager.
- `appsettings.json` includes empty placeholders; do NOT put real keys in source.
