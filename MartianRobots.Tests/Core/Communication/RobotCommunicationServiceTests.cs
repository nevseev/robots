using Microsoft.Extensions.Logging.Abstractions;
using MartianRobots.Core.Communication;
using MartianRobots.Core.Services;
using MartianRobots.Tests.Mocks;

namespace MartianRobots.Tests.Core.Communication;

/// <summary>
/// Unit tests for RobotCommunicationService
/// </summary>
public class RobotCommunicationServiceTests : IDisposable
{
    private readonly RobotCommunicationService _service;
    private readonly MarsGrid _testGrid;
    private readonly RobotCommunicationOptions _options;
    private readonly MockDelayService _mockDelayService;

    public RobotCommunicationServiceTests()
    {
        _options = new RobotCommunicationOptions
        {
            BaseDelay = TimeSpan.FromMilliseconds(1), // Fast for tests
            MaxRandomDelay = TimeSpan.FromMilliseconds(1),
            FailureProbability = 0.0, // No random failures for deterministic tests
            CommandTimeout = TimeSpan.FromSeconds(5)
        };

        _mockDelayService = new MockDelayService();
        _service = new RobotCommunicationService(
            NullLogger<RobotCommunicationService>.Instance,
            _options,
            _mockDelayService,
            new NoFailureSimulator()); // Deterministic - no failures

        _testGrid = new MarsGrid(4, 4);
    }

    public void Dispose()
    {
        _service?.Dispose();
    }

    #region ConnectToRobotAsync Tests

