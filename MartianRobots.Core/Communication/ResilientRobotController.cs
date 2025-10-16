using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MartianRobots.Core.Communication;

/// <summary>
/// A resilient wrapper around IRobotCommunicationService that adds retry capabilities
/// This controller provides additional resilience for critical robot operations
/// </summary>
public class ResilientRobotController : IResilientRobotController
{
    private readonly IRobotCommunicationService _communicationService;
    private readonly ILogger<ResilientRobotController> _logger;
    private readonly RobotCommunicationOptions _options;
    private bool _disposed;

    public ResilientRobotController(
        IRobotCommunicationService communicationService,
        ILogger<ResilientRobotController> logger,
        IOptions<RobotCommunicationOptions> options)
    {
        _communicationService = communicationService ?? throw new ArgumentNullException(nameof(communicationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Executes an operation with retry logic
    /// </summary>
    private async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, string operationName, CancellationToken cancellationToken = default)
    {
        var maxAttempts = _options.MaxRetryAttempts + 1; // +1 for initial attempt
        Exception lastException = null!;

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                _logger.LogDebug("Executing {OperationName}, attempt {Attempt}/{MaxAttempts}", operationName, attempt, maxAttempts);
                
                var result = await operation();
                
                if (attempt > 1)
                {
                    _logger.LogInformation("Successfully executed {OperationName} on attempt {Attempt}", operationName, attempt);
                }
                
                return result;
            }
            catch (Exception ex) when (attempt < maxAttempts && 
                                        ex is not TimeoutException && 
                                        ex is not OperationCanceledException &&
                                        ex is not RobotCommandException)
            {
                lastException = ex;
                var delay = TimeSpan.FromMilliseconds(Math.Pow(2, attempt - 1) * 1000); // Exponential backoff
                
                _logger.LogWarning("Attempt {Attempt} failed for {OperationName}, retrying in {Delay}ms: {Exception}",
                    attempt, operationName, delay.TotalMilliseconds, ex.Message);
                
                await Task.Delay(delay, cancellationToken);
            }
            catch (Exception ex)
            {
                lastException = ex;
                break;
            }
        }

        _logger.LogError("All {MaxAttempts} attempts failed for {OperationName}: {Exception}",
            maxAttempts, operationName, lastException?.Message);
        
        throw lastException ?? new InvalidOperationException($"Operation {operationName} failed without exception details");
    }

