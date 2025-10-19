namespace MartianRobots.Core.Communication;

/// <summary>
/// Interface for a resilient robot controller with circuit breaker and retry capabilities
/// </summary>
public interface IResilientRobotController : IDisposable
{
    /// <summary>
    /// Connects to a robot with resilience patterns
    /// </summary>
    Task<bool> ConnectRobotAsync(string robotId, Position initialPosition, Orientation initialOrientation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a sequence of instructions on a robot with resilience patterns
    /// </summary>
    Task<List<CommandResponse>> ExecuteInstructionSequenceAsync(string robotId, string instructions, MarsGrid grid, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a health check on a robot to verify connectivity
    /// </summary>
    Task<bool> HealthCheckRobotAsync(string robotId, CancellationToken cancellationToken = default);
}