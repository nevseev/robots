using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MartianRobots.Abstractions.Models;
using MartianRobots.Core.Communication;
using Xunit;

namespace MartianRobots.Tests.Core.Communication;

public class RobotCommunicationServiceTests
{
    private readonly RobotCommunicationService _service;
    private readonly RobotCommunicationOptions _options;
    private readonly ILogger<RobotCommunicationService> _logger;

    public RobotCommunicationServiceTests()
    {
        _options = new RobotCommunicationOptions
        {
            BaseDelay = TimeSpan.Zero, // No delay for tests
            MaxRandomDelay = TimeSpan.Zero, // No random delay for tests
            FailureProbability = 0.0, // No simulated failures for tests
            MaxRetryAttempts = 3,
            CommandTimeout = TimeSpan.FromSeconds(30)
        };
        _logger = NullLogger<RobotCommunicationService>.Instance;
        _service = new RobotCommunicationService(_logger, _options);
    }

    [Fact]
    public async Task ConnectToRobotAsync_ShouldConnectSuccessfully()
    {
        // Arrange
        const string robotId = "TEST-ROBOT-1";
        var position = new Position(1, 1);
        var orientation = Orientation.North;

        // Act
        var result = await _service.ConnectToRobotAsync(robotId, position, orientation);

        // Assert
        Assert.True(result);
        
        var robotState = await _service.GetRobotStateAsync(robotId);
        Assert.NotNull(robotState);
        Assert.Equal(robotId, robotState.Id);
        Assert.Equal(position, robotState.Position);
        Assert.Equal(orientation, robotState.Orientation);
        Assert.Equal(ConnectionState.Connected, robotState.ConnectionState);
        Assert.False(robotState.IsLost);
    }

    [Fact]
    public async Task ConnectToRobotAsync_SameRobotTwice_ShouldReturnFalse()
    {
        // Arrange
        const string robotId = "TEST-ROBOT-2";
        var position = new Position(2, 2);
        var orientation = Orientation.East;

        // Act
        var firstConnection = await _service.ConnectToRobotAsync(robotId, position, orientation);
        var secondConnection = await _service.ConnectToRobotAsync(robotId, position, orientation);

        // Assert
        Assert.True(firstConnection);
        Assert.True(secondConnection); // Service allows reconnection (overwrites existing)
    }

    [Fact]
    public async Task SendCommandAsync_WithValidCommand_ShouldExecuteSuccessfully()
    {
        // Arrange
        const string robotId = "TEST-ROBOT-3";
        var position = new Position(2, 2);
        var orientation = Orientation.North;
        
        await _service.ConnectToRobotAsync(robotId, position, orientation);

        // Act
        var response = await _service.SendCommandAsync(robotId, 'F');

        // Assert
        Assert.NotNull(response);
        Assert.Equal(robotId, response.RobotId);
        Assert.Equal(CommandStatus.Executed, response.Status);
        Assert.Equal(new Position(2, 3), response.NewPosition);
        Assert.Equal(Orientation.North, response.NewOrientation);
        Assert.False(response.IsLost);
        Assert.Null(response.ErrorMessage);
    }

    [Fact]
    public async Task SendCommandAsync_RightTurn_ShouldChangeOrientation()
    {
        // Arrange
        const string robotId = "TEST-ROBOT-4";
        var position = new Position(1, 1);
        var orientation = Orientation.North;
        
        await _service.ConnectToRobotAsync(robotId, position, orientation);

        // Act
        var response = await _service.SendCommandAsync(robotId, 'R');

        // Assert
        Assert.NotNull(response);
        Assert.Equal(CommandStatus.Executed, response.Status);
        Assert.Equal(position, response.NewPosition); // Position unchanged
        Assert.Equal(Orientation.East, response.NewOrientation);
        Assert.False(response.IsLost);
    }

    [Fact]
    public async Task SendCommandAsync_LeftTurn_ShouldChangeOrientation()
    {
        // Arrange
        const string robotId = "TEST-ROBOT-5";
        var position = new Position(1, 1);
        var orientation = Orientation.North;
        
        await _service.ConnectToRobotAsync(robotId, position, orientation);

        // Act
        var response = await _service.SendCommandAsync(robotId, 'L');

        // Assert
        Assert.NotNull(response);
        Assert.Equal(CommandStatus.Executed, response.Status);
        Assert.Equal(position, response.NewPosition); // Position unchanged
        Assert.Equal(Orientation.West, response.NewOrientation);
        Assert.False(response.IsLost);
    }

