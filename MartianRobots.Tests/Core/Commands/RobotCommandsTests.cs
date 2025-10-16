using MartianRobots.Core.Commands;

namespace MartianRobots.Tests.Core.Commands;

public class RobotCommandsTests
{
    [Fact]
    public void TurnLeftCommand_ShouldTurnRobotLeft()
    {
        // Arrange
        var robot = new Robot(new Position(1, 1), Orientation.North);
        var grid = new MarsGrid(5, 5);
        var command = new TurnLeftCommand();

        command.Execute(robot, grid);

        robot.Orientation.Should().Be(Orientation.West);
        robot.Position.Should().Be(new Position(1, 1)); // Position unchanged
    }

    [Fact]
    public void TurnRightCommand_ShouldTurnRobotRight()
    {
        // Arrange
        var robot = new Robot(new Position(1, 1), Orientation.North);
        var grid = new MarsGrid(5, 5);
        var command = new TurnRightCommand();

        command.Execute(robot, grid);

        robot.Orientation.Should().Be(Orientation.East);
        robot.Position.Should().Be(new Position(1, 1)); // Position unchanged
    }

    [Fact]
    public void MoveForwardCommand_WithValidMove_ShouldMoveRobot()
    {
        // Arrange
        var robot = new Robot(new Position(1, 1), Orientation.North);
        var grid = new MarsGrid(5, 5);
        var command = new MoveForwardCommand();

        command.Execute(robot, grid);

        robot.Position.Should().Be(new Position(1, 2));
        robot.IsLost.Should().BeFalse();
    }

    [Fact]
    public void MoveForwardCommand_AtBoundaryWithoutScent_ShouldMarkRobotAsLost()
    {
        // Arrange
        var robot = new Robot(new Position(5, 5), Orientation.North);
        var grid = new MarsGrid(5, 5);
        var command = new MoveForwardCommand();

        command.Execute(robot, grid);

        robot.Position.Should().Be(new Position(5, 5)); // Position unchanged
        robot.IsLost.Should().BeTrue();
        grid.HasScent(new Position(5, 5)).Should().BeTrue();
    }

    [Fact]
    public void MoveForwardCommand_AtBoundaryWithScent_ShouldIgnoreCommand()
    {
        // Arrange
        var robot = new Robot(new Position(5, 5), Orientation.North);
        var grid = new MarsGrid(5, 5);
        grid.AddScent(new Position(5, 5)); // Add scent first
        var command = new MoveForwardCommand();

        command.Execute(robot, grid);

        robot.Position.Should().Be(new Position(5, 5)); // Position unchanged
        robot.IsLost.Should().BeFalse(); // Robot not lost due to scent
    }

    [Fact]
    public void TurnLeftCommand_OnLostRobot_ShouldNotChangeOrientation()
    {
        // Arrange
        var robot = new Robot(new Position(1, 1), Orientation.North);
        robot.MarkAsLost();
        var grid = new MarsGrid(5, 5);
        var command = new TurnLeftCommand();

        command.Execute(robot, grid);

        robot.Orientation.Should().Be(Orientation.North); // Unchanged
        robot.IsLost.Should().BeTrue();
    }

    [Fact]
    public void TurnRightCommand_OnLostRobot_ShouldNotChangeOrientation()
    {
        // Arrange
        var robot = new Robot(new Position(1, 1), Orientation.North);
        robot.MarkAsLost();
        var grid = new MarsGrid(5, 5);
        var command = new TurnRightCommand();

        command.Execute(robot, grid);

        robot.Orientation.Should().Be(Orientation.North); // Unchanged
        robot.IsLost.Should().BeTrue();
    }

    [Fact]
    public void MoveForwardCommand_OnLostRobot_ShouldNotMove()
    {
        // Arrange
        var robot = new Robot(new Position(1, 1), Orientation.North);
        robot.MarkAsLost();
        var grid = new MarsGrid(5, 5);
        var command = new MoveForwardCommand();

        command.Execute(robot, grid);

        robot.Position.Should().Be(new Position(1, 1)); // Unchanged
        robot.IsLost.Should().BeTrue();
    }
}