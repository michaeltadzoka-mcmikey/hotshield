# Release History

## [v1.0.0] — 2025-06-30

**Initial working release**

### Features
- Per-network data protection (WiFi, USB tether, Ethernet)
- Kill Switch — emergency block-all toggle
- Pause protection (5 min / 15 min / 30 min / 1 hour)
- Live process monitor
- Connection history
- Import / Export configuration

### Bug Fixes (post-refactor)
- Fix 2: Name-based database seeding (no hardcoded IDs)
- Fix 3: Centralized preset assignment via `EnsureNetworkHasPreset()`
- Fix 4: Block rule covers `ras` interface type (USB tether)
- Fix 5: Non-WiFi network fallback via MAC address

### Known Issues
- Build must be done after exiting the running app (file lock)
- CA1416 platform compatibility warnings (cosmetic, not errors)

### Build
```
dotnet build
# Output: bin\Debug\net9.0-windows\Hotshield.exe
```

Portable release requires .NET 9 runtime — included.