# Hotshield — Data Guardian

> Windows desktop app that protects your metered networks (WiFi, USB tether, Ethernet) from background data drains. You choose which apps are allowed — everything else is blocked by Windows Firewall.

![Platform](https://img.shields.io/badge/platform-Windows-blue)
![License](https://img.shields.io/badge/license-GPL--3.0-green)

## Quick Start

1. Download the latest `Hotshield.zip` from [Releases](https://github.com/yourusername/hotshield/releases)
2. Extract to a folder (e.g. `C:\Program Files\Hotshield`)
3. Run `Hotshield.exe` as **Administrator** (right-click → Run as administrator)
4. Connect to your network and click **Turn ON Protection**

That's it. No installation wizard, no service, no driver.

## How It Works

Hotshield uses **Windows Firewall** (netsh) to enforce rules:

- When you connect to a network and turn on protection, Hotshield creates a block rule that blocks ALL outbound traffic on that interface.
- It then enables allow-rules only for the apps you explicitly choose.
- When you disconnect or turn off protection, everything is cleaned up automatically.

### Features

| Feature | Description |
|---------|-------------|
| **Per-network rules** | Different allowed apps per WiFi / USB tether / Ethernet |
| **Kill Switch** | Emergency: block ALL internet with one click |
| **Pause** | Temporarily stop blocking (5 min – 1 hour) |
| **Connection History** | See all networks you've ever used |
| **Live Processes** | View apps currently using the network |
| **Import / Export** | Backup your configuration as JSON |

## Screenshots

(Add screenshots here — open an issue or PR)

## Requirements

- Windows 10 / 11 (64-bit)
- .NET 9 Runtime (included in the portable build — no separate install needed)
- Administrator privileges (required to modify firewall rules)

## Building from Source

```powershell
git clone https://github.com/yourusername/hotshield.git
cd hotshield
dotnet build
```

Then run:
```powershell
.\bin\Debug\net9.0-windows\Hotshield.exe
```

## Project Structure

```
Hotshield/
├── Core/              # Business logic (firewall, network watching, rules)
│   ├── FirewallManager.cs    # Windows Firewall via netsh
│   ├── RuleEngine.cs         # Core decision engine
│   ├── NetworkWatcher.cs     # Network change detection
│   └── ...
├── Data/              # SQLite repositories (networks, rules, presets)
├── Models/            # Entity classes
├── Helpers/           # WiFi, adapter detection, export
├── UI/                # WinForms interface
│   ├── DashboardForm.cs      # Main window
│   ├── AppConfigForm.cs      # App allowlist editor
│   └── ...
├── Resources/         # Icons and images
└── Program.cs         # Entry point, tray icon
```

## Troubleshooting

**"Access denied" when building or running:**
- Make sure Hotshield.exe is not running (check tray icon → Exit)
- If blocked by antivirus, add an exception

**Internet still works when protection is ON:**
- You must run as Administrator
- Check that "Hotshield_Protection_Block" rule is enabled in Windows Firewall

**No networks detected:**
- Connect to WiFi or USB tether first
- The app detects WiFi SSIDs and USB tethers automatically

## Contributing

1. Fork the repo
2. Create a feature branch (`git checkout -b my-feature`)
3. Commit your changes (`git commit -am 'Add feature'`)
4. Push (`git push origin my-feature`)
5. Open a Pull Request

Please read [CONTRIBUTING.md](CONTRIBUTING.md) for coding standards.

## License

This project is licensed under the **GNU General Public License v3.0** — see [LICENSE](LICENSE) for details.

You are free to use, modify, and distribute this software, provided that any derivative work is also released under the same license.

## Acknowledgements

- [NETworkManager](https://github.com/BornToBeRoot/NETworkManager) — inspiration for network detection
- [SharpPcap](https://github.com/chmorgan/sharppcap) — packet capture concepts
- All contributors and testers

---

**Note:** Hotshield modifies Windows Firewall rules. Always verify rules in `wf.msc` if you suspect misconfiguration.