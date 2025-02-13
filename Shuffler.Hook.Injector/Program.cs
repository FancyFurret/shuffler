using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Shuffler.Hook.Injector;

internal static class Program
{
    private static int Main(string[] args)
    {
        if (args.Length != 1 || !int.TryParse(args[0], out var pid))
        {
            Console.WriteLine("Usage: Shuffler.Hook.Injector.exe <pid>");
            return 1;
        }

        try
        {
            var process = Process.GetProcessById(pid);
            InjectIntoProcess(process);
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    private static void InjectIntoProcess(Process process)
    {
        var processHandle = IntPtr.Zero;
        var threadHandle = IntPtr.Zero;
        var remoteDllPathAddr = IntPtr.Zero;

        try
        {
            Console.WriteLine($"Injecting into process {process.ProcessName} (PID: {process.Id})");

            // Get the full path to our DLL based on process architecture
            var dllPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                Environment.Is64BitProcess ? "Shuffler.Hook64.dll" : "Shuffler.Hook.dll"
            );

            Console.WriteLine($"Using {(Environment.Is64BitProcess ? "64-bit" : "32-bit")} DLL: {dllPath}");
            if (!File.Exists(dllPath))
                throw new Exception($"Shuffler hook DLL not found: {dllPath}");

            // Open the target process
            processHandle = OpenProcess(
                PROCESS_CREATE_THREAD | PROCESS_QUERY_INFORMATION | PROCESS_VM_OPERATION | PROCESS_VM_WRITE |
                PROCESS_VM_READ,
                false,
                process.Id
            );

            if (processHandle == IntPtr.Zero)
            {
                var error = Marshal.GetLastWin32Error();
                throw new Exception($"Failed to open process: {error}");
            }

            // Allocate memory in the target process for the DLL path
            var dllPathBytes = Encoding.Unicode.GetBytes(dllPath + "\0");
            remoteDllPathAddr = VirtualAllocEx(
                processHandle,
                IntPtr.Zero,
                (uint)dllPathBytes.Length,
                MEM_COMMIT | MEM_RESERVE,
                PAGE_READWRITE
            );

            if (remoteDllPathAddr == IntPtr.Zero)
            {
                var error = Marshal.GetLastWin32Error();
                throw new Exception($"Failed to allocate memory in target process: {error}");
            }

            // Write the DLL path to the target process
            if (!WriteProcessMemory(processHandle, remoteDllPathAddr, dllPathBytes, (uint)dllPathBytes.Length,
                    out _))
            {
                var error = Marshal.GetLastWin32Error();
                throw new Exception($"Failed to write to target process memory: {error}");
            }

            // Get the address of LoadLibraryW in kernel32.dll
            var loadLibraryAddr = GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryW");
            if (loadLibraryAddr == IntPtr.Zero)
            {
                var error = Marshal.GetLastWin32Error();
                throw new Exception($"Failed to get LoadLibraryW address: {error}");
            }

            // Create a remote thread to load our DLL
            threadHandle = CreateRemoteThread(
                processHandle,
                IntPtr.Zero,
                0,
                loadLibraryAddr,
                remoteDllPathAddr,
                0,
                IntPtr.Zero
            );

            if (threadHandle == IntPtr.Zero)
            {
                var error = Marshal.GetLastWin32Error();
                throw new Exception($"Failed to create remote thread: {error}");
            }

            // Wait for the thread to complete and get the module handle
            var waitResult = WaitForSingleObject(threadHandle, 10000);
            if (waitResult != 0)
                throw new Exception($"LoadLibrary thread wait failed with result: {waitResult}");

            GetExitCodeThread(threadHandle, out var moduleHandle);
            if (moduleHandle == IntPtr.Zero)
                throw new Exception($"Failed to inject into process {process.ProcessName}");
        }
        finally
        {
            if (threadHandle != IntPtr.Zero)
                CloseHandle(threadHandle);
            if (processHandle != IntPtr.Zero)
                CloseHandle(processHandle);
            if (remoteDllPathAddr != IntPtr.Zero)
                VirtualFreeEx(processHandle, remoteDllPathAddr, 0, MEM_RELEASE);
        }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flAllocationType,
        uint flProtect);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize,
        out int lpNumberOfBytesWritten);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize,
        IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint WaitForSingleObject(IntPtr handle, uint milliseconds);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetExitCodeThread(IntPtr hThread, out IntPtr lpExitCode);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint dwFreeType);

    private const uint PROCESS_CREATE_THREAD = 0x0002;
    private const uint PROCESS_QUERY_INFORMATION = 0x0400;
    private const uint PROCESS_VM_OPERATION = 0x0008;
    private const uint PROCESS_VM_WRITE = 0x0020;
    private const uint PROCESS_VM_READ = 0x0010;
    private const uint MEM_COMMIT = 0x00001000;
    private const uint MEM_RESERVE = 0x00002000;
    private const uint PAGE_READWRITE = 0x04;
    private const uint MEM_RELEASE = 0x8000;
}