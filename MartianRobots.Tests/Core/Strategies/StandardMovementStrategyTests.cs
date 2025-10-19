using MartianRobots.Core.Strategies;

namespace MartianRobots.Tests.Core.Strategies;

public class StandardMovementStrategyTests
{
    [Fact]
    public void TryMove_WithinGrid_ShouldMoveRobotAndReturnTrue()
    {
        // Arrange
        var strategy = new StandardMovementStrategy();
        var robot = new Robot(new Position(1, 1), Orientation.North);
        var grid = new MarsGrid(5, 5);

        var result = strategy.TryMove(robot, grid);

        result.Should().BeTrue();
        robot.Position.Should().Be(new Position(1, 2));
        robot.IsLost.Should().BeFalse();
    }

    [Fact]
    public void TryMove_OutsideGridWithoutScent_ShouldMarkLostAndReturnFalse()
    {
        // Arrange
        var strategy = new StandardMovementStrategy();
        var robot = new Robot(new Position(5, 5), Orientation.North);
        var grid = new MarsGrid(5, 5);

        var result = strategy.TryMove(robot, grid);

        result.Should().BeFalse();
        robot.Position.Should().Be(new Position(5, 5)); // Position unchanged
        robot.IsLost.Should().BeTrue();
        grid.HasScent(new Position(5, 5)).Should().BeTrue(); // Scent added
    }

    [Fact]
    public void TryMove_OutsideGridWithScent_ShouldIgnoreAndReturnTrue()
    {
        // Arrange
        var strategy = new StandardMovementStrategy();
        var robot = new Robot(new Position(5, 5), Orientation.North);
        var grid = new MarsGrid(5, 5);
        grid.AddScent(new Position(5, 5)); // Pre-existing scent

        var result = strategy.TryMove(robot, grid);

        result.Should().BeTrue();
        robot.Position.Should().Be(new Position(5, 5)); // Position unchanged
        robot.IsLost.Should().BeFalse(); // Not lost due to scent
    }

    [Fact]
    public void TryMove_LostRobot_ShouldReturnFalse()
    {
        // Arrange
        var strategy = new StandardMovementStrategy();
        var robot = new Robot(new Position(1, 1), Orientation.North);
        robot.MarkAsLost();
        var grid = new MarsGrid(5, 5);

        var result = strategy.TryMove(robot, grid);

        result.Should().BeFalse();
        robot.Position.Should().Be(new Position(1, 1)); // Position unchanged
    }

    [Theory]
    [InlineData(Orientation.North, 0, 5)] // Moving north from top edge
    [InlineData(Orientation.East, 5, 0)]  // Moving east from right edge
    [InlineData(Orientation.South, 0, 0)] // Moving south from bottom edge
    [InlineData(Orientation.West, 0, 0)]  // Moving west from left edge
    public void TryMove_FromBoundaryInEachDirection_ShouldHandleCorrectly(
        Orientation orientation, int startX, int startY)
    {
        // Arrange
        var strategy = new StandardMovementStrategy();
        var robot = new Robot(new Position(startX, startY), orientation);
        var grid = new MarsGrid(5, 5);

        var result = strategy.TryMove(robot, grid);

        result.Should().BeFalse(); // Should fail to move outside grid
        robot.Position.Should().Be(new Position(startX, startY)); // Position unchanged
        robot.IsLost.Should().BeTrue();
        grid.HasScent(new Position(startX, startY)).Should().BeTrue();
    }

    [Fact]
    public void TryMove_MultipleRobotsAtSamePosition_ShouldRespectScent()
    {
        // Arrange
        var strategy = new StandardMovementStrategy();
        var grid = new MarsGrid(5, 5);

        // First robot falls off and leaves scent
        var robot1 = new Robot(new Position(5, 5), Orientation.North);
        var result1 = strategy.TryMove(robot1, grid);

        // Second robot tries same move
        var robot2 = new Robot(new Position(5, 5), Orientation.North);

        var result2 = strategy.TryMove(robot2, grid);

        result1.Should().BeFalse(); // First robot lost
        robot1.IsLost.Should().BeTrue();

        result2.Should().BeTrue(); // Second robot saved by scent
        robot2.IsLost.Should().BeFalse();
        robot2.Position.Should().Be(new Position(5, 5)); // Stayed in place
    }

    [Fact]
    public void TryMove_EdgeCases_ShouldHandleZeroCoordinates()
    {
        // Arrange
        var strategy = new StandardMovementStrategy();
        var robot = new Robot(new Position(0, 0), Orientation.East);
        var grid = new MarsGrid(5, 5);

        var result = strategy.TryMove(robot, grid);

        result.Should().BeTrue();
        robot.Position.Should().Be(new Position(1, 0));
        robot.IsLost.Should().BeFalse();
    }

    [Fact]
    public void TryMove_FromScentedPositionToValidPosition_ShouldMoveNormally()
    {
        // Arrange
        var strategy = new StandardMovementStrategy();
        var grid = new MarsGrid(5, 5);
        
        // Add scent at position (5, 5) - simulating a previous robot fell off when moving north
        grid.AddScent(new Position(5, 5));
        
        // Place robot at the scented position but facing East (toward valid position)
        var robot = new Robot(new Position(5, 5), Orientation.East);

        // Act - Robot should move east to (6, 5)... wait, that's outside!
        // Let me fix: robot at (4, 4) with scent, facing East to valid (5, 4)
        robot = new Robot(new Position(4, 4), Orientation.East);
        grid.AddScent(new Position(4, 4)); // Add scent at robot's position
        
        var result = strategy.TryMove(robot, grid);

        // Assert - Robot should move normally to valid position despite being on scented position
        result.Should().BeTrue();
        robot.Position.Should().Be(new Position(5, 4)); // Moved to valid position
        robot.IsLost.Should().BeFalse();
    }

    [Fact]
    public void TryMove_FromScentedPositionToInvalidPosition_ShouldStayInPlace()
    {
        // Arrange
        var strategy = new StandardMovementStrategy();
        var grid = new MarsGrid(5, 5);
        
        // Robot at edge position with pre-existing scent, facing off-grid
        var robot = new Robot(new Position(5, 5), Orientation.North);
        grid.AddScent(new Position(5, 5));
        
        var result = strategy.TryMove(robot, grid);

        // Assert - Robot should stay in place due to scent preventing fall-off
        result.Should().BeTrue();
        robot.Position.Should().Be(new Position(5, 5)); // Stayed in place
        robot.IsLost.Should().BeFalse(); // Protected by scent
    }
}