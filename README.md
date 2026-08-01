# Greg's Auto

Scheduling and service history for an independent auto repair shop. Customers
request appointments from a public page; staff confirm them, run the day off a
schedule board, and keep a service history against each vehicle.

ASP.NET Core 8 MVC · EF Core · SQL Server · Bootstrap 5. Server-rendered
throughout — no SPA.

## Start here

| | |
|---|---|
| **[docs/DEVELOPER-GUIDE.md](docs/DEVELOPER-GUIDE.md)** | Setup, how the code is laid out, how each process works, conventions. **Read this first.** |
| [docs/ARCHITECTURE-NOTES.md](docs/ARCHITECTURE-NOTES.md) | What's deliberately unfinished, and the traps. Short. |

## ⚠️ Before you clone and expect it to run

`Gregs Auto.DAL/Scripts/` is gitignored. **The SQL that builds the database is
not in this repository.** A fresh clone compiles and passes the unit tests, and
cannot create a database or run the smoke tests. Get those scripts from whoever
gave you the repo.

## Tests

```
dotnet test "Gregs Auto.Tests"        # 102 — business rules, no database
dotnet test "Gregs Auto.SmokeTests"   # 14 — real app, real database
```

Run the smoke tests after any schema change. The unit suite cannot see a broken
migration; that has bitten before.
