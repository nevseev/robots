using MartianRobots.Abstractions.Services;

namespace MartianRobots.Core.Services;

/// <summary>
/// Standard implementation of IDelayService that uses Task.Delay
/// </summary>
public sealed class DelayService : IDelayService
{
    /// <inheritdoc />
    public async Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken = default)
    {
        await Task.Delay(delay, cancellationToken);
    }

    /// <inheritdoc />
    public async Task DelayAsync(int millisecondsDelay, CancellationToken cancellationToken = default)
    {
        await Task.Delay(millisecondsDelay, cancellationToken);
    }
}