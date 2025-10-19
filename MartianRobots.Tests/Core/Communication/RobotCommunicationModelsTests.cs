namespace MartianRobots.Tests.Core.Communication;

public class RobotCommunicationModelsTests
{
    [Fact]
    public void RobotInstance_DefaultValues_ShouldBeSetCorrectly()
    {
        // Act
        var robot = new RobotInstance();

        // Assert
        Assert.Equal(string.Empty, robot.Id);
        Assert.Equal(default(Position), robot.Position);
        Assert.Equal(default(Orientation), robot.Orientation);
        Assert.False(robot.IsLost);
        Assert.Equal(ConnectionState.Disconnected, robot.ConnectionState);
        Assert.Null(robot.LastError);
        Assert.Equal(0, robot.FailedCommandCount);
    }

    [Fact]
    public void RobotInstance_InitWithValues_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        const string robotId = "MARS-ROVER-1";
        var position = new Position(2, 3);
        var orientation = Orientation.East;

        // Act
        var robot = new RobotInstance
        {
            Id = robotId,
            Position = position,
            Orientation = orientation,
            IsLost = true,
            ConnectionState = ConnectionState.Connected,
            LastError = "Test error",
            FailedCommandCount = 5
        };

        // Assert
        Assert.Equal(robotId, robot.Id);
        Assert.Equal(position, robot.Position);
        Assert.Equal(orientation, robot.Orientation);
        Assert.True(robot.IsLost);
        Assert.Equal(ConnectionState.Connected, robot.ConnectionState);
        Assert.Equal("Test error", robot.LastError);
        Assert.Equal(5, robot.FailedCommandCount);
    }

    [Fact]
    public void CommandResponse_DefaultValues_ShouldBeSetCorrectly()
    {
        // Act
        var response = new CommandResponse();

        // Assert
        Assert.Equal(string.Empty, response.CommandId);
        Assert.Equal(string.Empty, response.RobotId);
        Assert.Equal(default(CommandStatus), response.Status);
        Assert.Null(response.NewPosition);
        Assert.Null(response.NewOrientation);
        Assert.False(response.IsLost);
        Assert.Null(response.ErrorMessage);
        Assert.True((DateTime.UtcNow - response.ResponseTime).TotalSeconds < 1); // Should be recent
        Assert.Equal(default(TimeSpan), response.ProcessingTime);
    }

    [Fact]
    public void CommandResponse_InitWithValues_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        const string commandId = "response-test-456";
        const string robotId = "MARS-ROVER-3";
        var newPosition = new Position(4, 2);
        var newOrientation = Orientation.West;
        var responseTime = DateTime.UtcNow.AddSeconds(-2);
        var processingTime = TimeSpan.FromMilliseconds(500);

        // Act
        var response = new CommandResponse
        {
            CommandId = commandId,
            RobotId = robotId,
            Status = CommandStatus.Executed,
            NewPosition = newPosition,
            NewOrientation = newOrientation,
            IsLost = true,
            ErrorMessage = "Test response error",
            ResponseTime = responseTime,
            ProcessingTime = processingTime
        };

        // Assert
        Assert.Equal(commandId, response.CommandId);
        Assert.Equal(robotId, response.RobotId);
        Assert.Equal(CommandStatus.Executed, response.Status);
        Assert.Equal(newPosition, response.NewPosition);
        Assert.Equal(newOrientation, response.NewOrientation);
        Assert.True(response.IsLost);
        Assert.Equal("Test response error", response.ErrorMessage);
        Assert.Equal(responseTime, response.ResponseTime);
        Assert.Equal(processingTime, response.ProcessingTime);
    }

    [Theory]
    [InlineData(ConnectionState.Disconnected)]
    [InlineData(ConnectionState.Connecting)]
    [InlineData(ConnectionState.Connected)]
    [InlineData(ConnectionState.Unstable)]
    [InlineData(ConnectionState.Lost)]
    public void ConnectionState_AllValues_ShouldBeValid(ConnectionState state)
    {
        // Act
        var robot = new RobotInstance { ConnectionState = state };

        // Assert
        Assert.Equal(state, robot.ConnectionState);
    }

    [Theory]
    [InlineData(CommandStatus.Executed)]
    [InlineData(CommandStatus.Failed)]
    [InlineData(CommandStatus.TimedOut)]
    public void CommandStatus_AllValues_ShouldBeValid(CommandStatus status)
    {
        // Act
        var response = new CommandResponse { Status = status };

        // Assert
        Assert.Equal(status, response.Status);
    }

    [Fact]
    public void RobotCommunicationOptions_DefaultValues_ShouldBeSetCorrectly()
    {
        // Act
        var options = new RobotCommunicationOptions();

        // Assert
        Assert.Equal(TimeSpan.FromMilliseconds(500), options.BaseDelay);
        Assert.Equal(TimeSpan.FromSeconds(1), options.MaxRandomDelay);
        Assert.Equal(0.1, options.FailureProbability);
        Assert.Equal(TimeSpan.FromSeconds(10), options.CommandTimeout);
        Assert.Equal(3, options.MaxRetryAttempts);
        Assert.Equal(5, options.CircuitBreakerThreshold);
        Assert.Equal(TimeSpan.FromSeconds(30), options.CircuitBreakerSamplingDuration);
        Assert.Equal(10, options.CircuitBreakerMinimumThroughput);
    }

    [Fact]
    public void RobotCommunicationOptions_SetCustomValues_ShouldUpdateCorrectly()
    {
        // Arrange
        var customBaseDelay = TimeSpan.FromMilliseconds(1000);
        var customMaxDelay = TimeSpan.FromSeconds(2);
        const double customFailureProbability = 0.2;
        var customTimeout = TimeSpan.FromSeconds(15);
        const int customRetries = 5;
        const int customThreshold = 10;
        var customSamplingDuration = TimeSpan.FromMinutes(1);
        const int customThroughput = 20;

        // Act
        var options = new RobotCommunicationOptions
        {
            BaseDelay = customBaseDelay,
            MaxRandomDelay = customMaxDelay,
            FailureProbability = customFailureProbability,
            CommandTimeout = customTimeout,
            MaxRetryAttempts = customRetries,
            CircuitBreakerThreshold = customThreshold,
            CircuitBreakerSamplingDuration = customSamplingDuration,
            CircuitBreakerMinimumThroughput = customThroughput
        };

        // Assert
        Assert.Equal(customBaseDelay, options.BaseDelay);
        Assert.Equal(customMaxDelay, options.MaxRandomDelay);
        Assert.Equal(customFailureProbability, options.FailureProbability);
        Assert.Equal(customTimeout, options.CommandTimeout);
        Assert.Equal(customRetries, options.MaxRetryAttempts);
        Assert.Equal(customThreshold, options.CircuitBreakerThreshold);
        Assert.Equal(customSamplingDuration, options.CircuitBreakerSamplingDuration);
        Assert.Equal(customThroughput, options.CircuitBreakerMinimumThroughput);
    }

    [Fact]
    public void RobotCommunicationOptions_SectionName_ShouldBeCorrect()
    {
        // Assert
        Assert.Equal("RobotCommunication", RobotCommunicationOptions.SectionName);
    }
}