# Installation Guide

## Requirements

- **Windows 10 or 11** (64-bit)
- **Administrator privileges** — required to modify Windows Firewall rules
- No .NET installation needed — the portable build includes the runtime

## Steps

### 1. Download

Download `Hotshield.zip` from the latest GitHub Release and extract it to a permanent location, for example:

```
C:\Program Files\Hotshield\
```

### 2. First Run (Important)

**Right-click** `Hotshield.exe` → **Run as administrator**.

> If you skip this step, the firewall rules will not be applied.

### 3. Initial Setup

1. A shield icon appears in your system tray.
2. Connect to your WiFi or USB tether.
3. Open the Dashboard (tray icon → Open Dashboard, or double-click the tray icon).
4. Click **Turn ON Protection**.
5. Click **Allow Apps** to select which apps may use data.
6. Everything else is blocked.

## Common Issues

### "Access denied" or "File in use"

Hotshield is still running. Right-click the tray icon → **Exit**, wait 5 seconds, then run again.

### Internet not blocked

- Ensure you ran `Hotshield.exe` as Administrator.
- Verify the rule exists: open `wf.msc`, look for `Hotshield_Protection_Block` under Outbound Rules.

### Database locked

Close all instances, delete `%AppData%\Hotshield\hotshield.db*`, and restart.

## Uninstall

1. Right-click tray icon → Exit
2. Delete the extracted folder
3. Delete `%AppData%\Hotshield\hotshield.db*` to remove saved networks

That's all — no registry entries, no services, no drivers.