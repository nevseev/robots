namespace MartianRobots.Abstractions.Models;

/// <summary>
/// Represents a robot instance that can be communicated with
/// </summary>
public sealed class RobotInstance
{
    public string Id { get; init; } = string.Empty;
    public Position Position { get; set; }
    public Orientation Orientation { get; set; }
    public bool IsLost { get; set; }
    public DateTime LastCommunication { get; set; }
    public ConnectionState ConnectionState { get; set; } = ConnectionState.Disconnected;
    public string? LastError { get; set; }
    public int FailedCommandCount { get; set; }
}

/// <summary>
/// Connection state of a robot
/// </summary>
public enum ConnectionState
{
    Disconnected,
    Connecting,
    Connected,
    Unstable,
    Lost
}

/// <summary>
/// Status of a command sent to a robot
/// </summary>
public enum CommandStatus
{
    Executed,
    Failed,
    TimedOut
}

/// <summary>
/// Response from a robot command
/// </summary>
public sealed class CommandResponse
{
    public string CommandId { get; init; } = string.Empty;
    public string RobotId { get; init; } = string.Empty;
    public CommandStatus Status { get; init; }
    public Position? NewPosition { get; init; }
    public Orientation? NewOrientation { get; init; }
    public bool IsLost { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime ResponseTime { get; init; } = DateTime.UtcNow;
    public TimeSpan ProcessingTime { get; init; }
}

/// <summary>
/// Configuration for robot communication
/// </summary>
public sealed class RobotCommunicationOptions
{
    public const string SectionName = "RobotCommunication";

    /// <summary>
    /// Base delay for simulating Mars communication distance (default: 500ms)
    /// </summary>
    public TimeSpan BaseDelay { get; set; } = TimeSpan.FromMilliseconds(500);

    /// <summary>
    /// Maximum random additional delay (default: 1000ms)
    /// </summary>
    public TimeSpan MaxRandomDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Probability of communication failure (0.0 to 1.0, default: 0.1)
    /// </summary>
    public double FailureProbability { get; set; } = 0.1;

    /// <summary>
    /// Command timeout duration (default: 10 seconds)
    /// </summary>
    public TimeSpan CommandTimeout { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Maximum number of retry attempts (default: 3)
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Circuit breaker failure threshold (default: 5)
    /// </summary>
    public int CircuitBreakerThreshold { get; set; } = 5;

    /// <summary>
    /// Circuit breaker sampling duration (default: 30 seconds)
    /// </summary>
    public TimeSpan CircuitBreakerSamplingDuration { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Circuit breaker minimum throughput (default: 10)
    /// </summary>
    public int CircuitBreakerMinimumThroughput { get; set; } = 10;
}