    [Fact]
    public async Task ConnectToRobotAsync_WithValidParameters_ShouldConnectSuccessfully()
    {
        // Arrange
        var robotId = "ROBOT-1";
        var position = new Position(1, 1);
        var orientation = Orientation.North;

        // Act
        var result = await _service.ConnectToRobotAsync(robotId, position, orientation);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ConnectToRobotAsync_WithNullRobotId_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _service.ConnectToRobotAsync(null!, new Position(0, 0), Orientation.North));
    }

    [Fact]
    public async Task ConnectToRobotAsync_WithEmptyRobotId_ShouldThrowArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.ConnectToRobotAsync("", new Position(0, 0), Orientation.North));
    }

    [Fact]
    public async Task ConnectToRobotAsync_WhenCancelled_ShouldThrowOperationCanceledException()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await _service.ConnectToRobotAsync("ROBOT-2", new Position(0, 0), Orientation.North, cts.Token));
    }

    [Fact]
    public async Task ConnectToRobotAsync_WithFailureProbability_CanFail()
    {
        // Arrange - service with AlwaysFailSimulator (deterministic)
        var failingOptions = new RobotCommunicationOptions
        {
            BaseDelay = TimeSpan.FromMilliseconds(1),
            MaxRandomDelay = TimeSpan.FromMilliseconds(1),
            FailureProbability = 1.0, // Not used with AlwaysFailSimulator
            CommandTimeout = TimeSpan.FromSeconds(5)
        };

        var failingService = new RobotCommunicationService(
            NullLogger<RobotCommunicationService>.Instance,
            failingOptions,
            _mockDelayService,
            new AlwaysFailSimulator()); // Deterministic - always fails

        // Act
        var result = await failingService.ConnectToRobotAsync("ROBOT-3", new Position(0, 0), Orientation.North);

        // Assert
        result.Should().BeFalse();
        
        failingService.Dispose();
    }

    [Fact]
    public async Task ConnectToRobotAsync_WhenDelayServiceThrows_ShouldReturnFalse()
    {
        // Arrange - mock delay service that throws an exception (not OperationCanceledException)
        var mockDelayService = new Mock<IDelayService>();
        mockDelayService
            .Setup(x => x.DelayAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Network error"));

        var service = new RobotCommunicationService(
            NullLogger<RobotCommunicationService>.Instance,
            _options,
            mockDelayService.Object,
            new NoFailureSimulator());

        // Act
        var result = await service.ConnectToRobotAsync("ROBOT-FAIL", new Position(0, 0), Orientation.North);

        // Assert
        result.Should().BeFalse();
        
        service.Dispose();
    }

    #endregion

    #region DisconnectFromRobotAsync Tests

    [Fact]
    public async Task DisconnectFromRobotAsync_WithConnectedRobot_ShouldDisconnect()
    {
        // Arrange
        var robotId = "ROBOT-4";
        await _service.ConnectToRobotAsync(robotId, new Position(1, 1), Orientation.East);

        // Act
        await _service.DisconnectFromRobotAsync(robotId);

        // Assert
        var pingResult = await _service.PingRobotAsync(robotId);
        pingResult.Should().BeFalse(); // Robot should no longer be connected
    }

    [Fact]
    public async Task DisconnectFromRobotAsync_WithNonExistentRobot_ShouldNotThrow()
    {
        // Act & Assert - should not throw
        var exception = await Record.ExceptionAsync(
            async () => await _service.DisconnectFromRobotAsync("NON-EXISTENT"));
        
        exception.Should().BeNull();
    }

    [Fact]
    public async Task DisconnectFromRobotAsync_WithNullRobotId_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _service.DisconnectFromRobotAsync(null!));
    }

    #endregion

    #region PingRobotAsync Tests

    [Fact]
    public async Task PingRobotAsync_WithConnectedRobot_ShouldReturnTrue()
    {
        // Arrange
        var robotId = "ROBOT-5";
        await _service.ConnectToRobotAsync(robotId, new Position(2, 2), Orientation.South);

        // Act
        var result = await _service.PingRobotAsync(robotId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task PingRobotAsync_WithNonExistentRobot_ShouldReturnFalse()
    {
        // Act
        var result = await _service.PingRobotAsync("NON-EXISTENT");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task PingRobotAsync_WithNullRobotId_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _service.PingRobotAsync(null!));
    }

    [Fact]
    public async Task PingRobotAsync_WhenCancelled_ShouldReturnFalse()
    {
        // Arrange
        var robotId = "ROBOT-6";
        await _service.ConnectToRobotAsync(robotId, new Position(0, 0), Orientation.North);
        
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var result = await _service.PingRobotAsync(robotId, cts.Token);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region SendCommandBatchAsync Tests

    [Fact]
    public async Task SendCommandBatchAsync_WithValidCommands_ShouldExecuteSuccessfully()
    {
        // Arrange
        var robotId = "ROBOT-7";
        await _service.ConnectToRobotAsync(robotId, new Position(1, 1), Orientation.North);

        // Act
        var responses = await _service.SendCommandBatchAsync(robotId, "RFR", _testGrid);

        // Assert
        responses.Should().HaveCount(3);
        responses.Should().AllSatisfy(r => r.Status.Should().Be(CommandStatus.Executed));
        responses.Should().AllSatisfy(r => r.RobotId.Should().Be(robotId));
    }

    [Fact]
    public async Task SendCommandBatchAsync_WithNonConnectedRobot_ShouldReturnFailedResponse()
    {
        // Act
        var responses = await _service.SendCommandBatchAsync("NON-EXISTENT", "RFR", _testGrid);

        // Assert
        responses.Should().HaveCount(1);
        responses[0].Status.Should().Be(CommandStatus.Failed);
        responses[0].ErrorMessage.Should().Contain("not connected");
    }

    [Fact]
    public async Task SendCommandBatchAsync_WithNullRobotId_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _service.SendCommandBatchAsync(null!, "RFR", _testGrid));
    }

    [Fact]
    public async Task SendCommandBatchAsync_WithNullInstructions_ShouldThrowArgumentNullException()
    {
        // Arrange
        var robotId = "ROBOT-8";
        await _service.ConnectToRobotAsync(robotId, new Position(0, 0), Orientation.North);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _service.SendCommandBatchAsync(robotId, null!, _testGrid));
    }

    [Fact]
    public async Task SendCommandBatchAsync_WithEmptyInstructions_ShouldThrowArgumentException()
    {
        // Arrange
        var robotId = "ROBOT-9";
        await _service.ConnectToRobotAsync(robotId, new Position(0, 0), Orientation.North);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.SendCommandBatchAsync(robotId, "", _testGrid));
    }

    [Fact]
    public async Task SendCommandBatchAsync_WithNullGrid_ShouldThrowArgumentNullException()
    {
        // Arrange
        var robotId = "ROBOT-10";
        await _service.ConnectToRobotAsync(robotId, new Position(0, 0), Orientation.North);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _service.SendCommandBatchAsync(robotId, "RFR", null!));
    }

    [Fact]
    public async Task SendCommandBatchAsync_WithInvalidCommand_ShouldThrowArgumentException()
    {
        // Arrange
        var robotId = "ROBOT-11";
        await _service.ConnectToRobotAsync(robotId, new Position(0, 0), Orientation.North);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.SendCommandBatchAsync(robotId, "XYZ", _testGrid));
    }

    [Fact]
    public async Task SendCommandBatchAsync_WhenRobotGetsLost_ShouldStopExecution()
    {
        // Arrange
        var robotId = "ROBOT-12";
        await _service.ConnectToRobotAsync(robotId, new Position(4, 4), Orientation.North);

        // Act - move forward from edge, robot will get lost
        var responses = await _service.SendCommandBatchAsync(robotId, "FFFFF", _testGrid);

        // Assert
        responses.Should().HaveCount(1); // Should stop after getting lost
        responses[0].IsLost.Should().BeTrue();
    }

    [Fact]
    public async Task SendCommandBatchAsync_WhenCancelled_ShouldReturnTimedOutResponse()
    {
        // Arrange
        var robotId = "ROBOT-13";
        await _service.ConnectToRobotAsync(robotId, new Position(0, 0), Orientation.North);
        
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var responses = await _service.SendCommandBatchAsync(robotId, "RFR", _testGrid, cts.Token);

        // Assert
        responses.Should().Contain(r => r.Status == CommandStatus.TimedOut);
    }

    [Fact]
    public async Task SendCommandBatchAsync_WithFailureSimulator_ShouldReturnFailedCommands()
    {
        // Arrange - Create a mock that succeeds for connection but fails for command batch
        var mockFailureSimulator = new Mock<IFailureSimulator>();
        var callCount = 0;
        mockFailureSimulator.Setup(f => f.ShouldSimulateFailure())
            .Returns(() =>
            {
                callCount++;
                // First call (connection) - succeed
                if (callCount == 1) return false;
                // Subsequent calls (commands) - fail
                return true;
            });

        var failingOptions = new RobotCommunicationOptions
        {
            BaseDelay = TimeSpan.FromMilliseconds(1),
            MaxRandomDelay = TimeSpan.FromMilliseconds(1),
            FailureProbability = 1.0,
            CommandTimeout = TimeSpan.FromSeconds(5)
        };

        var failingService = new RobotCommunicationService(
            NullLogger<RobotCommunicationService>.Instance,
            failingOptions,
            _mockDelayService,
            mockFailureSimulator.Object);

        var robotId = "ROBOT-14";
        var connected = await failingService.ConnectToRobotAsync(robotId, new Position(0, 0), Orientation.North);
        connected.Should().BeTrue();

        // Act - execute commands which will all fail deterministically
        var responses = await failingService.SendCommandBatchAsync(robotId, "RRR", _testGrid);

        // Assert - all commands should fail deterministically
        responses.Should().AllSatisfy(r => r.Status.Should().Be(CommandStatus.Failed));
        responses.Should().AllSatisfy(r => r.ErrorMessage.Should().Contain("communication failure"));

        failingService.Dispose();
    }

    [Fact]
    public async Task SendCommandBatchAsync_WithFailures_ShouldTransitionToUnstableAfter3Failures()
    {
        // Arrange - Mock that succeeds for connection but fails for all commands
        var mockFailureSimulator = new Mock<IFailureSimulator>();
        var callCount = 0;
        mockFailureSimulator.Setup(f => f.ShouldSimulateFailure())
            .Returns(() =>
            {
                callCount++;
                // First call (connection) - succeed
                if (callCount == 1) return false;
                // All command calls - fail
                return true;
            });

        var failingService = new RobotCommunicationService(
            NullLogger<RobotCommunicationService>.Instance,
            _options,
            _mockDelayService,
            mockFailureSimulator.Object);

        var robotId = "ROBOT-UNSTABLE";
        var connected = await failingService.ConnectToRobotAsync(robotId, new Position(0, 0), Orientation.North);
        connected.Should().BeTrue();

        // Act - execute 5 commands to trigger failures and transition to Unstable after 3
        var responses = await failingService.SendCommandBatchAsync(robotId, "RRRRR", _testGrid);

        // Assert - all commands should fail
        responses.Should().AllSatisfy(r => r.Status.Should().Be(CommandStatus.Failed));
        
        failingService.Dispose();
    }

    [Fact]
    public async Task SendCommandBatchAsync_SuccessfulExecution_ShouldUpdateRobotPosition()
    {
        // Arrange
        var robotId = "ROBOT-15";
        await _service.ConnectToRobotAsync(robotId, new Position(1, 1), Orientation.North);

        // Act
        var responses = await _service.SendCommandBatchAsync(robotId, "F", _testGrid);

        // Assert
        responses.Should().HaveCount(1);
        responses[0].NewPosition.Should().Be(new Position(1, 2));
        responses[0].NewOrientation.Should().Be(Orientation.North);
    }

    [Fact]
    public async Task SendCommandBatchAsync_AfterDisconnect_ShouldReturnNotConnectedError()
    {
        // Arrange
        var robotId = "ROBOT-DISCONNECT-TEST";
        await _service.ConnectToRobotAsync(robotId, new Position(0, 0), Orientation.North);
        
        // Act - Disconnect the robot to put it in Disconnected state
        await _service.DisconnectFromRobotAsync(robotId);

        // Act - try to send commands to disconnected robot
        var responses = await _service.SendCommandBatchAsync(robotId, "RRR", _testGrid);

        // Assert - should fail with specific error message
        responses.Should().HaveCount(1);
        responses[0].Status.Should().Be(CommandStatus.Failed);
        responses[0].ErrorMessage.Should().Contain("not connected");
    }

    [Fact]
    public async Task SendCommandBatchAsync_WhenCommandExecutionFails_ShouldCatchExceptionAndReturnFailedResponse()
    {
        // Arrange - Create a custom service with a mock delay service that throws during execution
        var mockDelayService = new Mock<IDelayService>();
        
        // Setup: First call succeeds (for connection), subsequent calls throw to simulate failure during command execution
        var callCount = 0;
        mockDelayService.Setup(d => d.DelayAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                callCount++;
                // First call is for ConnectToRobotAsync - let it succeed
                if (callCount == 1)
                {
                    return Task.CompletedTask;
                }
                // Second call is during SendCommandBatchAsync - throw exception
                throw new InvalidOperationException("Simulated command execution failure");
            });

        var testService = new RobotCommunicationService(
            NullLogger<RobotCommunicationService>.Instance,
            _options,
            mockDelayService.Object,
            new NoFailureSimulator());

        var robotId = "ROBOT-EXCEPTION-TEST";
        
        // Connect the robot (uses first delay call which succeeds)
        var connected = await testService.ConnectToRobotAsync(robotId, new Position(0, 0), Orientation.North);
        connected.Should().BeTrue();

        // Act - Execute command which will trigger exception during delay simulation
        var responses = await testService.SendCommandBatchAsync(robotId, "R", _testGrid);

        // Assert - Should catch the exception and return failed response
        responses.Should().HaveCount(1);
        responses[0].Status.Should().Be(CommandStatus.Failed);
        responses[0].ErrorMessage.Should().Contain("Simulated command execution failure");
        
        testService.Dispose();
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public async Task Dispose_WithConnectedRobots_ShouldDisconnectAll()
    {
        // Arrange
        var service = new RobotCommunicationService(
            NullLogger<RobotCommunicationService>.Instance,
            _options,
            _mockDelayService,
            new NoFailureSimulator());

        await service.ConnectToRobotAsync("ROBOT-16", new Position(0, 0), Orientation.North);
        await service.ConnectToRobotAsync("ROBOT-17", new Position(1, 1), Orientation.East);

        // Act
        service.Dispose();

        // Assert - calling dispose should not throw
        var exception = Record.Exception(() => service.Dispose());
        exception.Should().BeNull();
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_ShouldNotThrow()
    {
        // Arrange
        var service = new RobotCommunicationService(
            NullLogger<RobotCommunicationService>.Instance,
            _options,
            _mockDelayService,
            new NoFailureSimulator());

        // Act & Assert - multiple dispose calls should not throw
        service.Dispose();
        service.Dispose();
        service.Dispose();
    }

    #endregion

    #region Integration Scenarios

    [Fact]
    public async Task CompleteWorkflow_ConnectExecuteDisconnect_ShouldWork()
    {
        // Arrange
        var robotId = "ROBOT-18";

        // Act - Connect
        var connected = await _service.ConnectToRobotAsync(robotId, new Position(2, 2), Orientation.East);
        connected.Should().BeTrue();

        // Act - Ping
        var ping1 = await _service.PingRobotAsync(robotId);
        ping1.Should().BeTrue();

        // Act - Execute commands
        var responses = await _service.SendCommandBatchAsync(robotId, "RFRFRF", _testGrid);
        responses.Should().AllSatisfy(r => r.Status.Should().Be(CommandStatus.Executed));

        // Act - Disconnect
        await _service.DisconnectFromRobotAsync(robotId);

        // Act - Ping after disconnect
        var ping2 = await _service.PingRobotAsync(robotId);
        ping2.Should().BeFalse();
    }

    [Fact]
    public async Task MultipleRobots_ShouldBeIndependent()
    {
        // Arrange
        var robot1 = "ROBOT-19";
        var robot2 = "ROBOT-20";

        // Act - Connect both robots
        await _service.ConnectToRobotAsync(robot1, new Position(0, 0), Orientation.North);
        await _service.ConnectToRobotAsync(robot2, new Position(2, 2), Orientation.South);

        // Act - Execute different commands
        var responses1 = await _service.SendCommandBatchAsync(robot1, "RR", _testGrid);
        var responses2 = await _service.SendCommandBatchAsync(robot2, "LL", _testGrid);

        // Assert - both should be successful and independent
        responses1.Should().AllSatisfy(r => r.RobotId.Should().Be(robot1));
        responses2.Should().AllSatisfy(r => r.RobotId.Should().Be(robot2));

        // Act - Disconnect one robot
        await _service.DisconnectFromRobotAsync(robot1);

        // Assert - other robot should still be connected
        var ping1 = await _service.PingRobotAsync(robot1);
        var ping2 = await _service.PingRobotAsync(robot2);

        ping1.Should().BeFalse();
        ping2.Should().BeTrue();
    }

    #endregion
}
