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
3. **`Service/DeviceManager.cs`** — BLE layer, two classes:
   - `DeviceManager` wraps a `DeviceInformation.CreateWatcher` with the AQS filter for the BLE protocol GUID `{bb7bb05e-5972-42b5-94fc-76eaa7084d49}`. It only tracks **paired** devices (`devInfo.Pairing.IsPaired`), dedups by `devInfo.Id` in a `ConcurrentDictionary`, and notifies the UI via `IDeviceNotification.OnNewDevice`. The `scanForEver` flag restarts the watcher in its `Stopped` handler for continuous discovery; otherwise it scans once until `EnumerationCompleted`.
   - `DeviceBLE` owns the GATT connection for a single device. It reads the standard **Battery Service `0x180F`** / **Battery Level characteristic `0x2A19`**, with 30 s connect and 5 s read timeouts. If the service isn't present on first connect, `supportBatterylevel` is set false and that device is skipped on subsequent ticks. A failed read leaves `batteryLevel = -1`, which `Settings.UpdateIcon` filters out.

### WinRT bridging

The project consumes WinRT APIs (`Windows.Devices.Bluetooth.*`, `Windows.Storage.Streams`) from .NET Framework 4.8 via the `DirectWindowsWinmd.Net 10.0.15063.0` NuGet package, which provides `Windows.winmd` and `System.Runtime.WindowsRuntime.dll` references. When adding new WinRT calls, expect `IAsyncOperation<T>` — convert with `.AsTask()` and use `Task.Wait(timeoutMs)` rather than `await` (the codebase is synchronous on the UI thread inside the timer tick).

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
