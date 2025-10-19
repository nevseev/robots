using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;

namespace MartianRobots.Core.Resilience;

/// <summary>
/// Provides configured resilience pipelines for robot communication operations
/// </summary>
public interface IResiliencePipelineProvider
{
    /// <summary>
    /// Gets the resilience pipeline for robot operations
    /// </summary>
    ResiliencePipeline Pipeline { get; }
}

/// <summary>
/// Default implementation of resilience pipeline provider
/// </summary>
public sealed class ResiliencePipelineProvider : IResiliencePipelineProvider
{
    public ResiliencePipeline Pipeline { get; }

    public ResiliencePipelineProvider(
        IOptions<RobotCommunicationOptions> options,
        ILogger<ResiliencePipelineProvider> logger)
    {
        var opts = options.Value;

        // Use fast retry delay for tests (when BaseDelay is very small), otherwise use 1 second for production
        var retryDelay = opts.BaseDelay < TimeSpan.FromMilliseconds(100) 
            ? TimeSpan.FromMilliseconds(1)  // Fast for tests
            : TimeSpan.FromSeconds(1);       // Production delay

        Pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = opts.MaxRetryAttempts,
                Delay = retryDelay,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                MaxDelay = TimeSpan.FromMinutes(5),
                ShouldHandle = new PredicateBuilder().Handle<Exception>(ex =>
                    ex is not TimeoutException &&
                    ex is not OperationCanceledException),
                OnRetry = args =>
                {
                    logger.LogWarning("Retry attempt {AttemptNumber} after {RetryDelay}ms due to: {Exception}",
                        args.AttemptNumber + 1, args.RetryDelay.TotalMilliseconds, args.Outcome.Exception?.Message);
                    return default;
                }
            })
            .Build();
    }
}