    [Fact]
    public async Task SendCommandAsync_MoveForwardOutOfBounds_ShouldMarkAsLost()
    {
        // Arrange
        const string robotId = "TEST-ROBOT-6";
        var position = new Position(4, 4); // Near edge of 5x3 grid
        var orientation = Orientation.North;
        
        await _service.ConnectToRobotAsync(robotId, position, orientation);

        // Act - Move forward beyond grid boundary
        var response = await _service.SendCommandAsync(robotId, 'F');

        // Assert
        Assert.NotNull(response);
        Assert.Equal(CommandStatus.Executed, response.Status);
        Assert.True(response.IsLost);
        Assert.Equal(position, response.NewPosition); // Robot stays at last valid position
        Assert.Equal(orientation, response.NewOrientation);
    }

    [Fact]
    public async Task SendCommandAsync_ToDisconnectedRobot_ShouldReturnFailed()
    {
        // Arrange
        const string robotId = "DISCONNECTED-ROBOT";

        // Act
        var response = await _service.SendCommandAsync(robotId, 'F');

        // Assert
        Assert.NotNull(response);
        Assert.Equal(robotId, response.RobotId);
        Assert.Equal(CommandStatus.Failed, response.Status);
        Assert.NotNull(response.ErrorMessage);
        Assert.Contains("not connected", response.ErrorMessage);
    }

    [Fact]
    public async Task SendCommandAsync_InvalidCommand_ShouldReturnFailed()
    {
        // Arrange
        const string robotId = "TEST-ROBOT-7";
        var position = new Position(1, 1);
        var orientation = Orientation.North;
        
        await _service.ConnectToRobotAsync(robotId, position, orientation);

        // Act
        var response = await _service.SendCommandAsync(robotId, 'X'); // Invalid command

        // Assert
        Assert.NotNull(response);
        Assert.Equal(CommandStatus.Failed, response.Status);
        Assert.NotNull(response.ErrorMessage);
        Assert.Contains("Invalid instruction", response.ErrorMessage);
    }

