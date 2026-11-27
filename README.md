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
- The database has **26 foreign keys** (KAN-145, `Database/KAN-145-add-foreign-keys.sql`):
  - `RESTRICT` for every relation: a row that is still referenced cannot be deleted. The application catches MySQL error 1451 and shows the "still in use" message of the screen.
  - The trees (category, storage location, work type) use `RESTRICT` on `ParentId` too: a parent with children cannot be deleted.
  - `CASCADE` only for the link rows `productsupplier` and `suppliercontact`: they are deleted together with their product or supplier.
- The table scripts contain these foreign keys, so they reference each other. To build an empty database from them, run them with `SET FOREIGN_KEY_CHECKS = 0;` first and `SET FOREIGN_KEY_CHECKS = 1;` after.
- For a one-off manual cleanup, use `Database/manual_cleanup_without_fk_checks_template.sql`. It turns the checks off for that session only. `Database/inspect_foreign_keys.sql` lists the foreign keys of a database.
- Structure changes go through numbered `KAN-*` scripts, which run with `Tools/Update-Database.ps1`.

## Branches and workflow

- `master` is the main branch. There is no `main` branch.
- Each Jira story gets its own branch: `feature/KAN-<number>-<short-description>`.
- Changes reach `master` only through a pull request, after review and test. The branch is deleted after the merge.

## Never commit

- Database dumps with data (`Database/dump-*.sql`).
- License keys and credentials (`docs/SyncfusionLicense.txt`, `Tools/root.txt`). Database passwords come from environment variables.
- Temporary files (`tmp/`) and IDE settings (`.idea/`).
