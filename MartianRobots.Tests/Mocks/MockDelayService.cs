using MartianRobots.Abstractions.Services;

namespace MartianRobots.Tests.Mocks;

/// <summary>
/// Mock implementation of IDelayService that executes instantly for fast unit tests
/// </summary>
public sealed class MockDelayService : IDelayService
{
    private readonly List<DelayCall> _delayCalls = new();
    private readonly object _lock = new();

    /// <summary>
    /// Gets the list of delay calls made to this service
    /// </summary>
    public IReadOnlyList<DelayCall> DelayCalls
    {
        get
        {
            lock (_lock)
            {
                return _delayCalls.ToList();
            }
        }
    }

    /// <summary>
    /// Clears the recorded delay calls
    /// </summary>
    public void ClearCalls()
    {
        lock (_lock)
        {
            _delayCalls.Clear();
        }
    }

    /// <inheritdoc />
    public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _delayCalls.Add(new DelayCall(delay, cancellationToken.IsCancellationRequested));
        }

        // Return immediately without actual delay for fast tests
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DelayAsync(int millisecondsDelay, CancellationToken cancellationToken = default)
    {
        return DelayAsync(TimeSpan.FromMilliseconds(millisecondsDelay), cancellationToken);
    }
}

/// <summary>
/// Record of a delay call made to MockDelayService
/// </summary>
public record DelayCall(TimeSpan Delay, bool WasCancellationRequested);