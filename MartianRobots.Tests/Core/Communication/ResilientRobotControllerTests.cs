using Microsoft.Extensions.Logging.Abstractions;
using MartianRobots.Core.Communication;
using MartianRobots.Core.Resilience;

namespace MartianRobots.Tests.Core.Communication;

/// <summary>
/// Unit tests for ResilientRobotController
/// </summary>
public class ResilientRobotControllerTests
{
    private readonly Mock<IRobotCommunicationService> _mockCommunicationService;
    private readonly Mock<IResiliencePipelineProvider> _mockResilienceProvider;
    private readonly ResilientRobotController _controller;
    private readonly MarsGrid _testGrid;

    public ResilientRobotControllerTests()
    {
        _mockCommunicationService = new Mock<IRobotCommunicationService>();
        _mockResilienceProvider = new Mock<IResiliencePipelineProvider>();
        
        // Setup a simple pass-through resilience pipeline for unit tests
        var pipeline = new Polly.ResiliencePipelineBuilder()
            .Build();
        _mockResilienceProvider.Setup(x => x.Pipeline).Returns(pipeline);

        _controller = new ResilientRobotController(
            _mockCommunicationService.Object,
            NullLogger<ResilientRobotController>.Instance,
            _mockResilienceProvider.Object);

        _testGrid = new MarsGrid(4, 4);
    }

