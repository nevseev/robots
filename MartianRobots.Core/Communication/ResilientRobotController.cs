using Microsoft.Extensions.Resilience;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace MartianRobots.Core.Communication;

/// <summary>
/// A resilient wrapper around IRobotCommunicationService that adds circuit breaker and retry capabilities
/// This controller provides additional resilience for critical robot operations
/// </summary>
public class ResilientRobotController : IResilientRobotController
{
    private readonly IRobotCommunicationService _communicationService;
    private readonly ILogger<ResilientRobotController> _logger;
    private readonly RobotCommunicationOptions _options;
    private readonly ResiliencePipeline _connectionPipeline;
    private readonly ResiliencePipeline _commandPipeline;
    private readonly ResiliencePipeline _queryPipeline;
    private bool _disposed;

    public ResilientRobotController(
        IRobotCommunicationService communicationService,
        ILogger<ResilientRobotController> logger,
        IOptions<RobotCommunicationOptions> options)
    {
        _communicationService = communicationService ?? throw new ArgumentNullException(nameof(communicationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));

        // Build resilience pipelines
        _connectionPipeline = BuildConnectionPipeline();
        _commandPipeline = BuildCommandPipeline();
        _queryPipeline = BuildQueryPipeline();
    }

    private ResiliencePipeline BuildConnectionPipeline()
    {
        return new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = _options.MaxRetryAttempts,
                DelayGenerator = static args => new ValueTask<TimeSpan?>(
                    TimeSpan.FromMilliseconds(Math.Pow(2, args.AttemptNumber) * 1000)), // Exponential backoff
                OnRetry = args =>
                {
                    _logger.LogWarning("Connection attempt {AttemptNumber} failed, retrying in {Delay}ms: {Exception}",
                        args.AttemptNumber + 1, args.RetryDelay.TotalMilliseconds, args.Outcome.Exception?.Message);
                    return ValueTask.CompletedTask;
                }
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = 0.5,
                MinimumThroughput = _options.CircuitBreakerMinimumThroughput,
                SamplingDuration = _options.CircuitBreakerSamplingDuration,
                BreakDuration = TimeSpan.FromSeconds(30),
                OnOpened = args =>
                {
                    _logger.LogError("Connection circuit breaker opened: {Exception}", args.Outcome.Exception?.Message);
                    return ValueTask.CompletedTask;
                },
                OnClosed = args =>
                {
                    _logger.LogInformation("Connection circuit breaker closed");
                    return ValueTask.CompletedTask;
                }
            })
            .AddTimeout(_options.CommandTimeout)
            .Build();
    }

    private ResiliencePipeline BuildCommandPipeline()
    {
        return new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = _options.MaxRetryAttempts,
                DelayGenerator = static args => new ValueTask<TimeSpan?>(
                    TimeSpan.FromMilliseconds(Math.Pow(2, args.AttemptNumber) * 500)), // Faster retry for commands
                ShouldHandle = new PredicateBuilder().Handle<Exception>(ex => 
                    ex is not RobotCommandException and not TimeoutException), // Don't retry business logic failures
                OnRetry = args =>
                {
                    _logger.LogWarning("Command attempt {AttemptNumber} failed, retrying in {Delay}ms: {Exception}",
                        args.AttemptNumber + 1, args.RetryDelay.TotalMilliseconds, args.Outcome.Exception?.Message);
                    return ValueTask.CompletedTask;
                }
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = 0.6,
                MinimumThroughput = _options.CircuitBreakerMinimumThroughput,
                SamplingDuration = _options.CircuitBreakerSamplingDuration,
                BreakDuration = TimeSpan.FromSeconds(15),
                OnOpened = args =>
                {
                    _logger.LogError("Command circuit breaker opened: {Exception}", args.Outcome.Exception?.Message);
                    return ValueTask.CompletedTask;
                },
                OnClosed = args =>
                {
                    _logger.LogInformation("Command circuit breaker closed");
                    return ValueTask.CompletedTask;
                }
            })
            .AddTimeout(_options.CommandTimeout)
            .Build();
    }

    private ResiliencePipeline BuildQueryPipeline()
    {
        return new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 2, // Fewer retries for queries
                DelayGenerator = static args => new ValueTask<TimeSpan?>(
                    TimeSpan.FromMilliseconds(args.AttemptNumber * 200)),
                OnRetry = args =>
                {
                    _logger.LogDebug("Query attempt {AttemptNumber} failed, retrying: {Exception}",
                        args.AttemptNumber + 1, args.Outcome.Exception?.Message);
                    return ValueTask.CompletedTask;
                }
            })
            .AddTimeout(TimeSpan.FromSeconds(5)) // Shorter timeout for queries
            .Build();
    }

    /// <summary>
    /// Establishes resilient connection to a robot
    /// </summary>
    public async Task<bool> ConnectRobotAsync(string robotId, Position initialPosition, Orientation initialOrientation, CancellationToken cancellationToken = default)
    {
        return await _connectionPipeline.ExecuteAsync(async (context) =>
        {
            _logger.LogDebug("Attempting resilient connection to robot {RobotId}",
                robotId);

            var connected = await _communicationService.ConnectToRobotAsync(robotId, initialPosition, initialOrientation, cancellationToken);
            
            if (!connected)
            {
                throw new RobotConnectionException($"Failed to connect to robot {robotId}");
            }

            _logger.LogInformation("Successfully established resilient connection to robot {RobotId}", robotId);
            return connected;
        }, cancellationToken);
    }

    /// <summary>
    /// Sends a command to a robot with full resilience (retry + circuit breaker)
    /// </summary>
    public async Task<CommandResponse> SendCommandWithResilienceAsync(string robotId, char instruction, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _commandPipeline.ExecuteAsync(async (context) =>
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
            }, cancellationToken);
        }
        catch (Exception ex) when (ex is not RobotCommandException and not TimeoutException)
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
        return await _queryPipeline.ExecuteAsync(async (context) =>
        {
            var robot = await _communicationService.GetRobotStateAsync(robotId, cancellationToken);
            
            if (robot == null)
            {
                throw new RobotNotFoundException($"Robot {robotId} not found");
            }

            return robot;
        }, cancellationToken);
    }

    /// <summary>
    /// Performs health check on a robot with resilience
    /// </summary>
    public async Task<bool> HealthCheckRobotAsync(string robotId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _queryPipeline.ExecuteAsync(async (context) =>
            {
                return await _communicationService.PingRobotAsync(robotId, cancellationToken);
            }, cancellationToken);
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