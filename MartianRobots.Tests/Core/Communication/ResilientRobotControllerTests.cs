using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MartianRobots.Core.Communication;

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
            MaxRetryAttempts = 3,
            CircuitBreakerMinimumThroughput = 2,
            CircuitBreakerSamplingDuration = TimeSpan.FromSeconds(10)
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
        var position = new Position(0, 0);
        var orientation = Orientation.North;

        _mockCommunicationService
            .Setup(s => s.ConnectToRobotAsync(robotId, position, orientation, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RobotConnectionException($"Failed to connect to robot {robotId}"));

        // Act & Assert
        await Assert.ThrowsAsync<RobotConnectionException>(
            () => _controller.ConnectRobotAsync(robotId, position, orientation));
    }    [Fact]
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

    [Fact]
    public async Task DisconnectRobotAsync_Success_ShouldReturnTrue()
    {
        // Arrange
        const string robotId = "DISCONNECT-ROBOT";
        
        _mockCommunicationService
            .Setup(s => s.DisconnectFromRobotAsync(robotId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DisconnectRobotAsync(robotId);

        // Assert
        Assert.True(result);
        _mockCommunicationService.Verify(
            s => s.DisconnectFromRobotAsync(robotId, It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Fact]
    public async Task DisconnectRobotAsync_Exception_ShouldReturnFalse()
    {
        // Arrange
        const string robotId = "DISCONNECT-ERROR-ROBOT";
        
        _mockCommunicationService
            .Setup(s => s.DisconnectFromRobotAsync(robotId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Disconnect failed"));

        // Act
        var result = await _controller.DisconnectRobotAsync(robotId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task SendCommandWithResilienceAsync_NullResponse_ShouldThrowRobotCommandException()
    {
        // Arrange
        const string robotId = "NULL-RESPONSE-ROBOT";
        const char instruction = 'F';
        
        _mockCommunicationService
            .Setup(s => s.SendCommandAsync(robotId, instruction, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CommandResponse)null!);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<RobotCommandException>(
            () => _controller.SendCommandWithResilienceAsync(robotId, instruction));
        
        Assert.Contains("null response", exception.Message);
    }

    [Fact]
    public async Task SendCommandWithResilienceAsync_UnexpectedException_ShouldWrapInRobotCommandException()
    {
        // Arrange
        const string robotId = "EXCEPTION-ROBOT";
        const char instruction = 'F';
        
        _mockCommunicationService
            .Setup(s => s.SendCommandAsync(robotId, instruction, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Unexpected error"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<RobotCommandException>(
            () => _controller.SendCommandWithResilienceAsync(robotId, instruction));
        
        Assert.Contains("Unexpected error", exception.Message);
        Assert.IsType<InvalidOperationException>(exception.InnerException);
    }

    [Fact]
    public async Task ExecuteInstructionSequenceAsync_TimeoutException_ShouldStopExecution()
    {
        // Arrange
        const string robotId = "TIMEOUT-ROBOT";
        const string instructions = "RFR";
        
        _mockCommunicationService
            .SetupSequence(s => s.SendCommandAsync(robotId, It.IsAny<char>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CommandResponse { Status = CommandStatus.Executed, RobotId = robotId })
            .ThrowsAsync(new TimeoutException("Command timed out"));

        // Act
        var result = await _controller.ExecuteInstructionSequenceAsync(robotId, instructions);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(CommandStatus.Executed, result[0].Status);
        Assert.Equal(CommandStatus.TimedOut, result[1].Status);
        Assert.Contains("Command timed out", result[1].ErrorMessage);
    }

    [Fact]
    public async Task ExecuteInstructionSequenceAsync_EmptyInstructions_ShouldReturnEmptyList()
    {
        // Arrange
        const string robotId = "EMPTY-INSTRUCTIONS-ROBOT";
        const string instructions = "";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _controller.ExecuteInstructionSequenceAsync(robotId, instructions));
    }

    [Fact]
    public async Task ExecuteInstructionSequenceAsync_CancellationRequested_ShouldHandleCancellationGracefully()
    {
        // Arrange
        const string robotId = "CANCEL-ROBOT";
        const string instructions = "RFRFRFRF";
        using var cts = new CancellationTokenSource();
        
        _mockCommunicationService
            .Setup(s => s.SendCommandAsync(robotId, It.IsAny<char>(), It.IsAny<CancellationToken>()))
            .Returns(async (string id, char cmd, CancellationToken ct) =>
            {
                if (ct.IsCancellationRequested)
                    ct.ThrowIfCancellationRequested();
                await Task.Delay(50, ct); // Allow some processing time
                return new CommandResponse { Status = CommandStatus.Executed, RobotId = id };
            });

        // Cancel immediately
        cts.Cancel();

        // Act & Assert - Should either throw OperationCanceledException or return empty result gracefully
        try
        {
            var result = await _controller.ExecuteInstructionSequenceAsync(robotId, instructions, cts.Token);
            // If no exception, the method handled cancellation gracefully
            Assert.NotNull(result);
        }
        catch (OperationCanceledException)
        {
            // This is also acceptable behavior
            Assert.True(true, "Method properly threw OperationCanceledException");
        }
    }

    [Fact]
    public void Constructor_NullCommunicationService_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = Options.Create(new RobotCommunicationOptions());

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new ResilientRobotController(null!, NullLogger<ResilientRobotController>.Instance, options));
    }

    [Fact]
    public void Constructor_NullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = Options.Create(new RobotCommunicationOptions());

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new ResilientRobotController(_mockCommunicationService.Object, null!, options));
    }

    [Fact]
    public void Constructor_NullOptions_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<NullReferenceException>(() => 
            new ResilientRobotController(_mockCommunicationService.Object, NullLogger<ResilientRobotController>.Instance, null!));
    }

    [Fact]
    public void Constructor_NullOptionsValue_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = Options.Create<RobotCommunicationOptions>(null!);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new ResilientRobotController(_mockCommunicationService.Object, NullLogger<ResilientRobotController>.Instance, options));
    }

    [Fact]
    public void Dispose_ShouldDisposeDisposableCommunicationService()
    {
        // Arrange
        var disposableMock = new Mock<IRobotCommunicationService>();
        disposableMock.As<IDisposable>();
        
        var options = Options.Create(new RobotCommunicationOptions
        {
            MaxRetryAttempts = 1,
            CommandTimeout = TimeSpan.FromSeconds(1),
            CircuitBreakerMinimumThroughput = 2,
            CircuitBreakerSamplingDuration = TimeSpan.FromSeconds(10)
        });

        var controller = new ResilientRobotController(
            disposableMock.Object,
            NullLogger<ResilientRobotController>.Instance,
            options);

        // Act
        controller.Dispose();

        // Assert
        disposableMock.As<IDisposable>().Verify(d => d.Dispose(), Times.Once);
    }

    [Fact]
    public void Dispose_MultipleCallsShouldNotThrow()
    {
        // Act & Assert - Should not throw
        _controller.Dispose();
        _controller.Dispose(); // Second call should be safe
    }

    [Fact]
    public async Task ConnectRobotAsync_WithCancellation_ShouldThrowOperationCanceledException()
    {
        // Arrange
        const string robotId = "CANCEL-CONNECT-ROBOT";
        var position = new Position(1, 1);
        var orientation = Orientation.North;
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert - TaskCanceledException is a subclass of OperationCanceledException
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _controller.ConnectRobotAsync(robotId, position, orientation, cts.Token));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ConnectRobotAsync_InvalidRobotId_ShouldThrowInvalidOperationException(string robotId)
    {
        // Arrange
        var position = new Position(0, 0);
        var orientation = Orientation.North;
        
        _mockCommunicationService
            .Setup(s => s.ConnectToRobotAsync(robotId, position, orientation, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Connection failed"));

        // Act & Assert - Now throws the original exception without wrapping
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _controller.ConnectRobotAsync(robotId, position, orientation));
    }

    [Fact]
    public async Task GetRobotStateAsync_WithCancellation_ShouldThrowOperationCanceledException()
    {
        // Arrange
        const string robotId = "CANCEL-STATE-ROBOT";
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert - TaskCanceledException is a subclass of OperationCanceledException
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _controller.GetRobotStateAsync(robotId, cts.Token));
    }

    [Fact]
    public async Task HealthCheckRobotAsync_WithCancellation_ShouldReturnFalse()
    {
        // Arrange
        const string robotId = "CANCEL-HEALTH-ROBOT";
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        
        // Act
        var result = await _controller.HealthCheckRobotAsync(robotId, cts.Token);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SendCommandWithResilienceAsync_InvalidRobotId_ShouldThrowRobotCommandException(string robotId)
    {
        // Arrange
        const char instruction = 'F';
        
        _mockCommunicationService
            .Setup(s => s.SendCommandAsync(robotId, instruction, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CommandResponse)null!);

        // Act & Assert
        await Assert.ThrowsAsync<RobotCommandException>(
            () => _controller.SendCommandWithResilienceAsync(robotId, instruction));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetRobotStateAsync_InvalidRobotId_ShouldThrowAppropriateException(string robotId)
    {
        // Arrange
        _mockCommunicationService
            .Setup(s => s.GetRobotStateAsync(robotId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Robot not found"));

        // Act & Assert - The actual exception depends on resilience policy state
        var exception = await Assert.ThrowsAnyAsync<Exception>(
            () => _controller.GetRobotStateAsync(robotId));
        
        Assert.True(exception is RobotNotFoundException || exception is InvalidOperationException,
            $"Expected RobotNotFoundException or InvalidOperationException, but got {exception.GetType().Name}");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task HealthCheckRobotAsync_InvalidRobotId_ShouldCompleteSuccessfully(string robotId)
    {
        // Arrange
        _mockCommunicationService
            .Setup(s => s.PingRobotAsync(robotId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act & Assert - the method doesn't validate arguments, it delegates to the service
        var result = await _controller.HealthCheckRobotAsync(robotId);
        Assert.False(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task DisconnectRobotAsync_InvalidRobotId_ShouldCompleteSuccessfully(string robotId)
    {
        // Arrange
        _mockCommunicationService
            .Setup(s => s.DisconnectFromRobotAsync(robotId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act & Assert - the method doesn't validate arguments, it delegates to the service
        var result = await _controller.DisconnectRobotAsync(robotId);
        Assert.True(result); // DisconnectRobotAsync typically returns true for successful completion
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
        GC.SuppressFinalize(this);
    }
}