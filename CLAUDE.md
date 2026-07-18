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
3. **`Service/`** — Bluetooth layer, one type per file:
   - `Service/IDeviceNotification.cs` — the UI callback contract; exposes `OnNewDevice` and `OnDeviceRemoved`.
   - `Service/DeviceManager.cs` (`DeviceManager`) runs **two** `DeviceInformation.CreateWatcher` instances in parallel: one for BLE (protocol GUID `{bb7bb05e-5972-42b5-94fc-76eaa7084d49}`) and one for Bluetooth Classic / BR-EDR (`{e0cbf06c-cd8b-4647-bb8a-263b43f0f974}`). Both feed the same `ConcurrentDictionary<string, BatteryDevice>` keyed by `devInfo.Id`, each device tagged with a `DeviceTransport` (`Ble` / `Classic`). Only **paired** devices (`devInfo.Pairing.IsPaired`) are tracked. The watcher's `Updated` handler does two things: removes the device if `System.Devices.Aep.IsPaired` flips to false, and forwards property bag changes into the cached `BatteryDevice` via `UpdateProperties`. `Removed` removes the device. `scanForEver` restarts each watcher in its `Stopped` handler for continuous discovery.
   - `Service/BatteryDevice.cs` (`BatteryDevice`) owns one device's battery state — a thin state holder + `IBatteryDeviceContext`. Each device **binds to one `IBatteryProvider`** and remembers it (see `Service/Battery/`). `UpdateBatteryLevel()` first re-reads the bound provider as a fast path; if that yields nothing it probes the other priority-ordered providers and binds to the first whose `ReadBattery(this)` returns a value. Re-probing on an empty read (rather than staying stuck on a provider that went quiet) lets a device recover from a transient failure and lets a **higher-priority** provider preempt when it comes online (e.g. GATT once the device connects). A `null` reading means "can't read right now" and leaves the previous `batteryLevel`; `-1` means never read, which `Settings.UpdateIcon` filters out.
   - **`Service/Battery/`** — the extensible battery layer, split into two sub-layers (one type per file):
     - **`Battery/Core/`** — abstractions only, so discovery and providers depend on Core, not on each other. `IBatteryDeviceContext` (read-only view `BatteryDevice` exposes: `DeviceId`, mutable `DeviceName`, `Transport`, `TryGetProperty`); `IBatteryProvider` (a single `ReadBattery(ctx)` → `int?` that may do I/O and cache what it establishes — a `null` return means "can't read this device right now", covering both "doesn't apply" and "momentarily unavailable", so it doubles as the capability check); `DeviceTransport` (`Ble` / `Classic`, consulted by providers like GATT that apply to only one transport); `IDeviceLinkState` (optional; a cheap no-I/O `IsLinkUp(ctx)` that `BatteryDevice.IsConnected()` uses for BLE — implemented only by the GATT provider); `DeviceProperties` (the canonical `PROP_*` property-bag keys, referenced by both the discovery layer's `requestedProperties` and the providers).
     - **`Battery/Providers/`** — `BatteryProviderRegistry` (ordered `Register(Func<IBatteryProvider>)` + `CreateProviders()`; registration order **is** priority order; the static ctor wires the four built-ins) plus one folder per device family. **To add a new source, implement `IBatteryProvider` and `Register` a factory** (before the first `BatteryDevice` is created) — no changes to `BatteryDevice`. One instance is created per device, so a provider may cache per-device state. The families, in priority order: `Gatt/GattBatteryProvider` (GATT Battery Service `0x180F` / char `0x2A19`, BLE-only, `supportGattBattery` latch, 30 s connect / 5 s read timeouts, also implements `IDeviceLinkState`); `DeviceProperty/DevicePropertyBatteryProvider` (AEP DEVPROPKEY `{104EA319-…} 2` = `DEVPKEY_Device_BatteryLevel`, byte 0–100); `Apple/AppleBatteryProvider`; `Coarse/CoarseBatteryProvider` (`System.Devices.BatteryLife` → Critical→10, Low→30, Average→60, Full→90).
     - `Apple/AppleBatteryProvider` self-contains its raw-HID reader (no separate helper class): Apple "Magic" devices (Mouse/Trackpad/Keyboard) report battery only through a vendor HID **input report id `0x90`** (byte[2] = 0–100), invisible to the WinRT Bluetooth property bag. The provider enumerates HID interfaces via SetupAPI (private P/Invoke), opens each Apple one (`HidD_GetAttributes` VID `0x004C` BT / `0x05AC` USB) with `CreateFile`, and calls `HidD_GetInputReport`. Devices are matched by Bluetooth MAC (Apple reports the MAC as the HID serial number, via `HidD_GetSerialNumberString`); a single-Apple-device + Apple-looking-name fallback covers systems where the serial isn't the MAC. No extra NuGet dependency.

### WinRT bridging

The project consumes WinRT APIs (`Windows.Devices.Bluetooth.*`, `Windows.Devices.Enumeration`, `Windows.Storage.Streams`) from .NET Framework 4.8 via the `DirectWindowsWinmd.Net 10.0.15063.0` NuGet package, which provides `Windows.winmd` and `System.Runtime.WindowsRuntime.dll` references. When adding new WinRT calls, expect `IAsyncOperation<T>` — convert with `.AsTask()` and use `Task.Wait(timeoutMs)` rather than `await` (the codebase is synchronous on the UI thread inside the timer tick).

`DeviceInformation.Properties` is a string-keyed bag that surfaces both canonical names (`System.Devices.BatteryLife`, `System.Devices.Aep.IsConnected`) and raw `DEVPROPKEY`s in the form `"{guid} pid"` (e.g. `"{104EA319-6EE2-4701-BD47-8DDBF425BBE5} 2"`). To populate them, the keys must be passed in the `requestedProperties` array to `CreateWatcher` — they're not delivered otherwise. `Updated` events carry only the changed keys, so `BatteryDevice` merges them into a `ConcurrentDictionary<string, object>` cache rather than replacing it.

### Persistence

All user settings live in the registry under **`HKCU\SOFTWARE\BluetoothLEBatteryMonitor`**:
- `IntervalMin` (DWORD, default 5) — polling interval in minutes; `numericUpDownRefreshPeriod` writes this and reconfigures `IconTimer.Interval` live.
- `NotificationEnabled` (DWORD, default 1).
- `AutomaticDetectionEnabled` (DWORD, default 0) — drives `DeviceManager.scan(scanForEver)`.

Auto-start is implemented by writing the exe path to **`HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Run\BluetoothLEBatteryMonitor`**.

## Conventions worth preserving

- The root namespace is `BluetoothLEBatteryMonitor` even though the repo/folder is `ozBluetoothLEBatteryMonitor`. Don't rename.
- Tray-icon thresholds are baked into `Settings.UpdateIcon` as cascading `if/else` (≥90/70/50/30/>0 → 100/80/60/40/20). The five `.ico` resources must stay in lockstep with these buckets.
- New BLE features should go through `BatteryDevice` rather than reading GATT from the UI layer; `Settings` only ever talks to `DeviceManager` and `BatteryDevice` accessors.
