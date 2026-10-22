using System.Text.Json;
using GlamourSaver.Services;

var directory = Path.GetFullPath(Path.Combine("PersistenceSmoke", ".scratch-" + Guid.NewGuid().ToString("N")));
Directory.CreateDirectory(directory);
var checks = 0;
void Check(bool condition, string label)
{
    if (!condition) throw new Exception(label);
    checks++;
}
string FileFor(string name) => Path.Combine(directory, name + ".json");
TestConfig Read(string path) => JsonSerializer.Deserialize<TestConfig>(File.ReadAllText(path))!;
try
{
    double now = 0;
    var path = FileFor("normal");
    using (var store = new ConfigurationStore<TestConfig>(path, () => now))
    {
        for (var i = 0; i < 600; i++) { store.Value.Number = i; store.MarkChanged(); store.Tick(); }
        Check(store.Saves == 0, "UI events do not write");
        now = 60; store.Tick();
        Check(store.Saves == 1 && Read(path).Number == 599, "one save per interval: " + store.Notice + " " + store.FailureKind);
        var size = new FileInfo(path).Length;
        Check(store.LogicalBytes == size, "logical byte counter");
        for (var i = 0; i < 480; i++) { now += 60; store.Tick(); }
        Check(store.Saves == 1 && store.LogicalBytes == size, "8 idle hours write zero");
        store.Value.Number = 4; store.MarkChanged(); store.Value.Number = 599; store.MarkChanged();
        now += 60; store.Tick();
        Check(store.Saves == 1, "revert is unchanged");
        store.Value.Number = 600; store.MarkChanged(); store.Flush(); store.Flush();
        Check(store.Saves == 2, "logout and duplicate flush");
        store.Value.Number = 601; store.MarkChanged(); store.Dispose();
        Check(store.Saves == 3 && Read(path).Number == 601, "Dispose flush");
        var bytes = store.LogicalBytes;
        store.Value.Number = 602; store.MarkChanged(); now += 600; store.Tick(); store.Flush();
        Check(store.LogicalBytes == bytes, "nothing after Dispose");
        Console.WriteLine($"600 edits: 1 commit/{size} logical bytes; 480 idle periods: 0 commits/0 bytes; logout+shutdown total: {store.Saves} commits/{bytes} bytes.");
    }
    using (var store = new ConfigurationStore<TestConfig>(path, () => now))
    {
        for (var i = 1; i <= 5; i++) { store.Value.Number = i; store.MarkChanged(); store.Flush(); }
        var backups = Directory.GetFiles(path + ".backups").OrderDescending().ToArray();
        Check(backups.Length == 3 && backups.Select(p => Read(p).Number).SequenceEqual(new[] { 4, 3, 2 }), "three previous generations");
    }
    File.WriteAllText(path, "{broken");
    using (var recovered = new ConfigurationStore<TestConfig>(path))
    {
        Check(recovered.Value.Number == 4 && Read(path).Number == 4 && recovered.Notice != null, "restore newest backup");
        Check(Directory.GetFiles(directory, "normal.json.corrupt-*").Length == 1, "retain corrupt main");
    }
    File.WriteAllText(Directory.GetFiles(path + ".backups").OrderDescending().First(), "null");
    File.WriteAllText(path, "[]");
    using (var recovered = new ConfigurationStore<TestConfig>(path)) Check(recovered.Value.Number == 3, "skip invalid newest backup");
    using (var healthy = new ConfigurationStore<TestConfig>(path)) Check(healthy.Value.Number == 3 && healthy.Notice == null, "healthy main preferred");
    var snapshots = Directory.GetFiles(path + ".backups").ToDictionary(p => p, File.ReadAllText);
    using (var denied = new ConfigurationStore<TestConfig>(path, fault: stage => { if (stage == "replace") throw new IOException("injected rename failure"); }))
    {
        denied.Value.Number = 90; denied.MarkChanged(); denied.Flush();
        Check(snapshots.All(pair => File.ReadAllText(pair.Key) == pair.Value), "failed replace preserves existing generations");
    }
    File.Delete(path);
    using (var missing = new ConfigurationStore<TestConfig>(path)) Check(missing.Value.Number == 3, "missing main recovered");

    var damaged = FileFor("unrecoverable"); File.WriteAllText(damaged, "{broken");
    using (var store = new ConfigurationStore<TestConfig>(damaged))
    {
        store.Value.Number = 12; store.MarkChanged(); store.Flush(); store.Retry();
        Check(store.Stopped && store.Saves == 0 && File.ReadAllText(damaged) == "{broken", "do not overwrite unrecoverable configuration");
    }

    foreach (var phase in new[] { "write", "flush", "replace" })
    {
        var failurePath = FileFor(phase); now = 0; var fail = false; var attempts = 0;
        using var store = new ConfigurationStore<TestConfig>(failurePath, () => now, stage =>
        {
            if (stage == phase && fail)
            {
                attempts++;
                if (phase == "write") throw new UnauthorizedAccessException("synthetic access denial");
                if (phase == "flush") throw new IOException("synthetic disk full", unchecked((int)0x80070070));
                throw new IOException("synthetic rename failure");
            }
        });
        store.Flush(); var before = File.ReadAllText(failurePath); fail = true;
        store.Value.Number = 9; store.MarkChanged();
        for (var i = 0; i < 10; i++) { now += 60; store.Tick(); }
        Check(attempts == 3 && store.Stopped, phase + " bounded failure");
        Check(File.ReadAllText(failurePath) == before && !Directory.Exists(failurePath + ".backups"), phase + " does not rotate");
        fail = false; store.Retry();
        Check(Read(failurePath).Number == 9 && !store.Stopped, phase + " manual retry");
        Check(Directory.GetFiles(directory, Path.GetFileName(failurePath) + ".tmp-*").Length == 0, phase + " temp cleanup");
        Console.WriteLine($"Fault {phase}: {attempts} failed attempts, {store.Saves} committed saves including seed/retry, {store.LogicalBytes} logical bytes including failed temporaries.");
    }

    var lockedPath = FileFor("locked");
    using (var store = new ConfigurationStore<TestConfig>(lockedPath))
    {
        store.Flush(); store.Value.Number = 7; store.MarkChanged();
        using (var held = new FileStream(lockedPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) store.Flush();
        Check(store.Saves == 1 && store.Notice != null, "real locked file failure");
        store.Retry(); Check(Read(lockedPath).Number == 7, "lock recovery");
    }
    ConfigurationStore<TestConfig>? concurrent = null;
    var mutate = false;
    using (concurrent = new ConfigurationStore<TestConfig>(FileFor("revision"), fault: stage =>
    {
        if (stage == "replace" && mutate)
        { mutate = false; concurrent!.Value.Number = 12; concurrent.MarkChanged(); }
    }))
    {
        concurrent.Flush(); concurrent.Value.Number = 11; concurrent.MarkChanged(); mutate = true;
        concurrent.Flush(); Check(Read(FileFor("revision")).Number == 11, "captured snapshot");
        concurrent.Flush(); Check(Read(FileFor("revision")).Number == 12, "change during save retained");
        concurrent.Value.Number = 13; concurrent.MarkChanged();
        Parallel.For(0, 20, _ => concurrent.Flush());
        Check(concurrent.Saves == 4, "concurrent flushes single writer");
        Parallel.For(0, 20, i => { concurrent.Change(c => c.Number = i); concurrent.Flush(); });
        concurrent.Flush();
        Check(Read(FileFor("revision")).Number == concurrent.Value.Number, "concurrent edits and flush agree");
        concurrent.Dispose();
        var finalValue = concurrent.Value.Number;
        concurrent.Change(c => c.Number = 999);
        Check(concurrent.Value.Number == finalValue, "no edits accepted after Dispose");
    }
    // Monotonic clock crosses a local midnight without affecting the periodic schedule.
    now = 86390;
    using (var store = new ConfigurationStore<TestConfig>(FileFor("midnight"), () => now))
    {
        now = 86410; store.Tick(); Check(store.Saves == 0, "midnight no early write");
        now = 86450; store.Tick(); Check(store.Saves == 1, "midnight correct deadline");
    }
    using (var operation = new OwnedOperation())
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var stopped = false;
        Check(operation.Start(async token =>
        {
            started.SetResult();
            try { await Task.Delay(Timeout.Infinite, token).ConfigureAwait(false); }
            finally { stopped = true; }
        }), "operation accepted");
        await started.Task;
        Check(!operation.Start(_ => Task.CompletedTask), "bounded concurrency");
        operation.Dispose();
        Check(stopped && !operation.Busy && !operation.Start(_ => Task.CompletedTask), "cancel drain stop acceptance");
    }
    Console.WriteLine($"PASS {checks} persistence/lifecycle assertions; temporary synthetic files only.");
}
finally { Directory.Delete(directory, true); }

public sealed class TestConfig
{
    public int Version { get; set; } = 4;
    public int Number { get; set; }
}