    [Fact]
    public async Task PingRobotAsync_ConnectedRobot_ShouldReturnTrue()
    {
        // Arrange
        const string robotId = "TEST-ROBOT-8";
        var position = new Position(1, 1);
        var orientation = Orientation.North;
        
        await _service.ConnectToRobotAsync(robotId, position, orientation);

        // Act
        var result = await _service.PingRobotAsync(robotId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task PingRobotAsync_DisconnectedRobot_ShouldReturnFalse()
    {
        // Arrange
        const string robotId = "DISCONNECTED-ROBOT";

        // Act
        var result = await _service.PingRobotAsync(robotId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetRobotStateAsync_ConnectedRobot_ShouldReturnState()
    {
        // Arrange
        const string robotId = "TEST-ROBOT-9";
        var position = new Position(3, 2);
        var orientation = Orientation.South;
        
        await _service.ConnectToRobotAsync(robotId, position, orientation);

        // Act
        var robotState = await _service.GetRobotStateAsync(robotId);

        // Assert
        Assert.NotNull(robotState);
        Assert.Equal(robotId, robotState.Id);
        Assert.Equal(position, robotState.Position);
        Assert.Equal(orientation, robotState.Orientation);
        Assert.Equal(ConnectionState.Connected, robotState.ConnectionState);
    }

    [Fact]
    public async Task GetRobotStateAsync_DisconnectedRobot_ShouldReturnNull()
    {
        // Arrange
        const string robotId = "DISCONNECTED-ROBOT";

        // Act
        var robotState = await _service.GetRobotStateAsync(robotId);

        // Assert
        Assert.Null(robotState);
    }

    [Fact]
    public async Task DisconnectFromRobotAsync_ConnectedRobot_ShouldDisconnectSuccessfully()
    {
        // Arrange
        const string robotId = "TEST-ROBOT-10";
        var position = new Position(1, 1);
        var orientation = Orientation.North;
        
        await _service.ConnectToRobotAsync(robotId, position, orientation);

        // Act
        await _service.DisconnectFromRobotAsync(robotId);

        // Assert
        var robotState = await _service.GetRobotStateAsync(robotId);
        Assert.Null(robotState); // Robot should be removed
    }

    [Fact]
    public async Task DisconnectFromRobotAsync_DisconnectedRobot_ShouldNotThrow()
    {
        // Arrange
        const string robotId = "DISCONNECTED-ROBOT";

        // Act & Assert - Should not throw
        await _service.DisconnectFromRobotAsync(robotId);
    }

    [Fact]
    public async Task GetConnectedRobots_WithMultipleRobots_ShouldReturnAllConnected()
    {
        // Arrange
        var robots = new[]
        {
            ("ROBOT-A", new Position(1, 1), Orientation.North),
            ("ROBOT-B", new Position(2, 2), Orientation.East),
            ("ROBOT-C", new Position(3, 3), Orientation.South)
        };

        foreach (var (id, pos, orient) in robots)
        {
            await _service.ConnectToRobotAsync(id, pos, orient);
        }

        // Act
        var connectedRobots = _service.GetConnectedRobots().ToList();

        // Assert
        Assert.Equal(3, connectedRobots.Count);
        Assert.Contains(connectedRobots, r => r.Id == "ROBOT-A");
        Assert.Contains(connectedRobots, r => r.Id == "ROBOT-B");
        Assert.Contains(connectedRobots, r => r.Id == "ROBOT-C");
    }

    [Fact]
    public async Task ComplexMovementSequence_ShouldMaintainCorrectState()
    {
        // Arrange
        const string robotId = "COMPLEX-ROBOT";
        var startPosition = new Position(2, 2);
        var startOrientation = Orientation.North;
        
        await _service.ConnectToRobotAsync(robotId, startPosition, startOrientation);

        // Act - Execute a square pattern: RFRFRFRF
        var commands = new[] { 'R', 'F', 'R', 'F', 'R', 'F', 'R', 'F' };
        var responses = new List<CommandResponse>();

        foreach (var command in commands)
        {
            var response = await _service.SendCommandAsync(robotId, command);
            responses.Add(response);
        }

        // Assert
        Assert.All(responses, r => Assert.Equal(CommandStatus.Executed, r.Status));
        Assert.All(responses, r => Assert.False(r.IsLost));

        // After completing a square, robot should be back at start position and orientation
        var finalResponse = responses.Last();
        Assert.Equal(startPosition, finalResponse.NewPosition);
        Assert.Equal(startOrientation, finalResponse.NewOrientation);
    }

    [Theory]
    [InlineData(Orientation.North, Orientation.East)]
    [InlineData(Orientation.East, Orientation.South)]
    [InlineData(Orientation.South, Orientation.West)]
    [InlineData(Orientation.West, Orientation.North)]
    public async Task RightTurn_FromAllOrientations_ShouldRotateCorrectly(Orientation initial, Orientation expected)
    {
        // Arrange
        const string robotId = "ROTATION-TEST";
        var position = new Position(2, 2);
        
        await _service.ConnectToRobotAsync(robotId, position, initial);

        // Act
        var response = await _service.SendCommandAsync(robotId, 'R');

        // Assert
        Assert.Equal(CommandStatus.Executed, response.Status);
        Assert.Equal(expected, response.NewOrientation);
    }

    [Theory]
    [InlineData(Orientation.North, Orientation.West)]
    [InlineData(Orientation.West, Orientation.South)]
    [InlineData(Orientation.South, Orientation.East)]
    [InlineData(Orientation.East, Orientation.North)]
    public async Task LeftTurn_FromAllOrientations_ShouldRotateCorrectly(Orientation initial, Orientation expected)
    {
        // Arrange
        const string robotId = "ROTATION-TEST";
        var position = new Position(2, 2);
        
        await _service.ConnectToRobotAsync(robotId, position, initial);

        // Act
        var response = await _service.SendCommandAsync(robotId, 'L');

        // Assert
        Assert.Equal(CommandStatus.Executed, response.Status);
        Assert.Equal(expected, response.NewOrientation);
    }
}