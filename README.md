![CodeRabbit Pull Request Reviews](https://img.shields.io/coderabbit/prs/github/hnsoftwaredevelopment/ModelbouwWerkbank?utm_source=oss&utm_medium=github&utm_campaign=hnsoftwaredevelopment%2FModelbouwWerkbank&labelColor=171717&color=FF570A&link=https%3A%2F%2Fcoderabbit.ai&label=CodeRabbit+Reviews)

# ModelbouwWerkbank

Tracks hours worked on model building projects, split into tasks, and keeps the inventory of materials and tools.

## Repository

| Folder | Contents |
|:---|:---|
| `Modelbouwer` | WPF desktop application (.NET, MVVM, Syncfusion) |
| `Modelbouwer.Mobile` | .NET MAUI Android app |
| `Modelbouwer.UnitTests` | MSTest unit tests for the desktop application |
| `Database` | Structure scripts per table, view and routine (no data), and the numbered `KAN-*` update scripts |
| `Tools` | `Update-Database.ps1` runs the update scripts with a backup first |
| `docs` | Design notes and reference material |

## Database

- The table scripts in `Database/` describe the structure only, generated from the local database with collation `utf8mb4_general_ci`. That collation works on both MySQL 8 and MariaDB 10.3 (NAS).
- The database has **no foreign keys**. They were removed deliberately with `Database/drop_all_foreign_keys.sql`. The application keeps related data consistent itself: for example, an order with receipts cannot be deleted, and usage and receipts are written to the stocklog in the same transaction. Whether foreign keys come back is tracked in KAN-145.
- Structure changes go through numbered `KAN-*` scripts, which run with `Tools/Update-Database.ps1`.

## Branches and workflow

- `master` is the main branch. There is no `main` branch.
- Each Jira story gets its own branch: `feature/KAN-<number>-<short-description>`.
- Changes reach `master` only through a pull request, after review and test. The branch is deleted after the merge.

## Never commit

- Database dumps with data (`Database/dump-*.sql`).
- License keys and credentials (`docs/SyncfusionLicense.txt`, `Tools/root.txt`). Database passwords come from environment variables.
- Temporary files (`tmp/`) and IDE settings (`.idea/`).
