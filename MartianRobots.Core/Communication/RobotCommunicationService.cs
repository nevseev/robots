using Microsoft.Extensions.Logging;

namespace MartianRobots.Core.Communication;

/// <summary>
/// Interface for communicating with robot instances
/// </summary>
public interface IRobotCommunicationService
{
    /// <summary>
    /// Establishes connection with a robot
    /// </summary>
    Task<bool> ConnectToRobotAsync(string robotId, Position initialPosition, Orientation initialOrientation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Disconnects from a robot
    /// </summary>
    Task DisconnectFromRobotAsync(string robotId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a command to a robot and waits for acknowledgment
    /// </summary>
    Task<CommandResponse> SendCommandAsync(string robotId, char instruction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current state of a robot
    /// </summary>
    Task<RobotInstance?> GetRobotStateAsync(string robotId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all connected robots
    /// </summary>
    IEnumerable<RobotInstance> GetConnectedRobots();

    /// <summary>
    /// Checks if a robot is connected and responsive
    /// </summary>
    Task<bool> PingRobotAsync(string robotId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Service for managing robot connections and command execution with resilience
/// </summary>
public sealed class RobotCommunicationService : IRobotCommunicationService, IDisposable
{
    private readonly Dictionary<string, RobotInstance> _robots = new();
    private readonly Random _random = new();
    private readonly ILogger<RobotCommunicationService> _logger;
    private readonly RobotCommunicationOptions _options;
    private readonly IDelayService _delayService;
    private readonly object _lock = new();
    private bool _disposed;

    public RobotCommunicationService(
        ILogger<RobotCommunicationService> logger,
        RobotCommunicationOptions options,
        IDelayService delayService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _delayService = delayService ?? throw new ArgumentNullException(nameof(delayService));
    }

    public async Task<bool> ConnectToRobotAsync(string robotId, Position initialPosition, Orientation initialOrientation, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(robotId);
        
        _logger.LogInformation("Attempting to connect to robot {RobotId} at position {Position} facing {Orientation}",
            robotId, initialPosition, initialOrientation);

        try
        {
            // Simulate connection delay
            await SimulateNetworkDelayAsync(cancellationToken);

            // Simulate connection failure
            if (ShouldSimulateFailure())
            {
                _logger.LogWarning("Simulated connection failure for robot {RobotId}", robotId);
                return false;
            }

            lock (_lock)
            {
                var robot = new RobotInstance
                {
                    Id = robotId,
                    Position = initialPosition,
                    Orientation = initialOrientation,
                    IsLost = false,
                    LastCommunication = DateTime.UtcNow,
                    ConnectionState = ConnectionState.Connected,
                    FailedCommandCount = 0
                };

                _robots[robotId] = robot;
            }

            _logger.LogInformation("Successfully connected to robot {RobotId}", robotId);
            return true;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Connection to robot {RobotId} was cancelled", robotId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to robot {RobotId}: {Error}", robotId, ex.Message);
            return false;
        }
    }

    public async Task DisconnectFromRobotAsync(string robotId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(robotId);

        _logger.LogInformation("Disconnecting from robot {RobotId}", robotId);

        // Simulate disconnection delay
        await SimulateNetworkDelayAsync(cancellationToken);

        lock (_lock)
        {
            if (_robots.TryGetValue(robotId, out var robot))
            {
                robot.ConnectionState = ConnectionState.Disconnected;
                _robots.Remove(robotId);
                _logger.LogInformation("Successfully disconnected from robot {RobotId}", robotId);
            }
            else
            {
                _logger.LogWarning("Robot {RobotId} was not connected", robotId);
            }
        }
    }

    public async Task<CommandResponse> SendCommandAsync(string robotId, char instruction, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(robotId);

        var commandId = Guid.NewGuid().ToString();
        var startTime = DateTime.UtcNow;

        _logger.LogDebug("Sending command {Instruction} to robot {RobotId} (CommandId: {CommandId})",
            instruction, robotId, commandId);

        RobotInstance? robot;
        lock (_lock)
        {
            if (!_robots.TryGetValue(robotId, out robot))
            {
                _logger.LogError("Robot {RobotId} is not connected", robotId);
                return new CommandResponse
                {
                    CommandId = commandId,
                    RobotId = robotId,
                    Status = CommandStatus.Failed,
                    ErrorMessage = "Robot not connected",
                    ResponseTime = DateTime.UtcNow,
                    ProcessingTime = DateTime.UtcNow - startTime
                };
            }

            if (robot.ConnectionState != ConnectionState.Connected)
            {
                _logger.LogWarning("Robot {RobotId} is in state {State}, cannot send command", 
                    robotId, robot.ConnectionState);
                return new CommandResponse
                {
                    CommandId = commandId,
                    RobotId = robotId,
                    Status = CommandStatus.Failed,
                    ErrorMessage = $"Robot in invalid state: {robot.ConnectionState}",
                    ResponseTime = DateTime.UtcNow,
                    ProcessingTime = DateTime.UtcNow - startTime
                };
            }
        }

        try
        {
            // Simulate network delay for command transmission
            await SimulateNetworkDelayAsync(cancellationToken);

            // Simulate command failure
            if (ShouldSimulateFailure())
            {
                lock (_lock)
                {
                    robot.FailedCommandCount++;
                    if (robot.FailedCommandCount >= 3)
                    {
                        robot.ConnectionState = ConnectionState.Unstable;
                    }
                }

                _logger.LogWarning("Simulated command failure for robot {RobotId}, command {Instruction}",
                    robotId, instruction);

                return new CommandResponse
                {
                    CommandId = commandId,
                    RobotId = robotId,
                    Status = CommandStatus.Failed,
                    ErrorMessage = "Simulated communication failure",
                    ResponseTime = DateTime.UtcNow,
                    ProcessingTime = DateTime.UtcNow - startTime
                };
            }

            // Execute the command on the robot
            var result = await ExecuteCommandOnRobotAsync(robot, instruction, cancellationToken);

            lock (_lock)
            {
                // Reset failure count on success
                robot.FailedCommandCount = 0;
                robot.ConnectionState = ConnectionState.Connected;
                robot.LastCommunication = DateTime.UtcNow;

                // Update robot state
                if (result.NewPosition.HasValue)
                    robot.Position = result.NewPosition.Value;
                if (result.NewOrientation.HasValue)
                    robot.Orientation = result.NewOrientation.Value;
                robot.IsLost = result.IsLost;
            }

            _logger.LogDebug("Command {Instruction} executed successfully on robot {RobotId}", 
                instruction, robotId);

            return new CommandResponse
            {
                CommandId = commandId,
                RobotId = robotId,
                Status = CommandStatus.Executed,
                NewPosition = result.NewPosition,
                NewOrientation = result.NewOrientation,
                IsLost = result.IsLost,
                ResponseTime = DateTime.UtcNow,
                ProcessingTime = DateTime.UtcNow - startTime
            };
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Command {Instruction} to robot {RobotId} was cancelled", instruction, robotId);
            return new CommandResponse
            {
                CommandId = commandId,
                RobotId = robotId,
                Status = CommandStatus.TimedOut,
                ErrorMessage = "Command was cancelled",
                ResponseTime = DateTime.UtcNow,
                ProcessingTime = DateTime.UtcNow - startTime
            };
        }
        catch (Exception ex)
        {
            lock (_lock)
            {
                robot.FailedCommandCount++;
                robot.LastError = ex.Message;
            }

            _logger.LogError(ex, "Failed to execute command {Instruction} on robot {RobotId}: {Error}",
                instruction, robotId, ex.Message);

            return new CommandResponse
            {
                CommandId = commandId,
                RobotId = robotId,
                Status = CommandStatus.Failed,
                ErrorMessage = ex.Message,
                ResponseTime = DateTime.UtcNow,
                ProcessingTime = DateTime.UtcNow - startTime
            };
        }
    }

    public Task<RobotInstance?> GetRobotStateAsync(string robotId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(robotId);

        lock (_lock)
        {
            _robots.TryGetValue(robotId, out var robot);
            return Task.FromResult(robot);
        }
    }

    public IEnumerable<RobotInstance> GetConnectedRobots()
    {
        lock (_lock)
        {
            return _robots.Values.Where(r => r.ConnectionState == ConnectionState.Connected).ToList();
        }
    }

    public async Task<bool> PingRobotAsync(string robotId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(robotId);

        try
        {
            await SimulateNetworkDelayAsync(cancellationToken);

            lock (_lock)
            {
                if (_robots.TryGetValue(robotId, out var robot))
                {
                    robot.LastCommunication = DateTime.UtcNow;
                    return robot.ConnectionState == ConnectionState.Connected;
                }
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private async Task<(Position? NewPosition, Orientation? NewOrientation, bool IsLost)> ExecuteCommandOnRobotAsync(
        RobotInstance robot, char instruction, CancellationToken cancellationToken)
    {
        // Simulate command processing delay
        await SimulateNetworkDelayAsync(cancellationToken);

        switch (instruction)
        {
            case 'L':
                var newOrientationLeft = robot.Orientation.TurnLeft();
                return (robot.Position, newOrientationLeft, robot.IsLost);

            case 'R':
                var newOrientationRight = robot.Orientation.TurnRight();
                return (robot.Position, newOrientationRight, robot.IsLost);

            case 'F':
                if (robot.IsLost)
                    return (robot.Position, robot.Orientation, true);

                var delta = robot.Orientation.GetMovementDelta();
                var newPosition = new Position(robot.Position.X + delta.X, robot.Position.Y + delta.Y);
                
                // For simulation, use default 5x5 grid (0,0 to 4,4) for boundary checking
                // This simulates the robot detecting it would fall off the edge
                var grid = new MarsGrid(4, 4); // 5x5 grid (0-4, 0-4)
                
                if (!grid.IsValidPosition(newPosition))
                {
                    // Robot falls off the grid and becomes lost
                    return (robot.Position, robot.Orientation, true);
                }
                
                return (newPosition, robot.Orientation, false);

            default:
                throw new ArgumentException($"Invalid instruction: {instruction}");
        }
    }

    private async Task SimulateNetworkDelayAsync(CancellationToken cancellationToken)
    {
        var baseDelay = _options.BaseDelay;
        var randomDelay = TimeSpan.FromMilliseconds(_random.NextDouble() * _options.MaxRandomDelay.TotalMilliseconds);
        var totalDelay = baseDelay + randomDelay;

        _logger.LogTrace("Simulating network delay of {Delay}ms", totalDelay.TotalMilliseconds);
        
        await _delayService.DelayAsync(totalDelay, cancellationToken);
    }

    private bool ShouldSimulateFailure()
    {
        return _random.NextDouble() < _options.FailureProbability;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _logger.LogInformation("Disposing robot communication service, disconnecting {Count} robots", _robots.Count);
            
            foreach (var robotId in _robots.Keys.ToList())
            {
                try
                {
                    DisconnectFromRobotAsync(robotId).Wait(TimeSpan.FromSeconds(5));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to disconnect robot {RobotId} during disposal", robotId);
                }
            }

            _disposed = true;
        }
    }
}