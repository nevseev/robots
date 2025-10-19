namespace MartianRobots.Abstractions.Services;

/// <summary>
/// Interface for delay operations to enable testability by abstracting Task.Delay
/// </summary>
public interface IDelayService
{
    /// <summary>
    /// Creates a delay for the specified time span
    /// </summary>
    /// <param name="delay">The amount of time to delay</param>
    /// <param name="cancellationToken">Token to observe for cancellation</param>
    /// <returns>A task that completes after the specified delay</returns>
    Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken = default);
}