using MartianRobots.Abstractions.Models;

namespace MartianRobots.Core.Communication;

/// <summary>
/// Interface for a resilient robot controller with circuit breaker and retry capabilities
/// </summary>
public interface IResilientRobotController
{
    /// <summary>
    /// Connects to a robot with resilience patterns
    /// </summary>
    Task<bool> ConnectRobotAsync(string robotId, Position initialPosition, Orientation initialOrientation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Disconnects from a robot with resilience patterns
    /// </summary>
    Task<bool> DisconnectRobotAsync(string robotId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a command to a robot with resilience patterns
    /// </summary>
    Task<CommandResponse> SendCommandWithResilienceAsync(string robotId, char instruction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a sequence of instructions on a robot with resilience patterns
    /// </summary>
    Task<List<CommandResponse>> ExecuteInstructionSequenceAsync(string robotId, string instructions, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current state of a robot with resilience patterns
    /// </summary>
    Task<RobotInstance?> GetRobotStateAsync(string robotId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a health check on a robot with resilience patterns
    /// </summary>
    Task<bool> HealthCheckRobotAsync(string robotId, CancellationToken cancellationToken = default);
}