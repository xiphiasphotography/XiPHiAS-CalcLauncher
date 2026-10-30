using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

internal static class Program
{
    private const string CalculatorId = "Microsoft.WindowsCalculator_8wekyb3d8bbwe!App";
    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "XiPHiAS.CalcLauncher", "launcher.log");

    [STAThread]
    private static int Main()
    {
        // Keep the lock through startup and focus, including a short debounce period.
        using var mutex = new Mutex(false, @"Local\XiPHiAS.CalcLauncher");
        bool acquired = false;
        try
        {
            try { acquired = mutex.WaitOne(0); }
            catch (AbandonedMutexException) { acquired = true; }
            if (!acquired) return 0;

            var window = FindCalculator();
            if (window == 0)
            {
                // A Calculator process may still be constructing its first window.
                // Wait before deciding to launch; never launch repeatedly on timeout.
                if (CalculatorIsStarting()) window = WaitForWindow(TimeSpan.FromSeconds(10));
                if (window == 0 && !CalculatorIsStarting())
                {
                    using var process = Process.Start(new ProcessStartInfo(
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "calc.exe"))
                    { UseShellExecute = true });
                    window = WaitForWindow(TimeSpan.FromSeconds(15));
                }
            }
            if (window == 0)
            {
                Log("Geen Calculator-venster gevonden. Controleer of Windows Calculator is geïnstalleerd en reageert.");
                return 1;
            }

            // ShowWindowAsync avoids blocking on a hung target thread.
            Native.ShowWindowAsync(window, Native.IsIconic(window) ? 9 : 5);
            for (int i = 0; i < 8; i++)
            {
                Native.SetForegroundWindow(window);
                Thread.Sleep(75);
                if (Native.GetForegroundWindow() == window)
                {
                    Thread.Sleep(250);
                    return 0;
                }
            }
            var flash = new Native.FLASHWINFO
            {
                cbSize = (uint)Marshal.SizeOf<Native.FLASHWINFO>(), hwnd = window,
                dwFlags = 3, uCount = 3, dwTimeout = 0
            };
            Native.FlashWindowEx(ref flash);
            Log("Windows heeft voorgrondfocus geweigerd. Klik op Calculator in de taakbalk. Start de launcher rechtstreeks vanuit de toegewezen toets, zonder administratorrechten.");
            return 2;
        }
        catch (Exception ex) { Log(ex.ToString()); return 1; }
        finally { if (acquired) mutex.ReleaseMutex(); }
    }

    private static nint WaitForWindow(TimeSpan timeout)
    {
        var timer = Stopwatch.StartNew();
        do
        {
            var window = FindCalculator();
            if (window != 0) return window;
            Thread.Sleep(100);
        } while (timer.Elapsed < timeout);
        return 0;
    }

    private static bool CalculatorIsStarting()
    {
        foreach (string name in new[] { "CalculatorApp", "Calculator" })
        {
            var processes = Process.GetProcessesByName(name);
            try
            {
                if (processes.Any(p => IsCalculatorProcess((uint)p.Id))) return true;
            }
            finally { foreach (var process in processes) process.Dispose(); }
        }
        return false;
    }

    private static nint FindCalculator()
    {
        nint result = 0;
        var cache = new Dictionary<uint, bool>();
        bool Matches(nint window)
        {
            Native.GetWindowThreadProcessId(window, out uint pid);
            if (!cache.TryGetValue(pid, out bool match))
                cache[pid] = match = IsCalculatorProcess(pid);
            return match;
        }
        // EnumWindows is in Z order: reuse the frontmost existing Calculator.
        Native.EnumWindows((window, _) =>
        {
            if (!Native.IsWindowVisible(window) || Native.GetWindow(window, 4) != 0) return true;
            bool match = Matches(window);
            if (!match)
            {
                // Older Store builds: the top-level HWND belongs to ApplicationFrameHost,
                // while a descendant belongs to CalculatorApp. Activate the top-level HWND.
                Native.EnumChildWindows(window, (child, _) =>
                {
                    if (!Matches(child)) return true;
                    match = true;
                    return false;
                }, 0);
            }
            if (!match) return true;
            result = window;
            return false;
        }, 0);
        return result;
    }

    private static bool IsCalculatorProcess(uint pid)
    {
        nint process = Native.OpenProcess(0x1000, false, pid);
        if (process == 0) return false;
        try
        {
            uint length = 0;
            if (Native.GetApplicationUserModelId(process, ref length, null) != 122) return false;
            var id = new StringBuilder((int)length);
            return Native.GetApplicationUserModelId(process, ref length, id) == 0
                && string.Equals(id.ToString(), CalculatorId, StringComparison.OrdinalIgnoreCase);
        }
        finally { Native.CloseHandle(process); }
    }

    private static void Log(string message)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
            if (File.Exists(LogPath) && new FileInfo(LogPath).Length > 256_000) File.Delete(LogPath);
            File.AppendAllText(LogPath, $"{DateTimeOffset.Now:O} {message}{Environment.NewLine}");
        }
        catch { /* Logging must never display a console or break the hotkey. */ }
    }
}

internal static class Native
{
    internal delegate bool EnumProc(nint hwnd, nint parameter);
    [DllImport("user32.dll")] internal static extern bool EnumWindows(EnumProc callback, nint parameter);
    [DllImport("user32.dll")] internal static extern bool EnumChildWindows(nint parent, EnumProc callback, nint parameter);
    [DllImport("user32.dll")] internal static extern uint GetWindowThreadProcessId(nint hwnd, out uint pid);
    [DllImport("user32.dll")] internal static extern bool IsWindowVisible(nint hwnd);
    [DllImport("user32.dll")] internal static extern nint GetWindow(nint hwnd, uint command);
    [DllImport("user32.dll")] internal static extern bool IsIconic(nint hwnd);
    [DllImport("user32.dll")] internal static extern bool ShowWindowAsync(nint hwnd, int command);
    [DllImport("user32.dll")] internal static extern bool SetForegroundWindow(nint hwnd);
    [DllImport("user32.dll")] internal static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] internal static extern bool FlashWindowEx(ref FLASHWINFO info);
    [DllImport("kernel32.dll")] internal static extern nint OpenProcess(uint access, bool inherit, uint pid);
    [DllImport("kernel32.dll")] internal static extern bool CloseHandle(nint handle);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    internal static extern int GetApplicationUserModelId(nint process, ref uint length, StringBuilder? id);
    [StructLayout(LayoutKind.Sequential)]
    internal struct FLASHWINFO
    {
        internal uint cbSize;
        internal nint hwnd;
        internal uint dwFlags, uCount, dwTimeout;
    }
}
