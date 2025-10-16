namespace MartianRobots.Tests.Models;

public class OrientationTests
{
    [Theory]
    [InlineData(Orientation.North, "N")]
    [InlineData(Orientation.East, "E")]
    [InlineData(Orientation.South, "S")]
    [InlineData(Orientation.West, "W")]
    public void ToChar_ShouldReturnCorrectCharacter(Orientation orientation, string expected)
    {
        var result = orientation.ToChar();
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData('N', Orientation.North)]
    [InlineData('E', Orientation.East)]
    [InlineData('S', Orientation.South)]
    [InlineData('W', Orientation.West)]
    public void FromChar_ShouldReturnCorrectOrientation(char input, Orientation expected)
    {
        var result = OrientationExtensions.FromChar(input);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData('X')]
    [InlineData('1')]
    [InlineData(' ')]
    public void FromChar_ShouldThrowForInvalidCharacter(char invalidChar)
    {
        var action = () => OrientationExtensions.FromChar(invalidChar);
        action.Should().Throw<ArgumentException>()
            .WithMessage($"Invalid orientation character: {invalidChar}");
    }

    [Theory]
    [InlineData(Orientation.North, Orientation.West)]
    [InlineData(Orientation.East, Orientation.North)]
    [InlineData(Orientation.South, Orientation.East)]
    [InlineData(Orientation.West, Orientation.South)]
    public void TurnLeft_ShouldReturnCorrectOrientation(Orientation input, Orientation expected)
    {
        var result = input.TurnLeft();
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(Orientation.North, Orientation.East)]
    [InlineData(Orientation.East, Orientation.South)]
    [InlineData(Orientation.South, Orientation.West)]
    [InlineData(Orientation.West, Orientation.North)]
    public void TurnRight_ShouldReturnCorrectOrientation(Orientation input, Orientation expected)
    {
        var result = input.TurnRight();
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(Orientation.North, 0, 1)]
    [InlineData(Orientation.East, 1, 0)]
    [InlineData(Orientation.South, 0, -1)]
    [InlineData(Orientation.West, -1, 0)]
    public void GetMovementDelta_ShouldReturnCorrectPosition(Orientation orientation, int expectedX, int expectedY)
    {
        var result = orientation.GetMovementDelta();
        result.Should().Be(new Position(expectedX, expectedY));
    }

    [Fact]
    public void TurnLeft_FourTimes_ShouldReturnToOriginal()
    {
        // Arrange
        var original = Orientation.North;

        var result = original.TurnLeft().TurnLeft().TurnLeft().TurnLeft();
        result.Should().Be(original);
    }

    [Fact]
    public void TurnRight_FourTimes_ShouldReturnToOriginal()
    {
        // Arrange
        var original = Orientation.South;

        var result = original.TurnRight().TurnRight().TurnRight().TurnRight();
        result.Should().Be(original);
    }
}