using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MartianRobots.Core.Resilience;

namespace MartianRobots.Tests.Core.Resilience;

/// <summary>
/// Tests for ResiliencePipelineProvider
/// </summary>
public class ResiliencePipelineProviderTests
{
    [Fact]
    public void Constructor_WithValidOptions_ShouldCreatePipeline()
    {
        // Arrange
        var options = Options.Create(new RobotCommunicationOptions
        {
            MaxRetryAttempts = 3,
            CommandTimeout = TimeSpan.FromSeconds(5)
        });
        var logger = NullLogger<ResiliencePipelineProvider>.Instance;

        // Act
        var provider = new ResiliencePipelineProvider(options, logger);

        // Assert
        provider.Should().NotBeNull();
        provider.Pipeline.Should().NotBeNull();
    }

    [Fact]
    public void Pipeline_ShouldBeAccessible()
    {
        // Arrange
        var options = Options.Create(new RobotCommunicationOptions
        {
            MaxRetryAttempts = 2
        });
        var provider = new ResiliencePipelineProvider(
            options,
            NullLogger<ResiliencePipelineProvider>.Instance);

        // Act
        var pipeline = provider.Pipeline;

        // Assert
        pipeline.Should().NotBeNull();
    }

    [Fact]
    public async Task Pipeline_ShouldExecuteOperation()
    {
        // Arrange
        var options = Options.Create(new RobotCommunicationOptions
        {
            BaseDelay = TimeSpan.FromMilliseconds(1), // Fast for tests
            MaxRetryAttempts = 1
        });
        var provider = new ResiliencePipelineProvider(
            options,
            NullLogger<ResiliencePipelineProvider>.Instance);
        var executed = false;

        // Act
        await provider.Pipeline.ExecuteAsync(async ct =>
        {
            executed = true;
            await Task.CompletedTask;
        });

        // Assert
        executed.Should().BeTrue();
    }

    [Fact]
    public async Task Pipeline_WithTimeoutException_ShouldNotRetry()
    {
        // Arrange
        var options = Options.Create(new RobotCommunicationOptions
        {
            BaseDelay = TimeSpan.FromMilliseconds(1), // Fast for tests
            MaxRetryAttempts = 3
        });
        var provider = new ResiliencePipelineProvider(
            options,
            NullLogger<ResiliencePipelineProvider>.Instance);
        var attemptCount = 0;

        // Act & Assert
        await Assert.ThrowsAsync<TimeoutException>(async () =>
        {
            await provider.Pipeline.ExecuteAsync(async ct =>
            {
                attemptCount++;
                await Task.CompletedTask;
                throw new TimeoutException("Operation timed out");
            });
        });

        // TimeoutException should not be retried, so only 1 attempt
        attemptCount.Should().Be(1);
    }

    [Fact]
    public async Task Pipeline_WithOperationCanceledException_ShouldNotRetry()
    {
        // Arrange
        var options = Options.Create(new RobotCommunicationOptions
        {
            BaseDelay = TimeSpan.FromMilliseconds(1), // Fast for tests
            MaxRetryAttempts = 3
        });
        var provider = new ResiliencePipelineProvider(
            options,
            NullLogger<ResiliencePipelineProvider>.Instance);
        var attemptCount = 0;

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await provider.Pipeline.ExecuteAsync(async ct =>
            {
                attemptCount++;
                await Task.CompletedTask;
                throw new OperationCanceledException("Operation was cancelled");
            });
        });

        // OperationCanceledException should not be retried, so only 1 attempt
        attemptCount.Should().Be(1);
    }

    [Fact]
    public async Task Pipeline_WithRetriableException_ShouldRetry()
    {
        // Arrange
        var options = Options.Create(new RobotCommunicationOptions
        {
            BaseDelay = TimeSpan.FromMilliseconds(1), // Fast for tests
            MaxRetryAttempts = 3
        });
        var provider = new ResiliencePipelineProvider(
            options,
            NullLogger<ResiliencePipelineProvider>.Instance);
        var attemptCount = 0;

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await provider.Pipeline.ExecuteAsync(async ct =>
            {
                attemptCount++;
                await Task.CompletedTask;
                throw new InvalidOperationException("Retriable error");
            });
        });

        // Should retry 3 times (initial + 3 retries = 4 total attempts)
        attemptCount.Should().Be(4); // Initial attempt + 3 retries
    }

    [Fact]
    public async Task Pipeline_WithSuccessAfterRetry_ShouldSucceed()
    {
        // Arrange
        var options = Options.Create(new RobotCommunicationOptions
        {
            BaseDelay = TimeSpan.FromMilliseconds(1), // Fast for tests
            MaxRetryAttempts = 3
        });
        var provider = new ResiliencePipelineProvider(
            options,
            NullLogger<ResiliencePipelineProvider>.Instance);
        var attemptCount = 0;

        // Act
        var result = await provider.Pipeline.ExecuteAsync(async ct =>
        {
            attemptCount++;
            await Task.CompletedTask;
            
            // Fail first 2 attempts, succeed on 3rd
            if (attemptCount < 3)
            {
                throw new InvalidOperationException("Temporary error");
            }
            
            return "Success";
        });

        // Assert
        result.Should().Be("Success");
        attemptCount.Should().Be(3); // Succeeded on 3rd attempt
    }
}
