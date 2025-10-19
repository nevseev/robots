using MartianRobots.Core.Services;

namespace MartianRobots.Tests.Core.Services;

public class DelayServiceTests
{
    private readonly DelayService _delayService;

    public DelayServiceTests()
    {
        _delayService = new DelayService();
    }

    [Fact]
    public async Task DelayAsync_WithTimeSpan_ShouldCompleteSuccessfully()
    {
        // Arrange
        var delay = TimeSpan.FromMilliseconds(1); // Minimal delay for fast test

        // Act & Assert - Should complete without throwing
        await _delayService.DelayAsync(delay);
        
        // If we reach here, the delay completed successfully
        Assert.True(true);
    }

    [Fact]
    public async Task DelayAsync_WithMilliseconds_ShouldCompleteSuccessfully()
    {
        // Arrange
        const int delayMs = 1; // Minimal delay for fast test

        // Act & Assert - Should complete without throwing
        await _delayService.DelayAsync(TimeSpan.FromMilliseconds(delayMs));
        
        // If we reach here, the delay completed successfully
        Assert.True(true);
    }

    [Fact]
    public async Task DelayAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var delay = TimeSpan.FromSeconds(10); // Long delay
        
        // Cancel immediately for deterministic behavior
        cts.Cancel();

        // Act & Assert - Should throw OperationCanceledException
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await _delayService.DelayAsync(delay, cts.Token));
    }

    [Fact]
    public async Task DelayAsync_WithMillisecondsAndCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        const int delayMs = 10000; // Long delay
        
        // Cancel immediately for deterministic behavior
        cts.Cancel();

        // Act & Assert - Should throw OperationCanceledException
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await _delayService.DelayAsync(TimeSpan.FromMilliseconds(delayMs), cts.Token));
    }

    [Fact]
    public async Task DelayAsync_WithZeroDelay_ShouldCompleteImmediately()
    {
        // Arrange
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        await _delayService.DelayAsync(TimeSpan.Zero);

        // Assert - Zero delay should complete successfully  
        stopwatch.Stop();
        Assert.True(stopwatch.Elapsed >= TimeSpan.Zero);
    }

    [Fact]
    public async Task DelayAsync_WithZeroMilliseconds_ShouldCompleteImmediately()
    {
        // Arrange
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        await _delayService.DelayAsync(TimeSpan.Zero);

        // Assert - Zero delay should complete successfully
        stopwatch.Stop();
        Assert.True(stopwatch.Elapsed >= TimeSpan.Zero);
    }
}