namespace GlamourSaver.Services;

// Work must not marshal to the game thread: Dispose drains it on that thread.
public sealed class OwnedOperation : IDisposable
{
    private readonly object gate = new();
    private readonly CancellationTokenSource lifetime = new();
    private Task running = Task.CompletedTask;
    private bool stopped;
    public bool Busy { get { lock (gate) return !running.IsCompleted; } }
    public bool Start(Func<CancellationToken, Task> work)
    {
        lock (gate)
        {
            if (stopped || !running.IsCompleted) return false;
            running = Task.Run(() => work(lifetime.Token));
            return true;
        }
    }
    public void Dispose()
    {
        Task task;
        lock (gate)
        {
            if (stopped) return;
            stopped = true;
            lifetime.Cancel();
            task = running;
        }
        try { task.GetAwaiter().GetResult(); }
        catch (OperationCanceledException) { }
        finally { lifetime.Dispose(); }
    }
}
