namespace MartianRobots.Tests.Models;

public class RobotTests
{
    [Fact]
    public void Constructor_ShouldSetInitialState()
    {
        // Arrange
        var position = new Position(1, 2);
        var orientation = Orientation.East;

        var robot = new Robot(position, orientation);

        robot.Position.Should().Be(position);
        robot.Orientation.Should().Be(orientation);
        robot.IsLost.Should().BeFalse();
    }

    [Fact]
    public void TurnLeft_ShouldChangeOrientation()
    {
        // Arrange
        var robot = new Robot(new Position(0, 0), Orientation.North);

        robot.TurnLeft();

        robot.Orientation.Should().Be(Orientation.West);
        robot.Position.Should().Be(new Position(0, 0));
        robot.IsLost.Should().BeFalse();
    }

    [Fact]
    public void TurnRight_ShouldChangeOrientation()
    {
        // Arrange
        var robot = new Robot(new Position(0, 0), Orientation.North);

        robot.TurnRight();

        robot.Orientation.Should().Be(Orientation.East);
        robot.Position.Should().Be(new Position(0, 0));
        robot.IsLost.Should().BeFalse();
    }

    [Fact]
    public void TurnLeft_WhenLost_ShouldNotChangeOrientation()
    {
        // Arrange
        var robot = new Robot(new Position(0, 0), Orientation.North);
        robot.MarkAsLost();

        robot.TurnLeft();

        robot.Orientation.Should().Be(Orientation.North);
        robot.IsLost.Should().BeTrue();
    }

    [Fact]
    public void TurnRight_WhenLost_ShouldNotChangeOrientation()
    {
        // Arrange
        var robot = new Robot(new Position(0, 0), Orientation.North);
        robot.MarkAsLost();

        robot.TurnRight();

        robot.Orientation.Should().Be(Orientation.North);
        robot.IsLost.Should().BeTrue();
    }

    [Theory]
    [InlineData(Orientation.North, 1, 2)]
    [InlineData(Orientation.East, 2, 1)]
    [InlineData(Orientation.South, 1, 0)]
    [InlineData(Orientation.West, 0, 1)]
    public void TryMoveForward_ShouldReturnCorrectNewPosition(Orientation orientation, int expectedX, int expectedY)
    {
        // Arrange
        var robot = new Robot(new Position(1, 1), orientation);

        var result = robot.TryMoveForward(out var newPosition);

        result.Should().BeTrue();
        newPosition.Should().Be(new Position(expectedX, expectedY));
        robot.Position.Should().Be(new Position(1, 1)); // Original position unchanged
    }

    [Fact]
    public void TryMoveForward_WhenLost_ShouldReturnFalse()
    {
        // Arrange
        var robot = new Robot(new Position(1, 1), Orientation.North);
        robot.MarkAsLost();

        var result = robot.TryMoveForward(out var newPosition);

        result.Should().BeFalse();
        newPosition.Should().Be(new Position(1, 1)); // Same as current position
    }

    [Fact]
    public void UpdatePosition_ShouldChangePosition()
    {
        // Arrange
        var robot = new Robot(new Position(1, 1), Orientation.North);
        var newPosition = new Position(2, 3);

        robot.UpdatePosition(newPosition);

        robot.Position.Should().Be(newPosition);
    }

    [Fact]
    public void UpdatePosition_WhenLost_ShouldNotChangePosition()
    {
        // Arrange
        var robot = new Robot(new Position(1, 1), Orientation.North);
        robot.MarkAsLost();
        var newPosition = new Position(2, 3);

        robot.UpdatePosition(newPosition);

        robot.Position.Should().Be(new Position(1, 1)); // Original position unchanged
    }

    [Fact]
    public void MarkAsLost_ShouldSetIsLostToTrue()
    {
        // Arrange
        var robot = new Robot(new Position(1, 1), Orientation.North);

        robot.MarkAsLost();

        robot.IsLost.Should().BeTrue();
    }

    [Fact]
    public void ToString_WhenNotLost_ShouldReturnCorrectFormat()
    {
        // Arrange
        var robot = new Robot(new Position(2, 3), Orientation.East);

        var result = robot.ToString();

        result.Should().Be("2 3 E");
    }

    [Fact]
    public void ToString_WhenLost_ShouldReturnCorrectFormat()
    {
        // Arrange
        var robot = new Robot(new Position(2, 3), Orientation.East);
        robot.MarkAsLost();

        var result = robot.ToString();

        result.Should().Be("2 3 E LOST");
    }
}