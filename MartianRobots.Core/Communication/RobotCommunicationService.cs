using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

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
    /// Sends a batch of commands to a robot and waits for acknowledgments
    /// More efficient than sending commands one at a time
    /// </summary>
    Task<List<CommandResponse>> SendCommandBatchAsync(string robotId, string instructions, MarsGrid grid, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a robot is connected and responsive
    /// </summary>
    Task<bool> PingRobotAsync(string robotId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Service for managing robot connections and command execution with resilience
/// </summary>
public sealed class RobotCommunicationService(
    ILogger<RobotCommunicationService> logger,
    RobotCommunicationOptions options,
    IDelayService delayService,
    IFailureSimulator failureSimulator) : IRobotCommunicationService, IDisposable
{
    private readonly ConcurrentDictionary<string, RobotInstance> _robots = new();
    private readonly Random _random = new();
    private bool _disposed;

    public async Task<bool> ConnectToRobotAsync(string robotId, Position initialPosition, Orientation initialOrientation, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(robotId);
        
        logger.LogInformation("Attempting to connect to robot {RobotId} at position {Position} facing {Orientation}",
            robotId, initialPosition, initialOrientation);

        try
        {
            // Simulate connection delay
            await SimulateNetworkDelayAsync(cancellationToken);

            // Simulate connection failure
            if (failureSimulator.ShouldSimulateFailure())
            {
                logger.LogWarning("Simulated connection failure for robot {RobotId}", robotId);
                return false;
            }

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

            logger.LogInformation("Successfully connected to robot {RobotId}", robotId);
            return true;
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Connection to robot {RobotId} was cancelled", robotId);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to connect to robot {RobotId}: {Error}", robotId, ex.Message);
            return false;
        }
    }

    public async Task DisconnectFromRobotAsync(string robotId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(robotId);

        logger.LogInformation("Disconnecting from robot {RobotId}", robotId);

        // Simulate disconnection delay
        await SimulateNetworkDelayAsync(cancellationToken);

        if (_robots.TryRemove(robotId, out var robot))
        {
            robot.ConnectionState = ConnectionState.Disconnected;
            logger.LogInformation("Successfully disconnected from robot {RobotId}", robotId);
        }
        else
        {
            logger.LogWarning("Robot {RobotId} was not connected", robotId);
        }
    }

    public async Task<List<CommandResponse>> SendCommandBatchAsync(string robotId, string instructions, MarsGrid grid, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(robotId);
        ArgumentException.ThrowIfNullOrEmpty(instructions);
        ArgumentNullException.ThrowIfNull(grid);

        logger.LogInformation("Sending command batch of {Count} instructions to robot {RobotId}: {Instructions}",
            instructions.Length, robotId, instructions);

        var responses = new List<CommandResponse>();
        
        // Validate all commands first using CommandFactory - will throw if any command is invalid
        var commands = Commands.CommandFactory.CreateCommands(instructions);
        
        logger.LogDebug("Validated {Count} commands for batch execution", commands.Count);

        // Check if robot exists and is connected before starting batch
        if (!_robots.TryGetValue(robotId, out var robot))
        {
            logger.LogError("Robot {RobotId} is not connected", robotId);
            return
            [
                new CommandResponse
                {
                    CommandId = Guid.NewGuid().ToString(),
                    RobotId = robotId,
                    Status = CommandStatus.Failed,
                    ErrorMessage = "Robot not connected",
                    ResponseTime = DateTime.UtcNow
                }
            ];
        }

        // Execute each command directly
        for (int i = 0; i < commands.Count; i++)
        {
            var command = commands[i];
            var instruction = instructions[i];
            var commandId = Guid.NewGuid().ToString();
            var startTime = DateTime.UtcNow;

            try
            {
                // Simulate network delay
                await SimulateNetworkDelayAsync(cancellationToken);

                // Check robot state before each command
                if (robot.ConnectionState != ConnectionState.Connected)
                {
                    logger.LogWarning("Robot {RobotId} is in state {State}, stopping batch execution",
                        robotId, robot.ConnectionState);
                    
                    responses.Add(new CommandResponse
                    {
                        CommandId = commandId,
                        RobotId = robotId,
                        Status = CommandStatus.Failed,
                        ErrorMessage = $"Robot in invalid state: {robot.ConnectionState}",
                        ResponseTime = DateTime.UtcNow,
                        ProcessingTime = DateTime.UtcNow - startTime
                    });
                    break;
                }

                // Simulate command failure
                if (failureSimulator.ShouldSimulateFailure())
                {
                    robot.FailedCommandCount++;
                    if (robot.FailedCommandCount >= 3)
                    {
                        robot.ConnectionState = ConnectionState.Unstable;
                    }

                    logger.LogWarning("Simulated command failure for robot {RobotId}, command {Instruction}",
                        robotId, instruction);

                    responses.Add(new CommandResponse
                    {
                        CommandId = commandId,
                        RobotId = robotId,
                        Status = CommandStatus.Failed,
                        ErrorMessage = "Simulated communication failure",
                        ResponseTime = DateTime.UtcNow,
                        ProcessingTime = DateTime.UtcNow - startTime
                    });
                    continue;
                }

                // Create a Robot model for command execution
                var robotModel = new Robot(robot.Position, robot.Orientation);

                // Execute the command using the Command pattern
                command.Execute(robotModel, grid);

                // Update robot instance state from the robot model
                robot.Position = robotModel.Position;
                robot.Orientation = robotModel.Orientation;
                robot.IsLost = robotModel.IsLost;
                robot.FailedCommandCount = 0;
                robot.ConnectionState = ConnectionState.Connected;
                robot.LastCommunication = DateTime.UtcNow;

                logger.LogDebug("Command {Instruction} executed successfully on robot {RobotId}", 
                    instruction, robotId);

                responses.Add(new CommandResponse
                {
                    CommandId = commandId,
                    RobotId = robotId,
                    Status = CommandStatus.Executed,
                    NewPosition = robot.Position,
                    NewOrientation = robot.Orientation,
                    IsLost = robot.IsLost,
                    ResponseTime = DateTime.UtcNow,
                    ProcessingTime = DateTime.UtcNow - startTime
                });

                // Stop batch execution if robot is lost
                if (robot.IsLost)
                {
                    logger.LogWarning("Robot {RobotId} lost during batch execution, stopping after {ExecutedCount}/{TotalCount} commands",
                        robotId, responses.Count, instructions.Length);
                    break;
                }
            }
            catch (OperationCanceledException)
            {
                logger.LogWarning("Batch execution cancelled for robot {RobotId} after {ExecutedCount} commands",
                    robotId, responses.Count);
                
                responses.Add(new CommandResponse
                {
                    CommandId = commandId,
                    RobotId = robotId,
                    Status = CommandStatus.TimedOut,
                    ErrorMessage = "Batch execution was cancelled",
                    ResponseTime = DateTime.UtcNow,
                    ProcessingTime = DateTime.UtcNow - startTime
                });
                break;
            }
            catch (Exception ex)
            {
                robot.FailedCommandCount++;
                robot.LastError = ex.Message;

                logger.LogError(ex, "Failed to execute command {Instruction} on robot {RobotId}: {Error}",
                    instruction, robotId, ex.Message);

                responses.Add(new CommandResponse
                {
                    CommandId = commandId,
                    RobotId = robotId,
                    Status = CommandStatus.Failed,
                    ErrorMessage = ex.Message,
                    ResponseTime = DateTime.UtcNow,
                    ProcessingTime = DateTime.UtcNow - startTime
                });
            }
        }

        var successCount = responses.Count(r => r.Status == CommandStatus.Executed);
        logger.LogInformation("Batch execution completed for robot {RobotId}: {SuccessCount}/{TotalCount} commands executed successfully",
            robotId, successCount, responses.Count);

        return responses;
    }

    public async Task<bool> PingRobotAsync(string robotId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(robotId);

        try
        {
            await SimulateNetworkDelayAsync(cancellationToken);

            if (_robots.TryGetValue(robotId, out var robot))
            {
                robot.LastCommunication = DateTime.UtcNow;
                return robot.ConnectionState == ConnectionState.Connected;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private async Task SimulateNetworkDelayAsync(CancellationToken cancellationToken)
    {
        var baseDelay = options.BaseDelay;
        var randomDelay = TimeSpan.FromMilliseconds(_random.NextDouble() * options.MaxRandomDelay.TotalMilliseconds);
        var totalDelay = baseDelay + randomDelay;

        logger.LogTrace("Simulating network delay of {Delay}ms", totalDelay.TotalMilliseconds);
        
        await delayService.DelayAsync(totalDelay, cancellationToken);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        
        logger.LogInformation("Disposing robot communication service, disconnecting {Count} robots", _robots.Count);

        // Synchronously disconnect all robots by updating their state
        foreach (var (robotId, robot) in _robots)
        {
            robot.ConnectionState = ConnectionState.Disconnected;
            logger.LogDebug("Marked robot {RobotId} as disconnected during disposal", robotId);
        }

        _robots.Clear();
        _disposed = true;
    }
}