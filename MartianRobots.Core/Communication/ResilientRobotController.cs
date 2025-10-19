using Microsoft.Extensions.Logging;
using Polly;
using MartianRobots.Core.Resilience;

namespace MartianRobots.Core.Communication;

/// <summary>
/// A resilient wrapper around IRobotCommunicationService that adds retry capabilities
/// This controller provides additional resilience for critical robot operations
/// </summary>
public class ResilientRobotController(
    IRobotCommunicationService communicationService,
    ILogger<ResilientRobotController> logger,
    IResiliencePipelineProvider resiliencePipelineProvider) : IResilientRobotController
{
    private readonly ResiliencePipeline _resiliencePipeline = resiliencePipelineProvider.Pipeline;
    private bool _disposed;

    /// <summary>
    /// Executes an operation with retry logic using resilience pipeline
    /// </summary>
    private async Task<T> ExecuteWithRetryAsync<T>(Func<CancellationToken, ValueTask<T>> operation, string operationName, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Executing {OperationName} with resilience pipeline", operationName);
        
        try
        {
            return await _resiliencePipeline.ExecuteAsync(operation, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "All retry attempts failed for {OperationName}", operationName);
            throw;
        }
    }

    /// <summary>
    /// Establishes resilient connection to a robot
    /// </summary>
    public async Task<bool> ConnectRobotAsync(string robotId, Position initialPosition, Orientation initialOrientation, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithRetryAsync(async ct =>
        {
            logger.LogDebug("Attempting resilient connection to robot {RobotId}", robotId);

            var connected = await communicationService.ConnectToRobotAsync(robotId, initialPosition, initialOrientation, ct);
            
            if (!connected)
            {
                throw new RobotConnectionException($"Failed to connect to robot {robotId}");
            }

            logger.LogInformation("Successfully established resilient connection to robot {RobotId}", robotId);
            return connected;
        }, $"ConnectRobot({robotId})", cancellationToken);
    }

    /// <summary>
    /// Executes a sequence of instructions on a robot with resilience
    /// </summary>
    public async Task<List<CommandResponse>> ExecuteInstructionSequenceAsync(
        string robotId, 
        string instructions,
        MarsGrid grid,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(robotId);
        ArgumentException.ThrowIfNullOrEmpty(instructions);
        ArgumentNullException.ThrowIfNull(grid);

        logger.LogInformation("Executing instruction sequence '{Instructions}' on robot {RobotId}", 
            instructions, robotId);

        try
        {
            // Use the batch command API for better efficiency
            var responses = await ExecuteWithRetryAsync(
                async ct => await communicationService.SendCommandBatchAsync(robotId, instructions, grid, ct),
                $"ExecuteInstructionBatch({robotId}, {instructions})",
                cancellationToken);

            logger.LogInformation("Completed instruction sequence on robot {RobotId}: {ExecutedCount}/{TotalCount} commands", 
                robotId, responses.Count(r => r.Status == CommandStatus.Executed), instructions.Length);

            return responses;
        }
        catch (OperationCanceledException)
        {
            // Let cancellation exceptions propagate
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to execute instruction sequence on robot {RobotId}", robotId);
            
            // Return empty list or partial results
            return
            [
                new CommandResponse
                {
                    CommandId = Guid.NewGuid().ToString(),
                    RobotId = robotId,
                    Status = CommandStatus.Failed,
                    ErrorMessage = ex.Message,
                    ResponseTime = DateTime.UtcNow
                }
            ];
        }
    }

    /// <summary>
    /// Performs health check on a robot with resilience
    /// </summary>
    public async Task<bool> HealthCheckRobotAsync(string robotId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await ExecuteWithRetryAsync(async ct =>
            {
                return await communicationService.PingRobotAsync(robotId, ct);
            }, $"HealthCheck({robotId})", cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Health check failed for robot {RobotId}", robotId);
            return false;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        if (communicationService is IDisposable disposable)
        {
            disposable.Dispose();
        }
        _disposed = true;
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
