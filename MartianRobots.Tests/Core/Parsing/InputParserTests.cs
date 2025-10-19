using MartianRobots.Core.Parsing;

namespace MartianRobots.Tests.Core.Parsing;

public class InputParserTests
{
    [Theory]
    [InlineData("5 3", 5, 3)]
    [InlineData("0 0", 0, 0)]
    [InlineData("50 50", 50, 50)]
    [InlineData("  10   20  ", 10, 20)] // With whitespace
    public void ParseGrid_WithValidInput_ShouldReturnCorrectGrid(string gridLine, int expectedMaxX, int expectedMaxY)
    {
        var grid = InputParser.ParseGrid(gridLine, null);

        grid.IsValidPosition(new Position(expectedMaxX, expectedMaxY)).Should().BeTrue();
        grid.IsValidPosition(new Position(expectedMaxX + 1, expectedMaxY)).Should().BeFalse();
        grid.IsValidPosition(new Position(expectedMaxX, expectedMaxY + 1)).Should().BeFalse();
    }

    [Theory]
    [InlineData("abc def")]
    [InlineData("5")]
    [InlineData("5 3 1")]
    public void ParseGrid_WithInvalidInput_ShouldThrowException(string gridLine)
    {
        var action = () => InputParser.ParseGrid(gridLine, null);
        action.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("1 2 N", 1, 2, Orientation.North)]
    [InlineData("0 0 E", 0, 0, Orientation.East)]
    [InlineData("5 3 S", 5, 3, Orientation.South)]
    [InlineData("10 20 W", 10, 20, Orientation.West)]
    [InlineData("  1   2   N  ", 1, 2, Orientation.North)] // With whitespace
    public void ParseRobot_WithValidInput_ShouldReturnCorrectRobot(
        string positionLine, int expectedX, int expectedY, Orientation expectedOrientation)
    {
        var robot = InputParser.ParseRobot(positionLine, null);

        robot.Position.Should().Be(new Position(expectedX, expectedY));
        robot.Orientation.Should().Be(expectedOrientation);
        robot.IsLost.Should().BeFalse();
    }

    [Theory]
    [InlineData("abc 2 N")]
    [InlineData("1 abc N")]
    [InlineData("1 2")]
    [InlineData("1 2 N E")]
    [InlineData("1 2 X")]
    public void ParseRobot_WithInvalidInput_ShouldThrowException(string positionLine)
    {
        var action = () => InputParser.ParseRobot(positionLine, null);
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ParseGrid_ShouldValidateInput()
    {
        // This test ensures that ParseGrid calls the validator
        // We can verify this by checking that invalid input throws an exception

        var action = () => InputParser.ParseGrid("invalid", null);
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ParseRobot_ShouldValidateInput()
    {
        // This test ensures that ParseRobot calls the validator
        // We can verify this by checking that invalid input throws an exception

        var action = () => InputParser.ParseRobot("invalid", null);
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ParseGrid_WithNegativeCoordinates_ShouldThrowWhenCreatingGrid()
    {
        // Note: This tests that even if the parsing succeeds, 
        // the MarsGrid constructor will validate the values

        var action = () => InputParser.ParseGrid("-1 5", null);
        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ParseGrid_WithLargeCoordinates_ShouldThrowWhenCreatingGrid()
    {
        // Note: This tests that even if the parsing succeeds, 
        // the MarsGrid constructor will validate the values

        var action = () => InputParser.ParseGrid("51 5", null);
        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData("1 2 n")] // lowercase
    [InlineData("1 2 northeast")] // too long
    public void ParseRobot_WithInvalidOrientation_ShouldThrow(string positionLine)
    {
        var action = () => InputParser.ParseRobot(positionLine, null);
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ParseRobot_WithValidData_ShouldCreateRobotWithCorrectInitialState()
    {
        // Arrange
        var positionLine = "3 4 E";

        var robot = InputParser.ParseRobot(positionLine, null);

        robot.Position.X.Should().Be(3);
        robot.Position.Y.Should().Be(4);
        robot.Orientation.Should().Be(Orientation.East);
        robot.IsLost.Should().BeFalse();
        robot.ToString().Should().Be("3 4 E");
    }
}