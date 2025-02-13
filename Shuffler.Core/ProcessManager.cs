using System.Runtime.InteropServices;
using System.Diagnostics;

namespace Shuffler.Core;

public static class ProcessManager
{
    private static readonly ILogger Logger = LogManager.Create(nameof(ProcessManager));

    [DllImport("ntdll.dll")]
    private static extern int NtSuspendProcess(IntPtr processHandle);

    [DllImport("ntdll.dll")]
    private static extern int NtResumeProcess(IntPtr processHandle);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool IsWow64Process(IntPtr hProcess, out bool wow64Process);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

    [StructLayout(LayoutKind.Sequential)]
    private struct STARTUPINFO
    {
        public int cb;
        public string lpReserved;
        public string lpDesktop;
        public string lpTitle;
        public uint dwX;
        public uint dwY;
        public uint dwXSize;
        public uint dwYSize;
        public uint dwXCountChars;
        public uint dwYCountChars;
        public uint dwFillAttribute;
        public uint dwFlags;
        public short wShowWindow;
        public short cbReserved2;
        public IntPtr lpReserved2;
        public IntPtr hStdInput;
        public IntPtr hStdOutput;
        public IntPtr hStdError;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PROCESS_INFORMATION
    {
        public IntPtr hProcess;
        public IntPtr hThread;
        public uint dwProcessId;
        public uint dwThreadId;
    }

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr CreateProcess(
        string lpApplicationName,
        string lpCommandLine,
        IntPtr lpProcessAttributes,
        IntPtr lpThreadAttributes,
        bool bInheritHandles,
        uint dwCreationFlags,
        IntPtr lpEnvironment,
        string lpCurrentDirectory,
        ref STARTUPINFO lpStartupInfo,
        out PROCESS_INFORMATION lpProcessInformation);

    public static bool IsSuspended(Process process)
    {
        return process.Threads.Cast<ProcessThread>().All(thread => thread.WaitReason == ThreadWaitReason.Suspended);
    }

    public static void SuspendProcess(IntPtr processHandle)
    {
        var result = NtSuspendProcess(processHandle);
        if (result != 0)
            throw new Exception($"Failed to suspend process. Error code: {result}");
    }

    public static void ResumeProcess(IntPtr processHandle)
    {
        var result = NtResumeProcess(processHandle);
        if (result != 0)
            throw new Exception($"Failed to resume process. Error code: {result}");
    }

    public static bool Is64BitProcess(IntPtr process)
    {
        if (!Environment.Is64BitOperatingSystem)
            return false;

        if (!IsWow64Process(process, out var isWow64))
            throw new Exception($"Failed to determine process architecture: {Marshal.GetLastWin32Error()}");

        return !isWow64; // If it's not running under WOW64, it's a native 64-bit process
    }

    public static Process CreateProcess(string exePath)
    {
        if (!File.Exists(exePath))
            throw new InvalidOperationException($"Executable not found: {exePath}");

        var si = new STARTUPINFO();
        si.cb = Marshal.SizeOf<STARTUPINFO>();

        var success = CreateProcess(
            exePath,
            null,
            IntPtr.Zero,
            IntPtr.Zero,
            false,
            0,
            IntPtr.Zero,
            Path.GetDirectoryName(exePath)!,
            ref si,
            out var pi
        );

        if (success == IntPtr.Zero)
            throw new InvalidOperationException($"Failed to launch process. Error code: {Marshal.GetLastWin32Error()}");

        try
        {
            return Process.GetProcessById((int)pi.dwProcessId);
        }
        finally
        {
            if (pi.hProcess != IntPtr.Zero) CloseHandle(pi.hProcess);
            if (pi.hThread != IntPtr.Zero) CloseHandle(pi.hThread);
        }
    }
}