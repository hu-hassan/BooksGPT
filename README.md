# :books: BooksGPT

An AI-powered book discovery and chat application built with ASP.NET Core and Google Gemini API.

## Features

- :robot: **AI-Powered Chat** - Chat with Gemini AI about books, get recommendations, and discuss literature
- :mag: **Book Search** - Search and discover books using Google Books API
- :bust_in_silhouette: **User Authentication** - Register, login, and manage your profile with email verification
- :speech_balloon: **Chat History** - Your conversations are saved for future reference
- :art: **Modern UI** - Clean, responsive interface

## Tech Stack

- **Framework:** ASP.NET Core 8.0 (MVC)
- **Database:** SQL Server with Entity Framework Core 9
- **AI Integration:** Google Gemini API
- **Book Data:** Google Books API
- **Logging:** log4net
- **Cloud-Ready:** .NET Aspire (AppHost & ServiceDefaults)

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server (LocalDB or full instance)
- Google Books API Key
- Google Gemini API Key

### Setup

1. **Clone the repository**
   ```bash
   git clone https://github.com/hu-hassan/BooksGPT.git
   cd BooksGPT
   ```

2. **Configure API Keys** (see [README-USER-SECRETS.md](README-USER-SECRETS.md) for details)
   ```bash
   cd BooksGPT
   dotnet user-secrets set "ApiKeys:GoogleBooks" "YOUR_GOOGLE_BOOKS_KEY"
   dotnet user-secrets set "ApiKeys:Gemini" "YOUR_GEMINI_KEY"
   ```

3. **Apply database migrations**
   ```bash
   dotnet ef database update
   ```

4. **Run the application**
   ```bash
   dotnet run --project BooksGPT
   ```

## Project Structure

```
BooksGPT/
|-- BooksGPT/                    # Main web application
|   |-- Controllers/             # MVC Controllers
|   |-- Models/                  # Database models & DbContext
|   |-- Views/                   # Razor views
|   |-- Handlers/                # Business logic handlers
|   +-- Migrations/              # EF Core migrations
|-- BooksGPT.AppHost/            # .NET Aspire host
|-- BooksGPT.ServiceDefaults/    # Shared service configurations
+-- README.md
```

## License

This project is for educational purposes.

## Author

**Hassan** - [GitHub](https://github.com/hu-hassan)


