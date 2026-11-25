# DANAW Curation Studio

A Windows desktop application for curating and validating cultural-heritage content (Meranaw, Iranun and Maguindanaon) before it is published to the DANAW mobile app. Researchers submit content items with source citations; a BCPCH validator verifies the sources and records a decision.

**Authors:** Balindong, S. H. - Balofinos, F. S. - Cabasag, E. G.
**Course:** ITSD81 Final Project
**Repository:** https://github.com/ellainne21/DanawCurationStudio (private)

## What it does

| Workflow | Actor | Summary |
|---|---|---|
| 1. Register and submit a content item | Researcher | Enter title, category, domain, description and source citations; save a draft or submit. Submission needs at least one source and a title that is unique within its category. Only the creator can edit or resubmit an item. |
| 2. Verify sources and record a decision | Validator | Mark each source verified or not with cross-check notes, then Approve, Request Revision or Reject. Approval requires every source verified; revision and rejection require a comment. |

The two workflows share the `ContentItem` and its sources. An item sent back with *Needs Revision* returns to the researcher, who edits and resubmits it; its sources must then be verified again.

**Advanced capability: Option A, transactional processing.** A decision saves the source verification results, the validation record and the new item status in one database transaction. If any part fails, none of it is saved and the user is told.

## Prerequisites

**To run the published application:** Windows 10 or 11 (x64). Nothing else; the .NET runtime is included in the published folder.

**To build and test from source:**
- .NET SDK 10.0 (developed with 10.0.401)
- Visual Studio 2026 or Visual Studio Code with the C# extension
- The EF Core command-line tool, only if you want to create migrations: `dotnet tool install --global dotnet-ef --version "10.*"`

NuGet packages (restored automatically): Avalonia 12.2.0, CommunityToolkit.Mvvm 8.4.2, Microsoft.EntityFrameworkCore.Sqlite 10.x, xUnit.

## Solution layout

| Project | Contents |
|---|---|
| `Danaw.Core` | Entities, service interfaces and business rules (no database or UI code) |
| `Danaw.Data` | EF Core `DbContext`, repositories, migrations and seed data |
| `Danaw.App` | Avalonia desktop UI (MVVM): sign-in, queue, data-entry and validation windows |
| `Danaw.Tests` | xUnit tests |

## Configuration

There are no configuration files, environment variables or API keys. The database is a single SQLite file at:

```
%LOCALAPPDATA%\DanawCurationStudio\danaw.db
```

(for example `C:\Users\<you>\AppData\Local\DanawCurationStudio\danaw.db`). The folder is created on first launch. To start with a clean database, close the app and delete that folder.

## Database initialization and migrations

The database is created and upgraded **automatically** when the sign-in window opens (`Database.Migrate()`); existing data is never wiped. A fresh database contains four categories and four demonstration accounts. If the database cannot be opened, the sign-in window shows the reason and disables sign-in.

To create a new migration after changing the model:

```powershell
dotnet ef migrations add <Name> --project Danaw.Data --startup-project Danaw.App
```

## Running the tests

```powershell
dotnet test
```

The tests run against an in-memory SQLite database with the real migrations and seed data applied, so database constraints and transactions are genuinely exercised. They cover: submission rules, duplicate titles, creator-only editing, the revision-and-resubmit loop, approval rules, role rules, stale or second decisions, a **forced mid-transaction failure that must roll back everything**, and authentication (including password hashing). Each test re-reads the data with a fresh `DbContext`.

## Launching

**From source:**

```powershell
dotnet run --project Danaw.App
```

**Published build (self-contained, Windows x64):**

```powershell
dotnet publish Danaw.App -c Release -r win-x64 --self-contained true -o publish\DanawCurationStudio
```

Then run `publish\DanawCurationStudio\Danaw.App.exe`. The published folder is about 200 MB because it includes the .NET runtime. The "Add sample items" button exists only in Debug builds.

## Demonstration accounts

These accounts exist **for demonstration and testing only**. The passwords are deliberately simple and must not be used for real data.

| Username | Password | Role |
|---|---|---|
| `balindong` | `balindong123` | Researcher |
| `balofinos` | `balofinos123` | Researcher |
| `cabasag` | `cabasag123` | Researcher |
| `validator` | `validator123` | Validator |

Passwords are stored only as salted PBKDF2-SHA256 hashes (600,000 iterations), never as plain text.

## External dependencies

None at run time. The application does not use the network, a server or a cloud database. SQLite is embedded in the application.

## Known limitations

- **Publishing to the mobile repository is not implemented.** The midterm's open question (API call, file export or scheduled sync) remains unresolved and is out of the approved scope. Validated items stay in the local database.
- **Concurrent edits** are guarded only by a status check (a decided item cannot be decided again). Two validators saving at the very same instant are not fully protected.
- **No account lockout** after repeated wrong passwords.
- **Accounts are fixed.** There is no registration, password change or user-administration screen.
- **The SQLite file is not encrypted;** anyone with access to the file can read the content (passwords remain hashed).
- **Demonstration passwords are weak** by design (see above).
- **Community feedback, vocabulary entries and media assets** from the midterm design are deferred.
- **Local data only:** each computer has its own database; there is no sharing between curators.
- **Cosmetic:** the layout was designed for a 1100 x 640 window or larger.

## Manual release check

The testing guide requires a manual check of the published build outside Visual Studio (launch, both workflows, a rejection, restart persistence, and an unavailable database). Its results are recorded in the separate verification record.
