# CareFlow - Diagnostic Booking Management (ITSD81 Performance Task 4)

Desktop app (C# / .NET 10 / Avalonia / MVVM / EF Core / SQLite) for booking 1-3 diagnostic services per patient per date. A booking is all-or-nothing.

**Team:** `<Member 1>` and `<Member 2>`

## Requirements
- .NET 10 SDK (Visual Studio 2026 or VS Code)
- EF Core CLI: `dotnet tool install --global dotnet-ef`

## Setup (from the solution folder)
```bash
dotnet restore
dotnet ef migrations add InitialCreate --project CareFlow     # only if CareFlow/Migrations does not exist yet
dotnet ef database update --project CareFlow                  # creates DB + seed data
dotnet run --project CareFlow
```
The database file is `careflow.db` in your user local-app-data folder, inside a `CareFlow` folder. The app also applies pending migrations on startup. To reset, close the app and delete that file.

Seed data: 5 fictional patients and 4 services (CBC cap 5, Urinalysis 5, Chest X-Ray 3, ECG 2). No credentials are used.

## Run tests
```bash
dotnet test
```

## Project layout
| Folder | Responsibility |
|---|---|
| `Domain` | Entities |
| `Business` | Business rules (`BookingRules`), service contracts, DTOs |
| `Data` | EF Core `DbContext`, migrations, `BookingService` (transaction + capacity check) |
| `ViewModels` / `Views` | MVVM + Avalonia XAML |

Docs: `docs/RelationalDesign.md`, `docs/VerificationChecklist.md`

## Acknowledgments
AI assistance (Claude) was used to generate the initial project structure and code. We reviewed, ran, and understand the submitted work. `<Add anything else you used or changed.>`
