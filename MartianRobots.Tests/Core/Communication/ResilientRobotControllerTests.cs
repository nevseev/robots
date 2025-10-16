using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MartianRobots.Abstractions.Models;
using MartianRobots.Core.Communication;
using Moq;
using Xunit;

namespace MartianRobots.Tests.Core.Communication;

public class ResilientRobotControllerTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly Mock<IRobotCommunicationService> _mockCommunicationService;
    private readonly ResilientRobotController _controller;

    public ResilientRobotControllerTests()
    {
        _mockCommunicationService = new Mock<IRobotCommunicationService>();
        
        var services = new ServiceCollection();
        services.AddResilienceEnricher();
        services.AddSingleton(_mockCommunicationService.Object);
        services.AddSingleton<ILogger<ResilientRobotController>>(NullLogger<ResilientRobotController>.Instance);
        
        _serviceProvider = services.BuildServiceProvider();
        
        var options = Options.Create(new RobotCommunicationOptions
        {
            BaseDelay = TimeSpan.FromMilliseconds(10),
            MaxRandomDelay = TimeSpan.FromMilliseconds(20),
            FailureProbability = 0.0,
            CommandTimeout = TimeSpan.FromSeconds(1),
            MaxRetryAttempts = 3
        });
        
        _controller = new ResilientRobotController(
            _mockCommunicationService.Object,
            NullLogger<ResilientRobotController>.Instance,
            options);
    }

    [Fact]
    public async Task ConnectRobotAsync_Success_ShouldReturnTrue()
    {
        // Arrange
        const string robotId = "TEST-ROBOT-1";
        var position = new Position(1, 1);
        var orientation = Orientation.North;
        
        _mockCommunicationService
            .Setup(s => s.ConnectToRobotAsync(robotId, position, orientation, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.ConnectRobotAsync(robotId, position, orientation);

        // Assert
        Assert.True(result);
        _mockCommunicationService.Verify(
            s => s.ConnectToRobotAsync(robotId, position, orientation, It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Fact]
    public async Task ConnectRobotAsync_Failure_ShouldThrowAndRetry()
    {
        // Arrange
        const string robotId = "TEST-ROBOT-2";
        var position = new Position(1, 1);
        var orientation = Orientation.North;
        
        _mockCommunicationService
            .Setup(s => s.ConnectToRobotAsync(robotId, position, orientation, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<RobotConnectionException>(
            () => _controller.ConnectRobotAsync(robotId, position, orientation));
        
        // Verify it was called (resilience pipeline will retry)
        _mockCommunicationService.Verify(
            s => s.ConnectToRobotAsync(robotId, position, orientation, It.IsAny<CancellationToken>()), 
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task SendCommandWithResilienceAsync_Success_ShouldReturnResponse()
    {
        // Arrange
        const string robotId = "TEST-ROBOT-3";
        const char instruction = 'F';
        var expectedResponse = new CommandResponse
        {
            CommandId = Guid.NewGuid().ToString(),
            RobotId = robotId,
            Status = CommandStatus.Executed,
            NewPosition = new Position(2, 1),
            NewOrientation = Orientation.North
        };
        
        _mockCommunicationService
            .Setup(s => s.SendCommandAsync(robotId, instruction, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.SendCommandWithResilienceAsync(robotId, instruction);

        // Assert
        Assert.Equal(expectedResponse.Status, result.Status);
        Assert.Equal(expectedResponse.RobotId, result.RobotId);
        Assert.Equal(expectedResponse.NewPosition, result.NewPosition);
        Assert.Equal(expectedResponse.NewOrientation, result.NewOrientation);
    }

    [Fact]
    public async Task SendCommandWithResilienceAsync_CommandFailed_ShouldThrowException()
    {
        // Arrange
        const string robotId = "TEST-ROBOT-4";
        const char instruction = 'F';
        var failedResponse = new CommandResponse
        {
            CommandId = Guid.NewGuid().ToString(),
            RobotId = robotId,
            Status = CommandStatus.Failed,
            ErrorMessage = "Robot malfunction"
        };
        
        _mockCommunicationService
            .Setup(s => s.SendCommandAsync(robotId, instruction, It.IsAny<CancellationToken>()))
            .ReturnsAsync(failedResponse);

        // Act & Assert
        await Assert.ThrowsAsync<RobotCommandException>(
            () => _controller.SendCommandWithResilienceAsync(robotId, instruction));
    }

    [Fact]
    public async Task SendCommandWithResilienceAsync_CommandTimedOut_ShouldThrowTimeoutException()
    {
        // Arrange
        const string robotId = "TEST-ROBOT-5";
        const char instruction = 'F';
        var timeoutResponse = new CommandResponse
        {
            CommandId = Guid.NewGuid().ToString(),
            RobotId = robotId,
            Status = CommandStatus.TimedOut,
            ErrorMessage = "Command timed out"
        };
        
        _mockCommunicationService
            .Setup(s => s.SendCommandAsync(robotId, instruction, It.IsAny<CancellationToken>()))
            .ReturnsAsync(timeoutResponse);

        // Act & Assert
        await Assert.ThrowsAsync<TimeoutException>(
            () => _controller.SendCommandWithResilienceAsync(robotId, instruction));
    }

    [Fact]
    public async Task ExecuteInstructionSequenceAsync_AllCommandsSucceed_ShouldExecuteAll()
    {
        // Arrange
        const string robotId = "TEST-ROBOT-6";
        const string instructions = "RFR";
        var responses = new[]
        {
            new CommandResponse { Status = CommandStatus.Executed, RobotId = robotId },
            new CommandResponse { Status = CommandStatus.Executed, RobotId = robotId },
            new CommandResponse { Status = CommandStatus.Executed, RobotId = robotId }
        };
        
        _mockCommunicationService
            .SetupSequence(s => s.SendCommandAsync(robotId, It.IsAny<char>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(responses[0])
            .ReturnsAsync(responses[1])
            .ReturnsAsync(responses[2]);

        // Act
        var result = await _controller.ExecuteInstructionSequenceAsync(robotId, instructions);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.All(result, r => Assert.Equal(CommandStatus.Executed, r.Status));
        _mockCommunicationService.Verify(
            s => s.SendCommandAsync(robotId, It.IsAny<char>(), It.IsAny<CancellationToken>()),
            Times.Exactly(3));
    }

    [Fact]
    public async Task ExecuteInstructionSequenceAsync_RobotLost_ShouldStopExecution()
    {
        // Arrange
        const string robotId = "TEST-ROBOT-7";
        const string instructions = "FFF";
        var responses = new[]
        {
            new CommandResponse { Status = CommandStatus.Executed, RobotId = robotId, IsLost = false },
            new CommandResponse { Status = CommandStatus.Executed, RobotId = robotId, IsLost = true }, // Robot lost
            new CommandResponse { Status = CommandStatus.Executed, RobotId = robotId, IsLost = false }  // This shouldn't be reached
        };
        
        _mockCommunicationService
            .SetupSequence(s => s.SendCommandAsync(robotId, It.IsAny<char>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(responses[0])
            .ReturnsAsync(responses[1])
            .ReturnsAsync(responses[2]);

        // Act
        var result = await _controller.ExecuteInstructionSequenceAsync(robotId, instructions);

        // Assert
        Assert.Equal(2, result.Count); // Should stop after robot is lost
        Assert.True(result[1].IsLost);
        _mockCommunicationService.Verify(
            s => s.SendCommandAsync(robotId, It.IsAny<char>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2)); // Should not execute the third command
    }

    [Fact]
    public async Task ExecuteInstructionSequenceAsync_CommandFails_ShouldStopExecution()
    {
        // Arrange
        const string robotId = "TEST-ROBOT-8";
        const string instructions = "RFR";
        
        _mockCommunicationService
            .SetupSequence(s => s.SendCommandAsync(robotId, It.IsAny<char>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CommandResponse { Status = CommandStatus.Executed, RobotId = robotId })
            .ThrowsAsync(new RobotCommandException("Command failed"));

        // Act
        var result = await _controller.ExecuteInstructionSequenceAsync(robotId, instructions);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(CommandStatus.Executed, result[0].Status);
        Assert.Equal(CommandStatus.Failed, result[1].Status);
        Assert.Contains("Command failed", result[1].ErrorMessage);
    }

    [Fact]
    public async Task GetRobotStateAsync_Success_ShouldReturnRobotState()
    {
        // Arrange
        const string robotId = "TEST-ROBOT-9";
        var expectedRobot = new RobotInstance
        {
            Id = robotId,
            Position = new Position(1, 1),
            Orientation = Orientation.North,
            ConnectionState = ConnectionState.Connected
        };
        
        _mockCommunicationService
            .Setup(s => s.GetRobotStateAsync(robotId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedRobot);

        // Act
        var result = await _controller.GetRobotStateAsync(robotId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedRobot.Id, result.Id);
        Assert.Equal(expectedRobot.Position, result.Position);
        Assert.Equal(expectedRobot.Orientation, result.Orientation);
        Assert.Equal(expectedRobot.ConnectionState, result.ConnectionState);
    }

    [Fact]
    public async Task GetRobotStateAsync_RobotNotFound_ShouldThrowException()
    {
        // Arrange
        const string robotId = "NONEXISTENT-ROBOT";
        
        _mockCommunicationService
            .Setup(s => s.GetRobotStateAsync(robotId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RobotInstance?)null);

        // Act & Assert
        await Assert.ThrowsAsync<RobotNotFoundException>(
            () => _controller.GetRobotStateAsync(robotId));
    }

    [Fact]
    public async Task HealthCheckRobotAsync_RobotHealthy_ShouldReturnTrue()
    {
        // Arrange
        const string robotId = "HEALTHY-ROBOT";
        
        _mockCommunicationService
            .Setup(s => s.PingRobotAsync(robotId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.HealthCheckRobotAsync(robotId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task HealthCheckRobotAsync_RobotUnhealthy_ShouldReturnFalse()
    {
        // Arrange
        const string robotId = "UNHEALTHY-ROBOT";
        
        _mockCommunicationService
            .Setup(s => s.PingRobotAsync(robotId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.HealthCheckRobotAsync(robotId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task HealthCheckRobotAsync_ExceptionThrown_ShouldReturnFalse()
    {
        // Arrange
        const string robotId = "ERROR-ROBOT";
        
        _mockCommunicationService
            .Setup(s => s.PingRobotAsync(robotId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Communication error"));

        // Act
        var result = await _controller.HealthCheckRobotAsync(robotId);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("")]
    public async Task ExecuteInstructionSequenceAsync_InvalidRobotId_ShouldThrowArgumentException(string? robotId)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _controller.ExecuteInstructionSequenceAsync(robotId!, "RF"));
    }

    [Fact]
    public async Task ExecuteInstructionSequenceAsync_NullRobotId_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _controller.ExecuteInstructionSequenceAsync(null!, "RF"));
    }

    [Theory]
    [InlineData("")]
    public async Task ExecuteInstructionSequenceAsync_InvalidInstructions_ShouldThrowArgumentException(string? instructions)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _controller.ExecuteInstructionSequenceAsync("ROBOT-1", instructions!));
    }

    [Fact]
    public async Task ExecuteInstructionSequenceAsync_NullInstructions_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _controller.ExecuteInstructionSequenceAsync("ROBOT-1", null!));
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
        GC.SuppressFinalize(this);
    }
}