    [Fact]
    public async Task ConnectRobotAsync_WhenSuccessful_ShouldReturnTrue()
    {
        // Arrange
        var robotId = "TEST-ROBOT-1";
        var position = new Position(1, 1);
        var orientation = Orientation.North;

        _mockCommunicationService
            .Setup(x => x.ConnectToRobotAsync(robotId, position, orientation, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.ConnectRobotAsync(robotId, position, orientation);

        // Assert
        result.Should().BeTrue();
        _mockCommunicationService.Verify(
            x => x.ConnectToRobotAsync(robotId, position, orientation, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ConnectRobotAsync_WhenFails_ShouldThrowRobotConnectionException()
    {
        // Arrange
        var robotId = "TEST-ROBOT-2";
        var position = new Position(2, 2);
        var orientation = Orientation.East;

        _mockCommunicationService
            .Setup(x => x.ConnectToRobotAsync(robotId, position, orientation, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<RobotConnectionException>(
            async () => await _controller.ConnectRobotAsync(robotId, position, orientation));

        exception.Message.Should().Contain(robotId);
    }

    [Fact]
    public async Task ExecuteInstructionSequenceAsync_WithValidInstructions_ShouldReturnResponses()
    {
        // Arrange
        var robotId = "TEST-ROBOT-3";
        var instructions = "RFR";
        var expectedResponses = new List<CommandResponse>
        {
            new() { CommandId = "1", RobotId = robotId, Status = CommandStatus.Executed, NewPosition = new Position(1, 1), NewOrientation = Orientation.East },
            new() { CommandId = "2", RobotId = robotId, Status = CommandStatus.Executed, NewPosition = new Position(2, 1), NewOrientation = Orientation.East },
            new() { CommandId = "3", RobotId = robotId, Status = CommandStatus.Executed, NewPosition = new Position(2, 1), NewOrientation = Orientation.South }
        };

        _mockCommunicationService
            .Setup(x => x.SendCommandBatchAsync(robotId, instructions, _testGrid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponses);

        // Act
        var result = await _controller.ExecuteInstructionSequenceAsync(robotId, instructions, _testGrid);

        // Assert
        result.Should().HaveCount(3);
        result.Should().BeEquivalentTo(expectedResponses);
    }

    [Fact]
    public async Task ExecuteInstructionSequenceAsync_WithNullRobotId_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _controller.ExecuteInstructionSequenceAsync(null!, "RFR", _testGrid));
    }

    [Fact]
    public async Task ExecuteInstructionSequenceAsync_WithEmptyInstructions_ShouldThrowArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _controller.ExecuteInstructionSequenceAsync("ROBOT-1", "", _testGrid));
    }

    [Fact]
    public async Task ExecuteInstructionSequenceAsync_WithNullGrid_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _controller.ExecuteInstructionSequenceAsync("ROBOT-1", "RFR", null!));
    }

    [Fact]
    public async Task ExecuteInstructionSequenceAsync_WhenOperationCancelled_ShouldPropagateCancellation()
    {
        // Arrange
        var robotId = "TEST-ROBOT-4";
        var instructions = "RFR";
        var cts = new CancellationTokenSource();
        cts.Cancel();

        _mockCommunicationService
            .Setup(x => x.SendCommandBatchAsync(robotId, instructions, _testGrid, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await _controller.ExecuteInstructionSequenceAsync(robotId, instructions, _testGrid, cts.Token));
    }

    [Fact]
    public async Task ExecuteInstructionSequenceAsync_WhenExceptionOccurs_ShouldReturnFailedResponse()
    {
        // Arrange
        var robotId = "TEST-ROBOT-5";
        var instructions = "RFR";
        var exceptionMessage = "Communication failed";

        _mockCommunicationService
            .Setup(x => x.SendCommandBatchAsync(robotId, instructions, _testGrid, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception(exceptionMessage));

        // Act
        var result = await _controller.ExecuteInstructionSequenceAsync(robotId, instructions, _testGrid);

        // Assert
        result.Should().HaveCount(1);
        result[0].Status.Should().Be(CommandStatus.Failed);
        result[0].RobotId.Should().Be(robotId);
        result[0].ErrorMessage.Should().Contain(exceptionMessage);
    }

    [Fact]
    public async Task HealthCheckRobotAsync_WhenRobotResponds_ShouldReturnTrue()
    {
        // Arrange
        var robotId = "TEST-ROBOT-6";

        _mockCommunicationService
            .Setup(x => x.PingRobotAsync(robotId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.HealthCheckRobotAsync(robotId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HealthCheckRobotAsync_WhenRobotDoesNotRespond_ShouldReturnFalse()
    {
        // Arrange
        var robotId = "TEST-ROBOT-7";

        _mockCommunicationService
            .Setup(x => x.PingRobotAsync(robotId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.HealthCheckRobotAsync(robotId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HealthCheckRobotAsync_WhenExceptionOccurs_ShouldReturnFalse()
    {
        // Arrange
        var robotId = "TEST-ROBOT-8";

        _mockCommunicationService
            .Setup(x => x.PingRobotAsync(robotId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Health check failed"));

        // Act
        var result = await _controller.HealthCheckRobotAsync(robotId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Dispose_ShouldDisposeDisposableCommunicationService()
    {
        // Arrange
        var mockDisposableService = new Mock<IRobotCommunicationService>();
        mockDisposableService.As<IDisposable>();
        
        var controller = new ResilientRobotController(
            mockDisposableService.Object,
            NullLogger<ResilientRobotController>.Instance,
            _mockResilienceProvider.Object);

        // Act
        controller.Dispose();

        // Assert
        mockDisposableService.As<IDisposable>().Verify(x => x.Dispose(), Times.Once);
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_ShouldOnlyDisposeOnce()
    {
        // Arrange
        var mockDisposableService = new Mock<IRobotCommunicationService>();
        mockDisposableService.As<IDisposable>();
        
        var controller = new ResilientRobotController(
            mockDisposableService.Object,
            NullLogger<ResilientRobotController>.Instance,
            _mockResilienceProvider.Object);

        // Act
        controller.Dispose();
        controller.Dispose();
        controller.Dispose();

        // Assert - Should only dispose once
        mockDisposableService.As<IDisposable>().Verify(x => x.Dispose(), Times.Once);
    }

    [Fact]
    public void Dispose_WhenServiceNotDisposable_ShouldNotThrow()
    {
        // Arrange - using the existing non-disposable mock

        // Act & Assert - should not throw
        var exception = Record.Exception(() => _controller.Dispose());
        exception.Should().BeNull();
    }
}