    /// <summary>
    /// Establishes resilient connection to a robot
    /// </summary>
    public async Task<bool> ConnectRobotAsync(string robotId, Position initialPosition, Orientation initialOrientation, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            _logger.LogDebug("Attempting resilient connection to robot {RobotId}", robotId);

            var connected = await _communicationService.ConnectToRobotAsync(robotId, initialPosition, initialOrientation, cancellationToken);
            
            if (!connected)
            {
                throw new RobotConnectionException($"Failed to connect to robot {robotId}");
            }

            _logger.LogInformation("Successfully established resilient connection to robot {RobotId}", robotId);
            return connected;
        }, $"ConnectRobot({robotId})", cancellationToken);
    }

    /// <summary>
    /// Sends a command to a robot with full resilience (retry)
    /// </summary>
    public async Task<CommandResponse> SendCommandWithResilienceAsync(string robotId, char instruction, CancellationToken cancellationToken = default)
    {
        try
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                _logger.LogDebug("Sending resilient command {Instruction} to robot {RobotId}",
                    instruction, robotId);

                var response = await _communicationService.SendCommandAsync(robotId, instruction, cancellationToken);

                // Ensure response is not null
                if (response == null)
                {
                    throw new RobotCommandException($"Received null response from robot {robotId} for command {instruction}");
                }

                // Check if we should treat this as a failure for resilience purposes
                if (response.Status == CommandStatus.Failed)
                {
                    throw new RobotCommandException($"Robot {robotId} failed to execute command {instruction}: {response.ErrorMessage}");
                }

                if (response.Status == CommandStatus.TimedOut)
                {
                    throw new TimeoutException($"Command {instruction} to robot {robotId} timed out");
                }

                _logger.LogDebug("Successfully executed resilient command {Instruction} on robot {RobotId}", 
                    instruction, robotId);

                return response;
            }, $"SendCommand({robotId}, {instruction})", cancellationToken);
        }
        catch (Exception ex) when (ex is not RobotCommandException and not TimeoutException and not OperationCanceledException)
        {
            // Wrap any other exceptions in RobotCommandException for consistent handling
            throw new RobotCommandException($"Unexpected error executing command {instruction} on robot {robotId}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Executes a sequence of instructions on a robot with resilience
    /// </summary>
    public async Task<List<CommandResponse>> ExecuteInstructionSequenceAsync(
        string robotId, 
        string instructions, 
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(robotId);
        ArgumentException.ThrowIfNullOrEmpty(instructions);

        _logger.LogInformation("Executing instruction sequence '{Instructions}' on robot {RobotId}", 
            instructions, robotId);

        var responses = new List<CommandResponse>();
        
        foreach (var instruction in instructions)
        {
            try
            {
                var response = await SendCommandWithResilienceAsync(robotId, instruction, cancellationToken);
                responses.Add(response);

                // Stop if robot is lost
                if (response.IsLost)
                {
                    _logger.LogWarning("Robot {RobotId} was lost during instruction sequence, stopping execution", robotId);
                    break;
                }

                // Add small delay between commands to avoid overwhelming the robot
                await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
            }
            catch (RobotCommandException ex)
            {
                _logger.LogError(ex, "Failed to execute instruction {Instruction} on robot {RobotId}", 
                    instruction, robotId);
                
                responses.Add(new CommandResponse
                {
                    CommandId = Guid.NewGuid().ToString(),
                    RobotId = robotId,
                    Status = CommandStatus.Failed,
                    ErrorMessage = ex.Message,
                    ResponseTime = DateTime.UtcNow
                });
                break; // Stop execution on failure
            }
            catch (TimeoutException ex)
            {
                _logger.LogError(ex, "Timeout executing instruction {Instruction} on robot {RobotId}", 
                    instruction, robotId);
                
                responses.Add(new CommandResponse
                {
                    CommandId = Guid.NewGuid().ToString(),
                    RobotId = robotId,
                    Status = CommandStatus.TimedOut,
                    ErrorMessage = ex.Message,
                    ResponseTime = DateTime.UtcNow
                });
                break; // Stop execution on timeout
            }
        }

        _logger.LogInformation("Completed instruction sequence on robot {RobotId}: {ExecutedCount}/{TotalCount} commands", 
            robotId, responses.Count(r => r.Status == CommandStatus.Executed), instructions.Length);

        return responses;
    }

    /// <summary>
    /// Gets robot state with resilience
    /// </summary>
    public async Task<RobotInstance?> GetRobotStateAsync(string robotId, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            var robot = await _communicationService.GetRobotStateAsync(robotId, cancellationToken);
            
            if (robot == null)
            {
                throw new RobotNotFoundException($"Robot {robotId} not found");
            }

            return robot;
        }, $"GetRobotState({robotId})", cancellationToken);
    }

    /// <summary>
    /// Performs health check on a robot with resilience
    /// </summary>
    public async Task<bool> HealthCheckRobotAsync(string robotId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                return await _communicationService.PingRobotAsync(robotId, cancellationToken);
            }, $"HealthCheck({robotId})", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Health check failed for robot {RobotId}", robotId);
            return false;
        }
    }

    /// <summary>
    /// Gracefully disconnects from a robot
    /// </summary>
    public async Task<bool> DisconnectRobotAsync(string robotId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _communicationService.DisconnectFromRobotAsync(robotId, cancellationToken);
            _logger.LogInformation("Successfully disconnected from robot {RobotId}", robotId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cleanly disconnect from robot {RobotId}", robotId);
            return false;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            if (_communicationService is IDisposable disposable)
            {
                disposable.Dispose();
            }
            _disposed = true;
        }
    }
}

/// <summary>
/// Exception thrown when robot connection fails
/// </summary>
public sealed class RobotConnectionException : Exception
{
    public RobotConnectionException(string message) : base(message) { }
    public RobotConnectionException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when robot command execution fails
/// </summary>
public sealed class RobotCommandException : Exception
{
    public RobotCommandException(string message) : base(message) { }
    public RobotCommandException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when robot is not found
/// </summary>
public sealed class RobotNotFoundException : Exception
{
    public RobotNotFoundException(string message) : base(message) { }
    public RobotNotFoundException(string message, Exception innerException) : base(message, innerException) { }
}