using System.Collections.Concurrent;
using BuggyApp.Models;

namespace BuggyApp.Services;

public sealed class DiagnosticsService
{
    private const int MaximumEntries = 2000;
    private readonly ConcurrentQueue<OperationLogEntry> _entries = new();
    private long _sequence;
    private int _activeRequestCount;

    public int ActiveRequestCount => Volatile.Read(ref _activeRequestCount);

    public void RequestStarted(string operation)
    {
        Interlocked.Increment(ref _activeRequestCount);
        Add(operation, OperationStatus.Started, TimeSpan.Zero);
    }

    public void RequestCompleted(string operation, TimeSpan elapsed)
    {
        Interlocked.Decrement(ref _activeRequestCount);
        Add(operation, OperationStatus.Succeeded, elapsed);
    }

    public void RequestCancelled(string operation, TimeSpan elapsed)
    {
        Interlocked.Decrement(ref _activeRequestCount);
        Add(operation, OperationStatus.Cancelled, elapsed);
    }

    public void RequestFailed(string operation, TimeSpan elapsed, Exception exception)
    {
        Interlocked.Decrement(ref _activeRequestCount);
        Add(operation, OperationStatus.Failed, elapsed, exception.ToString());
    }

    public void RecordNavigation(string route)
    {
        Add("Navigation", OperationStatus.Succeeded, TimeSpan.Zero, route);
    }

    public IReadOnlyList<OperationLogEntry> GetSnapshot()
    {
        return _entries.OrderByDescending(entry => entry.Sequence).ToArray();
    }

    public void Clear()
    {
        _entries.Clear();
    }

    private void Add(
        string operation,
        OperationStatus status,
        TimeSpan elapsed,
        string? detail = null)
    {
        _entries.Enqueue(new OperationLogEntry(
            Interlocked.Increment(ref _sequence),
            DateTimeOffset.UtcNow,
            operation,
            status,
            elapsed,
            detail));

        while (_entries.Count > MaximumEntries)
        {
            _entries.TryDequeue(out _);
        }
    }
}
