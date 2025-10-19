namespace MartianRobots.Abstractions.Services;

/// <summary>
/// Interface for simulating communication failures in robot operations
/// Used for testing resilience and error handling
/// </summary>
public interface IFailureSimulator
{
    /// <summary>
    /// Determines whether a failure should be simulated
    /// </summary>
    /// <returns>True if a failure should be simulated, false otherwise</returns>
    bool ShouldSimulateFailure();
}
