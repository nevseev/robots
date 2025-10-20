using Microsoft.Extensions.Logging;
using MartianRobots.Core.Strategies;

namespace MartianRobots.Tests.Core.Strategies;

/// <summary>
/// Tests for MovementStrategyBase abstract class to ensure logger coverage
/// Uses StandardMovementStrategy as concrete implementation for testing
/// </summary>
public class MovementStrategyBaseTests
{
    [Fact]
    public void TryMove_WithLogger_ShouldLogAttempt()
    {
        // Arrange
        var strategy = new StandardMovementStrategy();
        var robot = new Robot(new Position(2, 2), Orientation.North);
        var grid = new MarsGrid(5, 5);
        var mockLogger = new Mock<ILogger>();

        // Act
        strategy.TryMove(robot, grid, mockLogger.Object);

        // Assert - Verify debug logging for movement attempt
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Attempting to move robot")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "Should log movement attempt");
    }

    [Fact]
    public void TryMove_SuccessfulMove_ShouldLogSuccess()
    {
        // Arrange
        var strategy = new StandardMovementStrategy();
        var robot = new Robot(new Position(2, 2), Orientation.North);
        var grid = new MarsGrid(5, 5);
        var mockLogger = new Mock<ILogger>();

        // Act
        var result = strategy.TryMove(robot, grid, mockLogger.Object);

        // Assert
        result.Should().BeTrue();
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("successfully moved to position")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "Should log successful movement");
    }

    [Fact]
    public void TryMove_LostRobot_ShouldLogCannotMoveForward()
    {
        // Arrange
        var strategy = new StandardMovementStrategy();
        var robot = new Robot(new Position(2, 2), Orientation.North);
        robot.MarkAsLost(); // Lost robots can't move
        var grid = new MarsGrid(5, 5);
        var mockLogger = new Mock<ILogger>();

        // Act
        var result = strategy.TryMove(robot, grid, mockLogger.Object);

        // Assert
        result.Should().BeFalse();
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("cannot move forward")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "Should log that robot cannot move forward");
    }

    [Fact]
    public void TryMove_TargetOutsideGrid_ShouldLogBoundaryCollision()
    {
        // Arrange
        var strategy = new StandardMovementStrategy();
        var robot = new Robot(new Position(5, 5), Orientation.North);
        var grid = new MarsGrid(5, 5);
        var mockLogger = new Mock<ILogger>();

        // Act
        var result = strategy.TryMove(robot, grid, mockLogger.Object);

        // Assert
        result.Should().BeFalse();
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("outside grid boundaries")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "Should log boundary collision");
    }

    [Fact]
    public void TryMove_RobotLostAtBoundary_ShouldLogWarning()
    {
        // Arrange
        var strategy = new StandardMovementStrategy();
        var robot = new Robot(new Position(5, 5), Orientation.North);
        var grid = new MarsGrid(5, 5);
        var mockLogger = new Mock<ILogger>();

        // Act
        var result = strategy.TryMove(robot, grid, mockLogger.Object);

        // Assert
        result.Should().BeFalse();
        robot.IsLost.Should().BeTrue();
        // Note: There are 2 warning logs - one from StandardMovementStrategy.HandleBoundaryCollision
        // and one from MovementStrategyBase.TryMove
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Robot lost")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeast(1),
            "Should log warning when robot is lost");
    }

    [Fact]
    public void TryMove_BoundaryCollisionWithScent_ShouldLogSuccess()
    {
        // Arrange
        var strategy = new StandardMovementStrategy();
        var robot = new Robot(new Position(5, 5), Orientation.North);
        var grid = new MarsGrid(5, 5);
        grid.AddScent(new Position(5, 5)); // Pre-existing scent
        var mockLogger = new Mock<ILogger>();

        // Act
        var result = strategy.TryMove(robot, grid, mockLogger.Object);

        // Assert
        result.Should().BeTrue();
        robot.IsLost.Should().BeFalse();
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Boundary collision handled successfully")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "Should log successful boundary collision handling");
    }

    [Fact]
    public void TryMove_TargetPosition_ShouldLogTargetPosition()
    {
        // Arrange
        var strategy = new StandardMovementStrategy();
        var robot = new Robot(new Position(2, 2), Orientation.East);
        var grid = new MarsGrid(5, 5);
        var mockLogger = new Mock<ILogger>();

        // Act
        strategy.TryMove(robot, grid, mockLogger.Object);

        // Assert
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Target position for robot movement")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "Should log target position");
    }

    [Fact]
    public void TryMove_WithoutLogger_ShouldExecuteSuccessfully()
    {
        // Arrange
        var strategy = new StandardMovementStrategy();
        var robot = new Robot(new Position(2, 2), Orientation.North);
        var grid = new MarsGrid(5, 5);

        // Act - No logger provided (null)
        var result = strategy.TryMove(robot, grid, null);

        // Assert - Should work without logger
        result.Should().BeTrue();
        robot.Position.Should().Be(new Position(2, 3));
    }

    [Theory]
    [InlineData(Orientation.North)]
    [InlineData(Orientation.East)]
    [InlineData(Orientation.South)]
    [InlineData(Orientation.West)]
    public void TryMove_AllOrientations_ShouldLogCorrectOrientation(Orientation orientation)
    {
        // Arrange
        var strategy = new StandardMovementStrategy();
        var robot = new Robot(new Position(2, 2), orientation);
        var grid = new MarsGrid(5, 5);
        var mockLogger = new Mock<ILogger>();

        // Act
        strategy.TryMove(robot, grid, mockLogger.Object);

        // Assert
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(orientation.ToString())),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce,
            $"Should log orientation {orientation}");
    }

    [Fact]
    public void TryMove_EdgePosition_ShouldLogBoundaryHandling()
    {
        // Arrange
        var strategy = new StandardMovementStrategy();
        var robot = new Robot(new Position(0, 0), Orientation.South); // At edge, moving off
        var grid = new MarsGrid(5, 5);
        var mockLogger = new Mock<ILogger>();

        // Act
        var result = strategy.TryMove(robot, grid, mockLogger.Object);

        // Assert
        result.Should().BeFalse();
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("handling boundary collision")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "Should log boundary collision handling");
    }
}
