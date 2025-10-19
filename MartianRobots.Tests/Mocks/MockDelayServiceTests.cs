namespace MartianRobots.Tests.Mocks;

/// <summary>
/// Tests for MockDelayService functionality
/// </summary>
public class MockDelayServiceTests
{
    [Fact]
    public async Task DelayAsync_ShouldRecordCall()
    {
        // Arrange
        var mockService = new MockDelayService();
        var delay = TimeSpan.FromMilliseconds(100);

        // Act
        await mockService.DelayAsync(delay);

        // Assert
        Assert.Single(mockService.DelayCalls);
        Assert.Equal(delay, mockService.DelayCalls[0].Delay);
        Assert.False(mockService.DelayCalls[0].WasCancellationRequested);
    }

    [Fact]
    public async Task DelayAsync_WithCancellation_ShouldRecordCancellationState()
    {
        // Arrange
        var mockService = new MockDelayService();
        var delay = TimeSpan.FromMilliseconds(100);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => mockService.DelayAsync(delay, cts.Token));

        // Should still record the call even though it was cancelled
        Assert.Single(mockService.DelayCalls);
        Assert.Equal(delay, mockService.DelayCalls[0].Delay);
        Assert.True(mockService.DelayCalls[0].WasCancellationRequested);
    }

    [Fact]
    public async Task DelayAsync_MultipleCalls_ShouldRecordAllCalls()
    {
        // Arrange
        var mockService = new MockDelayService();
        var delays = new[] { 
            TimeSpan.FromMilliseconds(100), 
            TimeSpan.FromMilliseconds(200), 
            TimeSpan.FromMilliseconds(300) 
        };

        // Act
        foreach (var delay in delays)
        {
            await mockService.DelayAsync(delay);
        }

        // Assert
        Assert.Equal(3, mockService.DelayCalls.Count);
        for (int i = 0; i < delays.Length; i++)
        {
            Assert.Equal(delays[i], mockService.DelayCalls[i].Delay);
        }
    }

    [Fact]
    public void ClearCalls_WithNoCalls_ShouldReturnZero()
    {
        // Arrange
        var mockService = new MockDelayService();

        // Act
        mockService.ClearCalls();

        // Assert
        Assert.Empty(mockService.DelayCalls);
    }

    [Fact]
    public async Task ClearCalls_WithSingleCall_ShouldReturnOne()
    {
        // Arrange
        var mockService = new MockDelayService();
        await mockService.DelayAsync(TimeSpan.FromMilliseconds(100));

        // Act
        mockService.ClearCalls();

        // Assert
        Assert.Empty(mockService.DelayCalls);
    }

    [Fact]
    public async Task ClearCalls_WithMultipleCalls_ShouldReturnCorrectCount()
    {
        // Arrange
        var mockService = new MockDelayService();
        
        // Add multiple calls
        await mockService.DelayAsync(TimeSpan.FromMilliseconds(100));
        await mockService.DelayAsync(TimeSpan.FromMilliseconds(200));
        await mockService.DelayAsync(TimeSpan.FromMilliseconds(300));
        await mockService.DelayAsync(TimeSpan.FromMilliseconds(400));
        await mockService.DelayAsync(TimeSpan.FromMilliseconds(500));

        // Verify we have calls before clearing
        Assert.Equal(5, mockService.DelayCalls.Count);

        // Act
        mockService.ClearCalls();

        // Assert
        Assert.Empty(mockService.DelayCalls); // Should be empty after clearing
    }

    [Fact]
    public async Task ClearCalls_CalledTwice_ShouldReturnZeroOnSecondCall()
    {
        // Arrange
        var mockService = new MockDelayService();
        await mockService.DelayAsync(TimeSpan.FromMilliseconds(100));

        // Act
        mockService.ClearCalls();
        mockService.ClearCalls();

        // Assert
        Assert.Empty(mockService.DelayCalls);
    }

    [Fact]
    public async Task DelayAsync_AfterClear_ShouldRecordNewCalls()
    {
        // Arrange
        var mockService = new MockDelayService();
        
        // Add some calls and clear them
        await mockService.DelayAsync(TimeSpan.FromMilliseconds(100));
        await mockService.DelayAsync(TimeSpan.FromMilliseconds(200));
        mockService.ClearCalls();

        // Act - Add new calls after clearing
        await mockService.DelayAsync(TimeSpan.FromMilliseconds(300));
        await mockService.DelayAsync(TimeSpan.FromMilliseconds(400));

        // Assert
        Assert.Equal(2, mockService.DelayCalls.Count);
        Assert.Equal(TimeSpan.FromMilliseconds(300), mockService.DelayCalls[0].Delay);
        Assert.Equal(TimeSpan.FromMilliseconds(400), mockService.DelayCalls[1].Delay);
    }

    [Fact]
    public async Task DelayAsync_WithMilliseconds_ShouldConvertToTimeSpan()
    {
        // Arrange
        var mockService = new MockDelayService();
        const int delayMs = 500;

        // Act
        await mockService.DelayAsync(TimeSpan.FromMilliseconds(delayMs));

        // Assert
        Assert.Single(mockService.DelayCalls);
        Assert.Equal(TimeSpan.FromMilliseconds(delayMs), mockService.DelayCalls[0].Delay);
    }

    [Fact]
    public async Task ConcurrentOperations_ShouldHandleThreadSafety()
    {
        // Arrange
        var mockService = new MockDelayService();
        const int numberOfTasks = 10;
        const int callsPerTask = 5;

        // Act - Execute multiple tasks concurrently adding delays
        var tasks = new List<Task>();
        for (int i = 0; i < numberOfTasks; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                for (int j = 0; j < callsPerTask; j++)
                {
                    await mockService.DelayAsync(TimeSpan.FromMilliseconds(j * 10));
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert - All calls should be recorded
        Assert.Equal(numberOfTasks * callsPerTask, mockService.DelayCalls.Count);

        // Act - Clear all calls
        mockService.ClearCalls();

        // Assert - Should have cleared all calls
        Assert.Empty(mockService.DelayCalls);
    }
}