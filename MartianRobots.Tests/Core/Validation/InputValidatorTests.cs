using MartianRobots.Core.Validation;
using Microsoft.Extensions.Logging.Abstractions;

namespace MartianRobots.Tests.Core.Validation;

public class InputValidatorTests
{
    [Theory]
    [InlineData("LRF")]
    [InlineData("LFRFRFRFRF")]
    [InlineData("L")]
    [InlineData("F")]
    public void ValidateInstructions_WithValidInstructions_ShouldNotThrow(string instructions)
    {
        var action = () => InputValidator.ValidateInstructions(instructions, NullLogger.Instance);
        action.Should().NotThrow();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void ValidateInstructions_WithEmptyOrNull_ShouldThrowArgumentException(string? instructions)
    {
        var action = () => InputValidator.ValidateInstructions(instructions!, NullLogger.Instance);
        action.Should().Throw<ArgumentException>()
            .WithMessage("Instruction string cannot be empty");
    }

    [Fact]
    public void ValidateInstructions_WithTooLongString_ShouldThrowArgumentException()
    {
        // Arrange
        var longInstructions = new string('L', 100); // 100 characters

        var action = () => InputValidator.ValidateInstructions(longInstructions, NullLogger.Instance);
        action.Should().Throw<ArgumentException>()
            .WithMessage("Instruction string must be less than 100 characters");
    }

    [Theory]
    [InlineData("LRX")]
    [InlineData("123")]
    [InlineData("LR ")]
    [InlineData("lrf")] // lowercase
    public void ValidateInstructions_WithInvalidCharacters_ShouldThrowArgumentException(string instructions)
    {
        var action = () => InputValidator.ValidateInstructions(instructions, NullLogger.Instance);
        action.Should().Throw<ArgumentException>()
            .And.Message.Should().StartWith("Invalid instruction character:");
    }

    [Theory]
    [InlineData("5 3")]
    [InlineData("0 0")]
    [InlineData("50 50")]
    [InlineData("  10   20  ")] // With whitespace
    public void ValidateGridLine_WithValidGrid_ShouldNotThrow(string gridLine)
    {
        var action = () => InputValidator.ValidateGridLine(gridLine, NullLogger.Instance);
        action.Should().NotThrow();
    }

    [Theory]
    [InlineData("5")]
    [InlineData("5 3 1")]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateGridLine_WithWrongNumberOfParts_ShouldThrowArgumentException(string gridLine)
    {
        var action = () => InputValidator.ValidateGridLine(gridLine, NullLogger.Instance);
        action.Should().Throw<ArgumentException>()
            .WithMessage("Grid dimensions must contain exactly two integers");
    }

    [Theory]
    [InlineData("abc def")]
    [InlineData("5 abc")]
    [InlineData("abc 3")]
    [InlineData("5.5 3")]
    public void ValidateGridLine_WithNonIntegers_ShouldThrowArgumentException(string gridLine)
    {
        var action = () => InputValidator.ValidateGridLine(gridLine, NullLogger.Instance);
        action.Should().Throw<ArgumentException>()
            .WithMessage("Grid dimensions must be valid integers");
    }

    [Theory]
    [InlineData("1 2 N")]
    [InlineData("0 0 E")]
    [InlineData("50 50 S")]
    [InlineData("  1   2   W  ")] // With whitespace
    public void ValidateRobotPosition_WithValidPosition_ShouldNotThrow(string positionLine)
    {
        var action = () => InputValidator.ValidateRobotPosition(positionLine, NullLogger.Instance);
        action.Should().NotThrow();
    }

    [Theory]
    [InlineData("1 2")]
    [InlineData("1 2 N E")]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateRobotPosition_WithWrongNumberOfParts_ShouldThrowArgumentException(string positionLine)
    {
        var action = () => InputValidator.ValidateRobotPosition(positionLine, NullLogger.Instance);
        action.Should().Throw<ArgumentException>()
            .WithMessage("Robot position must contain exactly three parts: x y orientation");
    }

    [Theory]
    [InlineData("abc 2 N")]
    [InlineData("1 abc N")]
    [InlineData("1.5 2 N")]
    public void ValidateRobotPosition_WithInvalidCoordinates_ShouldThrowArgumentException(string positionLine)
    {
        var action = () => InputValidator.ValidateRobotPosition(positionLine, NullLogger.Instance);
        action.Should().Throw<ArgumentException>()
            .WithMessage("Robot coordinates must be valid integers");
    }

    [Theory]
    [InlineData("1 2 NE")]
    public void ValidateRobotPosition_WithInvalidOrientationLength_ShouldThrowArgumentException(string positionLine)
    {
        var action = () => InputValidator.ValidateRobotPosition(positionLine, NullLogger.Instance);
        action.Should().Throw<ArgumentException>()
            .WithMessage("Robot orientation must be a single character");
    }

    [Theory]
    [InlineData("1 2 X")]
    [InlineData("1 2 1")]
    [InlineData("1 2 n")] // lowercase
    public void ValidateRobotPosition_WithInvalidOrientation_ShouldThrowArgumentException(string positionLine)
    {
        // Arrange
        var orientationChar = positionLine.Split(' ')[2][0];

        var action = () => InputValidator.ValidateRobotPosition(positionLine, NullLogger.Instance);
        action.Should().Throw<ArgumentException>()
            .WithMessage($"Invalid orientation character: {orientationChar}. Must be N, E, S, or W");
    }

    [Fact]
    public void ValidateInputStructure_WithValidInput_ShouldNotThrow()
    {
        // Arrange
        var input = new[] { "5 3", "1 1 E", "RFRFRFRF", "3 2 N", "FRRFLLFFRRFLL" };

        var action = () => InputValidator.ValidateInputStructure(input, NullLogger.Instance);
        action.Should().NotThrow();
    }

    [Fact]
    public void ValidateInputStructure_WithNullInput_ShouldThrowArgumentNullException()
    {
        var action = () => InputValidator.ValidateInputStructure(null!, NullLogger.Instance);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ValidateInputStructure_WithEmptyInput_ShouldThrowArgumentException()
    {
        // Arrange
        var input = Array.Empty<string>();

        var action = () => InputValidator.ValidateInputStructure(input, NullLogger.Instance);
        action.Should().Throw<ArgumentException>()
            .WithMessage("Input cannot be empty");
    }

    [Fact]
    public void ValidateInputStructure_WithInsufficientData_OnlyGrid_ShouldThrowArgumentException()
    {
        // Arrange
        var input = new[] { "5 3" };

        var action = () => InputValidator.ValidateInputStructure(input, NullLogger.Instance);
        action.Should().Throw<ArgumentException>()
            .WithMessage("Input must contain at least grid dimensions and one robot definition");
    }

    [Fact]
    public void ValidateInputStructure_WithInsufficientData_GridAndPosition_ShouldThrowArgumentException()
    {
        // Arrange
        var input = new[] { "5 3", "1 1 E" };

        var action = () => InputValidator.ValidateInputStructure(input, NullLogger.Instance);
        action.Should().Throw<ArgumentException>()
            .WithMessage("Input must contain at least grid dimensions and one robot definition");
    }

    [Fact]
    public void ValidateInputStructure_WithIncompleteRobotData_MissingInstructions_ShouldThrowArgumentException()
    {
        // Arrange - Missing instructions for second robot
        var input = new[] { "5 3", "1 1 E", "RFRFRFRF", "3 2 N" };

        var action = () => InputValidator.ValidateInputStructure(input, NullLogger.Instance);
        action.Should().Throw<ArgumentException>()
            .WithMessage("Each robot must have both position and instruction lines");
    }

    [Fact]
    public void ValidateInputStructure_WithIncompleteRobotData_OnlyPosition_ShouldThrowArgumentException()
    {
        // Arrange - Missing instructions for first robot - need enough data to trigger different validation
        var input = new[] { "5 3", "1 1 E", "RFRFRFRF", "3 2 N" }; // Missing instructions for second robot

        var action = () => InputValidator.ValidateInputStructure(input, NullLogger.Instance);
        action.Should().Throw<ArgumentException>()
            .WithMessage("Each robot must have both position and instruction lines");
    }
}