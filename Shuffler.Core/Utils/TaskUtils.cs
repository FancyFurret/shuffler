using System.Diagnostics;

namespace Shuffler.Core.Utils;

public static class TaskUtils
{
    private static readonly ILogger Logger = LogManager.Create(nameof(TaskUtils));

    public static async Task WaitForEvent(TaskCompletionSource<bool> tcs, string eventName, int timeoutSeconds)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
        try
        {
            await using (cts.Token.Register(() => tcs.TrySetResult(false)))
            {
                await tcs.Task;
            }
        }
        catch (OperationCanceledException)
        {
            Logger.Error($"{eventName} did not complete within {timeoutSeconds} seconds");
            throw new TimeoutException($"{eventName} did not complete within {timeoutSeconds} seconds");
        }
    }
}