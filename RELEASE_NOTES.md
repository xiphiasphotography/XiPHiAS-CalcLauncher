# CalcLauncher 1.0

XiPHiAS CalcLauncher 1.0.0 is a small Windows application for the calculator key on the Logitech MX Keys S.

## New since 0.9

- When multiple Calculator windows exist, the instance with the most recent process start time is activated, preserving existing calculations.
- Improved detection of Calculator in hosted windows, including associations through child windows and owned helper windows.
- Windows hidden (cloaked) by DWM are skipped.
- The new `--diagnose` option logs detected windows, associated processes, start times, and the selected window without launching Calculator or changing focus.

## Features

- Starts Microsoft Windows Calculator or brings an existing window to the foreground.
- Restores minimized windows and preserves existing calculations.
- Identifies Calculator by application identity, independently of window titles and the Windows display language.
- Prevents overlapping launches using a session-local mutex.
- Runs without a console window, background service, or administrator privileges.
- Logs errors and denied foreground activation to `%LOCALAPPDATA%\XiPHiAS.CalcLauncher\launcher.log`.

## Downloads and installation

Requires Windows 11 x64 with Microsoft Windows Calculator installed.

- **XiPHiAS.CalcLauncher-1.0.0-win-x64.zip**: small executable; requires the .NET 10 runtime.
- **XiPHiAS.CalcLauncher-1.0.0-standalone-win-x64.zip**: includes the .NET runtime; no separate .NET installation is required.
- **SHA256SUMS.txt**: SHA-256 checksums for both ZIP files.

Extract a ZIP file to a permanent folder. In Logi Options+, assign the calculator key directly to `XiPHiAS.CalcLauncher.exe`. See the README for configuration instructions and manual test scenarios.

## Known limitations

Windows may deny foreground focus. If this happens, the launcher flashes the taskbar button and writes a log entry. An unresponsive Calculator process is not terminated. Virtual desktops and secure desktops are not managed.

When multiple windows share the same Calculator process, their individual creation times cannot be determined. Equal or unavailable process start times fall back to the existing window order.

## Validation

The Release build and publishing of both x64 variants have been checked. Interactive focus transfer and the Logitech key assignment must be tested on the target computer using the README instructions.
