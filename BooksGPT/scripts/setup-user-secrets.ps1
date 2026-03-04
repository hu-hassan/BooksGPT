<#
PowerShell helper to initialize dotnet user-secrets for the `BooksGPT` project
Run this from the repository root (where this script is located):
    pwsh .\BooksGPT\scripts\setup-user-secrets.ps1

It will: 
 - locate `BooksGPT.csproj` inside the `BooksGPT` folder
 - run `dotnet user-secrets init` for that project
 - set `ApiKeys:GoogleBooks` and `ApiKeys:Gemini` with values you enter (or from env)

This script only configures local user-secrets for your account and does not commit secrets to git.
#>

param(
    [string]$GoogleBooksKey = $null,
    [string]$GeminiKey = $null
)

try {
    $scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
    $repoRoot = Resolve-Path "$scriptDir\.."
    Push-Location $repoRoot

    $projPath = "BooksGPT\BooksGPT.csproj"
    if (-not (Test-Path $projPath)) {
        Write-Error "Project file not found at $projPath. Run this script from repository root where the 'BooksGPT' folder exists."
        exit 1
    }

    # Read keys from environment variables if not provided
    if (-not $GoogleBooksKey) { $GoogleBooksKey = $env:ApiKeys__GoogleBooks; if (-not $GoogleBooksKey) { $GoogleBooksKey = Read-Host -Prompt "Enter Google Books API Key (input hidden)" -AsSecureString | ConvertFrom-SecureString -AsPlainText } }
    if (-not $GeminiKey) { $GeminiKey = $env:ApiKeys__Gemini; if (-not $GeminiKey) { $GeminiKey = Read-Host -Prompt "Enter Gemini API Key (input hidden)" -AsSecureString | ConvertFrom-SecureString -AsPlainText } }

    Write-Host "Initializing user-secrets for project: $projPath"
    dotnet user-secrets init --project $projPath

    if ($LASTEXITCODE -ne 0) { Write-Error "dotnet user-secrets init failed"; exit $LASTEXITCODE }

    Write-Host "Storing GoogleBooks key..."
    dotnet user-secrets set --project $projPath "ApiKeys:GoogleBooks" "$GoogleBooksKey"
    if ($LASTEXITCODE -ne 0) { Write-Error "Failed to set GoogleBooks key"; exit $LASTEXITCODE }

    Write-Host "Storing Gemini key..."
    dotnet user-secrets set --project $projPath "ApiKeys:Gemini" "$GeminiKey"
    if ($LASTEXITCODE -ne 0) { Write-Error "Failed to set Gemini key"; exit $LASTEXITCODE }

    Write-Host "User-secrets configured successfully for project $projPath"
}
finally {
    Pop-Location -ErrorAction SilentlyContinue
}
