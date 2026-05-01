# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build / run

Windows-only WinForms app targeting **.NET Framework 4.8** (`WinExe`, AnyCPU). The solution uses the legacy (non-SDK-style) csproj format with `ToolsVersion=15.0`, so it must be built with MSBuild from Visual Studio 2017+ Build Tools — `dotnet build` will not work.

```sh
# Restore NuGet packages (creates ../packages/ next to the solution)
nuget restore ozBluetoothLEBatteryMonitor.sln

# Build
msbuild ozBluetoothLEBatteryMonitor.sln /p:Configuration=Release

# Output: ozBluetoothLEBatteryMonitor/bin/Release/BluetoothLEBatteryMonitor.exe
```

There is no test project, no linter, and no CI configured. Validation is by running the produced `.exe` on a Windows machine that has paired BLE devices.

## Architecture

The app is a **single-form WinForms tray application**. The form is created but kept hidden — `Settings.SetVisibleCore` suppresses visibility unless the user explicitly opens it from the tray menu. The form's job is to host the `NotifyIcon` and a `Timer` that drives polling.

Three layers, all in namespace `BluetoothLEBatteryMonitor`:

1. **`Program.cs`** — entry point; just runs `Application.Run(new Settings())`.
2. **UI** — `Settings` (main form + tray host) and `Info` (per-device list dialog). `Settings.UpdateIcon()` is the polling tick: it calls `device.UpdateBatteryLevel()` on every tracked device, picks the lowest battery level across all devices, maps that to one of five tray icons (`Icon_Battery_20/40/60/80/100`), updates the tooltip, and fires a balloon notification once per low-battery transition (`lowBatteryNotificationDone` latch resets when level rises above 20%).
3. **`Service/DeviceManager.cs`** — Bluetooth layer, two classes:
   - `DeviceManager` runs **two** `DeviceInformation.CreateWatcher` instances in parallel: one for BLE (protocol GUID `{bb7bb05e-5972-42b5-94fc-76eaa7084d49}`) and one for Bluetooth Classic / BR-EDR (`{e0cbf06c-cd8b-4647-bb8a-263b43f0f974}`). Both feed the same `ConcurrentDictionary<string, DeviceBLE>` keyed by `devInfo.Id`. Only **paired** devices (`devInfo.Pairing.IsPaired`) are tracked. The watcher's `Updated` handler does two things: removes the device if `System.Devices.Aep.IsPaired` flips to false, and forwards property bag changes into the cached `DeviceBLE` via `UpdateProperties`. `Removed` removes the device. `scanForEver` restarts each watcher in its `Stopped` handler for continuous discovery. `IDeviceNotification` exposes `OnNewDevice` and `OnDeviceRemoved`.
   - `DeviceBLE` owns one device's battery state and uses a **strategy chain** in `UpdateBatteryLevel()`, in priority order: (1) GATT Battery Service `0x180F` / level characteristic `0x2A19` — BLE-only, skipped if the service isn't present (`supportGattBattery` latches false); (2) the AEP DEVPROPKEY `{104EA319-6EE2-4701-BD47-8DDBF425BBE5} 2` (`DEVPKEY_Device_BatteryLevel`, byte 0–100); (3) the coarse `System.Devices.BatteryLife` enum mapped to bucket percentages (Critical→10, Low→30, Average→60, Full→90). Classic devices skip strategy 1 entirely. Each strategy returns `bool`; the first to succeed wins. A failed full chain leaves the previous `batteryLevel`; `-1` means never read, which `Settings.UpdateIcon` filters out. GATT uses 30 s connect / 5 s read timeouts.

### WinRT bridging

The project consumes WinRT APIs (`Windows.Devices.Bluetooth.*`, `Windows.Devices.Enumeration`, `Windows.Storage.Streams`) from .NET Framework 4.8 via the `DirectWindowsWinmd.Net 10.0.15063.0` NuGet package, which provides `Windows.winmd` and `System.Runtime.WindowsRuntime.dll` references. When adding new WinRT calls, expect `IAsyncOperation<T>` — convert with `.AsTask()` and use `Task.Wait(timeoutMs)` rather than `await` (the codebase is synchronous on the UI thread inside the timer tick).

`DeviceInformation.Properties` is a string-keyed bag that surfaces both canonical names (`System.Devices.BatteryLife`, `System.Devices.Aep.IsConnected`) and raw `DEVPROPKEY`s in the form `"{guid} pid"` (e.g. `"{104EA319-6EE2-4701-BD47-8DDBF425BBE5} 2"`). To populate them, the keys must be passed in the `requestedProperties` array to `CreateWatcher` — they're not delivered otherwise. `Updated` events carry only the changed keys, so `DeviceBLE` merges them into a `ConcurrentDictionary<string, object>` cache rather than replacing it.

### Persistence

All user settings live in the registry under **`HKCU\SOFTWARE\BluetoothLEBatteryMonitor`**:
- `IntervalMin` (DWORD, default 5) — polling interval in minutes; `numericUpDownRefreshPeriod` writes this and reconfigures `IconTimer.Interval` live.
- `NotificationEnabled` (DWORD, default 1).
- `AutomaticDetectionEnabled` (DWORD, default 0) — drives `DeviceManager.scan(scanForEver)`.

Auto-start is implemented by writing the exe path to **`HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Run\BluetoothLEBatteryMonitor`**.

## Conventions worth preserving

- The root namespace is `BluetoothLEBatteryMonitor` even though the repo/folder is `ozBluetoothLEBatteryMonitor`. Don't rename.
- Tray-icon thresholds are baked into `Settings.UpdateIcon` as cascading `if/else` (≥90/70/50/30/>0 → 100/80/60/40/20). The five `.ico` resources must stay in lockstep with these buckets.
- New BLE features should go through `DeviceBLE` rather than reading GATT from the UI layer; `Settings` only ever talks to `DeviceManager` and `DeviceBLE` accessors.
