using System.Diagnostics;
using System.Text.Json;

namespace GlamourSaver.Services;

// Small configuration only. All public operations are serialized; no timers or workers are owned.
public sealed class ConfigurationStore<T> : IDisposable where T : class, new()
{
    private readonly object gate = new();
    private readonly string path;
    private readonly Func<double> clock;
    private readonly Action<string>? fault;
    private readonly Func<T, bool> validate;
    private readonly JsonSerializerOptions options = new() { WriteIndented = true };
    private string? saved;
    private long revision;
    private long committed;
    private double next;
    private int failures;
    private bool disposed;
    private bool blockedLoad;
    public T Value { get; private set; } = new();
    public string? Notice { get; private set; }
    public string? FailureKind { get; private set; }
    public long Saves { get; private set; }
    public long LogicalBytes { get; private set; }
    public bool Stopped => failures >= 3 || blockedLoad;
    public bool CanRetry => failures > 0 && !blockedLoad;

    public ConfigurationStore(string path, Func<double>? clock = null, Action<string>? fault = null, Func<T, bool>? validate = null)
    {
        this.path = path;
        this.clock = clock ?? (() => (double)Stopwatch.GetTimestamp() / Stopwatch.Frequency);
        this.fault = fault;
        this.validate = validate ?? (_ => true);
        next = this.clock() + 60;
        Load();
    }

    private string BackupDirectory => path + ".backups";
    private string[] Backups() => Directory.Exists(BackupDirectory)
        ? Directory.GetFiles(BackupDirectory, "*.json").OrderDescending(StringComparer.Ordinal).ToArray()
        : [];

    private T Read(string file)
    {
        if (new FileInfo(file).Length > 1024 * 1024) throw new InvalidDataException();
        var json = File.ReadAllText(file);
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind != JsonValueKind.Object
            || !document.RootElement.TryGetProperty("Version", out _)) throw new InvalidDataException();
        var value = JsonSerializer.Deserialize<T>(json, options) ?? throw new InvalidDataException();
        if (!validate(value)) throw new InvalidDataException();
        return value;
    }

    private void Load()
    {
        try
        {
            if (File.Exists(path))
            {
                try { Value = Read(path); saved = JsonSerializer.Serialize(Value, options); return; }
                catch (JsonException) { }
                catch (InvalidDataException) { }
            }
            foreach (var backup in Backups())
            {
                try { Value = Read(backup); }
                catch (JsonException) { continue; }
                catch (InvalidDataException) { continue; }
                var json = JsonSerializer.Serialize(Value, options);
                Commit(json, false);
                Saves++;
                saved = json;
                Notice = "Configuration restored from backup.";
                return;
            }
            if (File.Exists(path) || Backups().Length != 0)
            {
                blockedLoad = true;
                Notice = "Configuration could not be loaded. Restore a valid file and reload the plugin.";
            }
            else revision = 1;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            FailureKind = ex.GetType().Name;
            blockedLoad = true;
            Notice = "Configuration could not be loaded. Restore a valid file and reload the plugin.";
        }
    }

    public void MarkChanged() { lock (gate) { if (!disposed) revision++; } }
    public void Change(Action<T> change)
    {
        lock (gate)
        {
            if (disposed) return;
            change(Value);
            revision++;
        }
    }
    public void Tick()
    {
        lock (gate)
        {
            if (disposed || clock() < next) return;
            next = clock() + 60;
            FlushCore();
        }
    }
    public void Flush() { lock (gate) { if (!disposed) FlushCore(); } }
    public void Retry()
    {
        lock (gate)
        {
            if (disposed || blockedLoad) return;
            failures = 0;
            next = clock() + 60;
            FlushCore();
        }
    }

    private void FlushCore()
    {
        if (Stopped || revision == committed) return;
        var capturedRevision = revision;
        try
        {
            var json = JsonSerializer.Serialize(Value, options);
            if (json != saved)
            {
                Commit(json, true);
                saved = json;
                Saves++;
            }
            committed = capturedRevision;
            failures = 0;
            Notice = null;
            FailureKind = null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            FailureKind = ex.GetType().Name;
            failures++;
            Notice = failures >= 3
                ? "Saving stopped after three failures. Check disk access and press Retry saving."
                : "Configuration save failed. Check disk access; retry at the next interval.";
        }
    }

    private void Commit(string json, bool backupPrevious)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporary = path + ".tmp-" + Guid.NewGuid().ToString("N");
        try
        {
            fault?.Invoke("write");
            var bytes = System.Text.Encoding.UTF8.GetBytes(json);
            using (var file = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                file.Write(bytes);
                LogicalBytes += bytes.Length;
                fault?.Invoke("flush");
                file.Flush(true);
            }
            _ = Read(temporary);
            fault?.Invoke("replace");
            if (File.Exists(path))
            {
                string archive;
                if (backupPrevious)
                {
                    // Never promote a damaged externally modified main file into the valid generations.
                    try { _ = Read(path); }
                    catch (Exception ex) when (ex is JsonException or InvalidDataException) { backupPrevious = false; }
                }
                if (backupPrevious)
                {
                    Directory.CreateDirectory(BackupDirectory);
                    var maximum = Backups().Select(f => long.TryParse(Path.GetFileNameWithoutExtension(f), out var n) ? n : 0).DefaultIfEmpty().Max();
                    archive = Path.Combine(BackupDirectory, (maximum + 1).ToString("D20") + ".json");
                }
                else archive = path + ".corrupt-" + Guid.NewGuid().ToString("N");
                File.Replace(temporary, path, archive);
            }
            else File.Move(temporary, path);
            // Cleanup follows the commit. Cleanup failure must never report the committed save as failed.
            try
            {
                var valid = 0;
                foreach (var old in Backups())
                {
                    try { _ = Read(old); }
                    catch (Exception ex) when (ex is JsonException or InvalidDataException) { continue; }
                    if (++valid > 3) File.Delete(old);
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
        }
        finally
        {
            try { if (File.Exists(temporary)) File.Delete(temporary); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
        }
    }

    public void Dispose()
    {
        lock (gate)
        {
            if (disposed) return;
            FlushCore();
            disposed = true;
        }
    }
}
