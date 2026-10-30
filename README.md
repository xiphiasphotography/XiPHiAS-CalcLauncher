# XiPHiAS.CalcLauncher

A small C# Windows application with no console window, designed for the calculator key on the Logitech MX Keys S. Requires Windows 11 with Microsoft Windows Calculator installed. Run the launcher as a standard user.

## Open in VS Code

Clone or download this repository and open its folder using **File > Open Folder**. Building requires the .NET 10 SDK. C# support in VS Code is useful for editing; you can build directly in the integrated PowerShell terminal.

Run all commands below from the project folder.

```powershell
dotnet build .\XiPHiAS.CalcLauncher.csproj -c Release
```

## Publish

Create a small, single executable for an x64 PC with the .NET 10 runtime installed:

```powershell
dotnet publish .\XiPHiAS.CalcLauncher.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o .\publish\win-x64
```

Use `publish\win-x64\XiPHiAS.CalcLauncher.exe`. This executable does not include its own .NET runtime. The .NET 10 SDK also installs a compatible runtime.

For a PC without .NET, the following variant includes the runtime and is considerably larger. When the application runs, native runtime files are extracted to the user's default .NET extraction directory.

```powershell
dotnet publish .\XiPHiAS.CalcLauncher.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o .\publish\standalone-win-x64
```

For Windows on ARM, replace `win-x64` with `win-arm64` and choose a separate output folder. Publishing may require internet access to download runtime packs. Republish the standalone variant to incorporate runtime updates.

## Configure the Logitech key

In Logi Options+, select the MX Keys S and its calculator key. If your version offers an action to open an application, choose it and select the published `XiPHiAS.CalcLauncher.exe` using its full path. The action's name may differ between Options+ versions. Use the general key assignment and check any application-specific overrides.

The key should launch only this executable. Remove any additional assignment that also opens Calculator directly. Select the executable itself, without an intermediate PowerShell or terminal window. Keep the published application in a permanent folder. No background service runs; the launcher exits after each key press.

## How it works and limitations

- A session-local mutex allows only one launcher instance at a time. Further invocations are ignored while the launcher is waiting for Calculator.
- The launcher identifies visible top-level windows by their process application identity (`Microsoft.WindowsCalculator_8wekyb3d8bbwe!App`), independently of window titles or the Windows display language. It also checks child windows: in older Store app hosting models, the top-level window may belong to `ApplicationFrameHost.exe` while a child belongs to Calculator. The top-level window is activated.
- When multiple Calculator windows exist, the instance with the most recent process start time is reused. The launcher also reads the host window's own application identity, skips windows hidden (cloaked) by DWM, and associates host windows with Calculator processes through child windows or owned helper windows. Equal or unavailable start times fall back to the existing window order. If a Calculator version hosts multiple windows in one process, their individual creation times cannot be determined this way. The launcher closes no windows and preserves existing calculations.
- An existing window is restored if minimized and activated using `SetForegroundWindow`. Windows then directs keyboard input to the active application. No digits or other keys are simulated, and Calculator's existing internal input focus is preserved. For example, a search field that already had focus may retain it.
- If no window exists, the launcher checks whether Calculator is still starting. Otherwise, it starts the system `calc.exe` exactly once and waits up to 15 seconds for the actual window. The `calc.exe` process itself does not need to acquire a main window: it may only forward the launch request.
- Windows decides whether foreground activation is allowed. The launcher retries briefly and checks the actual foreground window. If activation is denied, it flashes the taskbar button and writes a log entry. Automatic focus therefore cannot be guaranteed in every situation, such as when launched through Options+, while menus are active, or in a locked session. No registry focus settings are changed. `AttachThreadInput` is deliberately avoided because attaching input queues can cause hangs.
- The mutex prevents overlapping launches by this launcher. It cannot prevent another application or a simultaneous manual action from also opening Calculator. An unresponsive Calculator process or one without a window is not terminated; close it manually in Task Manager if startup remains stuck.
- Virtual desktops and secure desktops are not managed. Test on the same regular desktop. Run Options+, Calculator, and the launcher at the same standard user privilege level. Administrator privileges are not required.

## Manual testing

First run the published executable from File Explorer, then through the assigned Logitech key. Wait until Calculator is visibly in the foreground before typing.

| Scenario | Action and expected result |
| --- | --- |
| No Calculator open | Close all Calculator windows. Press the key once: one window appears. Type `12+3` and press Enter: the result is 15. |
| Calculator behind another app | Leave a recognizable calculation visible, switch to Notepad, and press the key: the same window and calculation come to the foreground. Typing goes to Calculator. |
| Minimized | Minimize Calculator and press the key: the same window is restored and receives input. |
| Maximized | Maximize Calculator, switch applications, and press the key: its maximized state is preserved. |
| Rapid repeated presses | Start with no Calculator open and press the key 10 times quickly: one Calculator window appears. Repeat with an existing window. |
| Multiple existing windows | Open three Calculator windows in sequence with different calculations. Bring the oldest one forward, switch to Notepad, and press the key: the most recently opened Calculator comes forward, without a fourth window. Minimize it and repeat. Then close the newest one: the next most recent instance is used. |
| Different language or mode | Test Scientific mode and a different Windows display language: detection still works without matching window titles. |
| Focus denied | Test from an application with an open menu. Windows may deny focus: no additional Calculator is opened; check the taskbar and log. Close the menu and try again. |
| Cold start | Repeat after signing out or restarting, with Options+ running. Pay particular attention to the first key press. |

If digits still appear in Notepad, the test has failed: check the log and the direct Logitech key assignment. A successful build does not prove that focus transfer works on your machine.

Log file: `%LOCALAPPDATA%\XiPHiAS.CalcLauncher\launcher.log`. Normally, only errors are logged. Running `XiPHiAS.CalcLauncher.exe --diagnose` logs the detected host windows, associated process IDs, start times, and selected window without launching Calculator or changing focus. The log is restarted at approximately 256 KB. Exit codes: 0 = focus acquired, diagnostics completed, or overlapping invocation ignored; 1 = startup, detection, or other error; 2 = Windows did not grant foreground focus.

## Technical references

- [Microsoft: SetForegroundWindow and foreground restrictions](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setforegroundwindow)
- [Microsoft: application identities / AUMID](https://learn.microsoft.com/en-us/windows/configuration/store/find-aumid)
- [Microsoft: the risks of AttachThreadInput](https://devblogs.microsoft.com/oldnewthing/20080801-00/?p=21393)
- [Microsoft: publishing a single executable](https://learn.microsoft.com/en-us/dotnet/core/deploying/single-file/)
