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

## Branches and workflow

- `master` is the main branch. There is no `main` branch.
- Each Jira story gets its own branch: `feature/KAN-<number>-<short-description>`.
- Changes reach `master` only through a pull request, after review and test. The branch is deleted after the merge.

## Never commit

- Database dumps with data (`Database/dump-*.sql`).
- License keys and credentials (`docs/SyncfusionLicense.txt`, `Tools/root.txt`). Database passwords come from environment variables.
- Temporary files (`tmp/`) and IDE settings (`.idea/`).
