# Contributing to Hotshield

Thanks for your interest! Here's how to get started.

## Code of Conduct

Be respectful. Constructive feedback only. No personal attacks.

## Before You Start

- Check existing issues and PRs to avoid duplicates.
- For large changes, open an issue first to discuss the approach.
- Follow the existing code style (see below).

## Development Setup

```powershell
git clone https://github.com/yourusername/hotshield.git
cd hotshield
dotnet build
```

Run the app:
```powershell
.\bin\Debug\net9.0-windows\Hotshield.exe
```

### Prerequisites

- Visual Studio 2022 or later / VS Code with C# extension
- Windows 10/11 (the app modifies Windows Firewall)
- Optional: [NETworkManager](https://github.com/BornToBeRoot/NETworkManager) for network diagnostics

## Code Style

- **C# 12** features are fine (targeting .NET 9)
- **WinForms** — use `FlatStyle.System` for native look
- **Naming**: PascalCase for public members, camelCase for locals
- **Async**: Prefer `async/await` for I/O operations
- **Error handling**: Catch specific exceptions; log with `Logger.Log()`
- **Threading**: Use `InvokeRequired` when touching UI from background threads

### File Structure

| Folder | Responsibility |
|--------|----------------|
| `Core/` | Business logic, firewall, engine |
| `Data/` | SQLite repositories |
| `Models/` | Entity classes |
| `Helpers/` | Network detection, export |
| `UI/` | WinForms forms and controls |

Do not add business logic to UI files. Keep UI code thin.

## Commit Guidelines

Use [Conventional Commits](https://www.conventionalcommits.org/):

```
feat: add Ethernet support
fix: handle null SSID in network watcher
docs: update install guide
refactor: simplify rule engine ApplyRules method
test: add unit tests for PresetRepo
chore: update dependencies
```

Prefix descriptions with a verb: Add, Remove, Change, Fix, Update, Refactor.

## Testing

- Manual test on Windows 10 and Windows 11
- Verify firewall rules in `wf.msc`
- Test kill switch, pause, and per-network rules
- Check that rules are cleaned up on app exit

## Pull Requests

1. Fork the repo
2. Create a branch: `git checkout -b fix-firewall-race`
3. Commit: `git commit -am 'fix: handle race condition in rule apply'`
4. Push: `git push origin fix-firewall-race`
5. Open a PR with:
   - Description of what changed and why
   - Screenshots or logs if relevant
   - Checklist of manual tests performed

We review PRs weekly. Thank you for contributing!