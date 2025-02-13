using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Shuffler.Core.Hook;

public class ShufflerHookException : Exception
{
    public ShufflerHookException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public ShufflerHookException(string message) : base(message)
    {
    }
}

public class ShufflerHook : IAsyncDisposable
{
    public Process Process { get; }
    public bool IsConnected => Process is { HasExited: false } && _ipc.IsConnected;

    private ShufflerHookIpc _ipc;
    private static readonly ILogger Logger = LogManager.Create(nameof(ShufflerHook));

    private ShufflerHook(Process process, ShufflerHookIpc ipc)
    {
        Process = process;
        _ipc = ipc;
    }

    public static async Task<ShufflerHook> HookProcessAsync(Process process, CancellationToken cancellationToken = default)
    {
        Logger.Info($"Hooking process {process.Id}");

        // Check if our DLL is already loaded
        process.Refresh();
        var is64Bit = ProcessManager.Is64BitProcess(process.Handle);
        var dllName = is64Bit ? "Shuffler.Hook64.dll" : "Shuffler.Hook.dll";

        if (process.Modules.Cast<ProcessModule>().Any(m => m.ModuleName.Equals(dllName, StringComparison.OrdinalIgnoreCase)))
        {
            Logger.Info($"Hook DLL already loaded in process {process.Id}, connecting to IPC");
            var existingIpc = await ShufflerHookIpc.ConnectAsync(process, cancellationToken);
            await existingIpc.EnableAsync(cancellationToken);
            Logger.Info($"Successfully connected to existing hook for process {process.Id}");
            return new ShufflerHook(process, existingIpc);
        }

        Logger.Info($"Hook DLL not found in process {process.Id}, proceeding with injection");

        // Check architecture and launch appropriate injector
        var injectorPath = Path.Combine(
            Path.GetDirectoryName(typeof(ShufflerHook).Assembly.Location)!,
            is64Bit ? "Shuffler.Hook.Injector64.exe" : "Shuffler.Hook.Injector.exe"
        );

        Logger.Info($"Using {(is64Bit ? "64-bit" : "32-bit")} injector");

        if (!File.Exists(injectorPath))
            throw new ShufflerHookException($"Injector not found: {injectorPath}");

        var injectorStartInfo = new ProcessStartInfo
        {
            FileName = injectorPath,
            Arguments = process.Id.ToString(),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var injector = Process.Start(injectorStartInfo);
        if (injector == null)
            throw new ShufflerHookException("Failed to start injector process");

        // Wait for the injector to complete
        await injector.WaitForExitAsync(cancellationToken);
        if (injector.ExitCode != 0)
        {
            var error = await injector.StandardError.ReadToEndAsync(cancellationToken);
            throw new ShufflerHookException($"Injector failed with exit code {injector.ExitCode}: {error}");
        }

        Logger.Info($"Connecting to IPC for process {process.Id}");
        var ipc = await ShufflerHookIpc.ConnectAsync(process, cancellationToken);
        await ipc.EnableAsync(cancellationToken);

        Logger.Info($"Successfully hooked process {process.Id}");
        return new ShufflerHook(process, ipc);
    }

    public async ValueTask DisposeAsync()
    {
        if (_ipc is IAsyncDisposable asyncDisposable)
            await asyncDisposable.DisposeAsync();
        else
            await _ipc.DisposeAsync();
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

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

    private const uint CREATE_SUSPENDED = 0x00000004;

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
}