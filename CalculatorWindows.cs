using System.Diagnostics;
using System.Runtime.InteropServices;

internal static class CalculatorWindows
{
    private const string CalculatorId = "Microsoft.WindowsCalculator_8wekyb3d8bbwe!App";
    private sealed record Candidate(nint Window, uint ProcessId, DateTime Started);

    internal static nint FindNewest() => FindCandidates().FirstOrDefault()?.Window ?? 0;

    internal static string Describe()
    {
        var candidates = FindCandidates();
        return "Calculator-diagnose (nieuwste eerst): " + string.Join("; ", candidates.Select(c =>
            $"hwnd={c.Window}, pid={c.ProcessId}, gestart={c.Started:O}"))
            + $"; gekozen={candidates.FirstOrDefault()?.Window ?? 0}";
    }

    private static List<Candidate> FindCandidates()
    {
        var windows = new List<nint>();
        Native.EnumWindows((window, _) => { windows.Add(window); return true; }, 0);
        var processCache = new Dictionary<uint, (bool Calculator, DateTime Started)>();
        (bool Calculator, DateTime Started) ProcessInfo(uint pid)
        {
            if (processCache.TryGetValue(pid, out var cached)) return cached;
            var info = (Calculator: Program.IsCalculatorProcess(pid), Started: DateTime.MinValue);
            if (info.Calculator)
            {
                try
                {
                    using var process = Process.GetProcessById((int)pid);
                    info.Started = process.StartTime.ToUniversalTime();
                }
                catch (Exception ex) when (ex is ArgumentException or InvalidOperationException
                    or System.ComponentModel.Win32Exception) { }
            }
            return processCache[pid] = info;
        }

        var candidates = new List<Candidate>();
        foreach (var window in windows)
        {
            if (!Native.IsWindowVisible(window) || Native.GetWindow(window, 4) != 0) continue;
            // CoreWindow can have WS_VISIBLE while DWM actually displays a different
            // ApplicationFrameWindow. Such cloaked windows cannot receive foreground focus.
            if (Native.DwmGetWindowAttribute(window, 14, out int cloaked, sizeof(int)) == 0
                && cloaked != 0) continue;

            uint calculatorPid = 0;
            DateTime started = DateTime.MinValue;
            void Associate(nint associatedWindow)
            {
                Native.GetWindowThreadProcessId(associatedWindow, out uint pid);
                var info = ProcessInfo(pid);
                if (info.Calculator && (calculatorPid == 0 || info.Started > started))
                {
                    calculatorPid = pid;
                    started = info.Started;
                }
            }
            Associate(window);
            Native.EnumChildWindows(window, (child, _) => { Associate(child); return true; }, 0);
            bool explicitCalculator = HasCalculatorAppId(window);
            if (calculatorPid == 0 && !explicitCalculator) continue;

            // A suspended UWP app can detach its CoreWindow from the frame. Its owned
            // helper windows (e.g. IME) still link the Calculator process to that frame.
            // Only use this association after independently verifying the frame's identity.
            if (explicitCalculator)
                foreach (var owned in windows)
                    if (Native.GetAncestor(owned, 3 /* GA_ROOTOWNER */) == window) Associate(owned);

            candidates.Add(new Candidate(window, calculatorPid, started));
        }
        // Stable sorting keeps the existing Z order when timestamps are equal/unknown.
        // Calculator normally uses one process per instance. Shared-process windows
        // cannot be ordered by creation time using the process timestamp alone.
        return candidates.OrderByDescending(c => c.Started).ToList();
    }

    private static bool HasCalculatorAppId(nint window)
    {
        Guid iid = typeof(Native.IPropertyStore).GUID;
        int hr = Native.SHGetPropertyStoreForWindow(window, ref iid, out var store);
        if (hr < 0 || store is null) return false;
        try
        {
            var key = new Native.PropertyKey
            {
                FormatId = new Guid("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3"), PropertyId = 5
            };
            hr = store.GetValue(ref key, out var value);
            try
            {
                return hr >= 0 && value.Type == 31 /* VT_LPWSTR */
                    && string.Equals(Marshal.PtrToStringUni(value.Pointer), CalculatorId,
                        StringComparison.OrdinalIgnoreCase);
            }
            finally { Native.PropVariantClear(ref value); }
        }
        finally { Marshal.ReleaseComObject(store); }
    }
}

internal static partial class Native
{
    [DllImport("dwmapi.dll")]
    internal static extern int DwmGetWindowAttribute(nint hwnd, uint attribute, out int value, int size);
    [DllImport("user32.dll")] internal static extern nint GetAncestor(nint hwnd, uint flags);
    [DllImport("shell32.dll")]
    internal static extern int SHGetPropertyStoreForWindow(nint hwnd, ref Guid iid,
        [MarshalAs(UnmanagedType.Interface)] out IPropertyStore? store);
    [DllImport("ole32.dll")] internal static extern int PropVariantClear(ref PropVariant value);

    [StructLayout(LayoutKind.Sequential)]
    internal struct PropertyKey { internal Guid FormatId; internal uint PropertyId; }
    // PROPVARIANT is 24 bytes on x64/ARM64 and 16 bytes on x86.
    [StructLayout(LayoutKind.Sequential)]
    internal struct PropVariant
    {
        internal ushort Type, Reserved1, Reserved2, Reserved3;
        internal nint Pointer, Extra;
    }
    [ComImport, Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IPropertyStore
    {
        [PreserveSig] int GetCount(out uint count);
        [PreserveSig] int GetAt(uint index, out PropertyKey key);
        [PreserveSig] int GetValue(ref PropertyKey key, out PropVariant value);
        [PreserveSig] int SetValue(ref PropertyKey key, ref PropVariant value);
        [PreserveSig] int Commit();
    }
}
