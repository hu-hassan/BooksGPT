Local setup: dotnet user-secrets (recommended)

1. Open a PowerShell terminal in the repository root.
2. Run the helper script (makes it simple):
   pwsh .\chatbot\scripts\setup-user-secrets.ps1

   This will:
   - initialize user-secrets for the `chatbot/chatbot.csproj` project
   - prompt for your Google Books and Gemini API keys (they are stored locally)

Manual commands (alternative):
1. Run in project folder (where `chatbot.csproj` lives):
   dotnet user-secrets init
2. Set keys:
   dotnet user-secrets set "ApiKeys:GoogleBooks" "YOUR_GOOGLE_KEY"
   dotnet user-secrets set "ApiKeys:Gemini" "YOUR_GEMINI_KEY"

Notes:
- Keys stored with user-secrets are kept in your user profile and are not checked into source control.
- For CI or production, set these values as environment variables or use a secret manager.
- appsettings.json includes empty placeholders; do NOT put real keys in source.
