using System.Diagnostics;
using System.IO.Pipes;
using System.Runtime.InteropServices;

namespace Shuffler.Core.Hook;

public class IpcException : Exception
{
    public IpcException(string message) : base(message) { }
    public IpcException(string message, Exception inner) : base(message, inner) { }
}

public enum ShufflerHookIpcMessageType
{
    Enable,
    Disable,
    SetActiveController
}

public struct ShufflerHookIpcMessage
{
    public ShufflerHookIpcMessageType Type;
    public int ControllerId;
}

public class ShufflerHookIpc : IAsyncDisposable, IDisposable
{
    public bool IsConnected => _pipeClient.IsConnected;

    private readonly Process _process;
    private readonly NamedPipeClientStream _pipeClient;

    private ShufflerHookIpc(Process process, NamedPipeClientStream pipeClient)
    {
        _process = process;
        _pipeClient = pipeClient;
    }

    public static async Task<ShufflerHookIpc> ConnectAsync(Process process, CancellationToken cancellationToken = default)
    {
        var pipeName = $"ShufflerHook-{process.Id}";
        var pipeClient = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut);

        try
        {
            await pipeClient.ConnectAsync(5000, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new IpcException($"Failed to connect to IPC pipe: {ex}");
        }

        return new ShufflerHookIpc(process, pipeClient);
    }

    public Task EnableAsync(CancellationToken cancellationToken = default)
    {
        return SendIpcMessageAsync(new ShufflerHookIpcMessage
        {
            Type = ShufflerHookIpcMessageType.Enable
        }, cancellationToken);
    }

    public Task DisableAsync(CancellationToken cancellationToken = default)
    {
        return SendIpcMessageAsync(new ShufflerHookIpcMessage
        {
            Type = ShufflerHookIpcMessageType.Disable
        }, cancellationToken);
    }

    public Task SetActiveControllerAsync(int controllerId, CancellationToken cancellationToken = default)
    {
        return SendIpcMessageAsync(new ShufflerHookIpcMessage
        {
            Type = ShufflerHookIpcMessageType.SetActiveController,
            ControllerId = controllerId
        }, cancellationToken);
    }

    private async Task SendIpcMessageAsync(ShufflerHookIpcMessage msg, CancellationToken cancellationToken)
    {
        if (!_pipeClient.IsConnected)
        {
            try
            {
                await _pipeClient.ConnectAsync(5000, cancellationToken);
            }
            catch (Exception ex)
            {
                throw new IpcException("Failed to reconnect to IPC pipe", ex);
            }
        }

        try
        {
            // Convert the message to bytes
            var size = Marshal.SizeOf<ShufflerHookIpcMessage>();
            var bytes = new byte[size];
            var ptr = Marshal.AllocHGlobal(size);

            try
            {
                Marshal.StructureToPtr(msg, ptr, false);
                Marshal.Copy(ptr, bytes, 0, size);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }

            try
            {
                await _pipeClient.WriteAsync(bytes, cancellationToken);
                await _pipeClient.FlushAsync(cancellationToken);
            }
            catch (Exception)
            {
                // First failure - try to reconnect and send again
                await _pipeClient.ConnectAsync(5000, cancellationToken);
                await _pipeClient.WriteAsync(bytes, cancellationToken);
                await _pipeClient.FlushAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            throw new IpcException($"Failed to send IPC message after retry", ex);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _pipeClient.DisposeAsync();
        _process.Dispose();
    }

    public void Dispose()
    {
        _pipeClient.Dispose();
        _process.Dispose();
    }
}