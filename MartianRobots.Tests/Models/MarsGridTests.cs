namespace MartianRobots.Tests.Models;

public class MarsGridTests
{
    [Fact]
    public void Constructor_ShouldSetDimensions()
    {
        var grid = new MarsGrid(5, 3);

        grid.ToString().Should().Contain("(0,0) to (5,3)");
    }

    [Theory]
    [InlineData(-1, 5)]
    [InlineData(5, -1)]
    [InlineData(-1, -1)]
    public void Constructor_WithNegativeValues_ShouldThrowArgumentOutOfRangeException(int maxX, int maxY)
    {
        var action = () => new MarsGrid(maxX, maxY);
        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(51, 25)]
    [InlineData(25, 51)]
    [InlineData(51, 51)]
    public void Constructor_WithValuesTooLarge_ShouldThrowArgumentOutOfRangeException(int maxX, int maxY)
    {
        var action = () => new MarsGrid(maxX, maxY);
        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(0, 0, true)]
    [InlineData(5, 3, true)]
    [InlineData(2, 1, true)]
    [InlineData(-1, 0, false)]
    [InlineData(0, -1, false)]
    [InlineData(6, 3, false)]
    [InlineData(5, 4, false)]
    [InlineData(6, 4, false)]
    public void IsValidPosition_ShouldReturnCorrectResult(int x, int y, bool expected)
    {
        // Arrange
        var grid = new MarsGrid(5, 3);
        var position = new Position(x, y);

        var result = grid.IsValidPosition(position);

        result.Should().Be(expected);
    }

    [Fact]
    public void HasScent_WhenNoScentAdded_ShouldReturnFalse()
    {
        // Arrange
        var grid = new MarsGrid(5, 3);
        var position = new Position(2, 1);

        var result = grid.HasScent(position);

        result.Should().BeFalse();
    }

    [Fact]
    public void AddScent_ShouldMakeScentDetectable()
    {
        // Arrange
        var grid = new MarsGrid(5, 3);
        var position = new Position(2, 1);

        grid.AddScent(position);
        var result = grid.HasScent(position);

        result.Should().BeTrue();
    }

    [Fact]
    public void AddScent_MultiplePositions_ShouldTrackAllScents()
    {
        // Arrange
        var grid = new MarsGrid(5, 3);
        var position1 = new Position(1, 1);
        var position2 = new Position(3, 2);
        var position3 = new Position(0, 0);

        grid.AddScent(position1);
        grid.AddScent(position2);

        grid.HasScent(position1).Should().BeTrue();
        grid.HasScent(position2).Should().BeTrue();
        grid.HasScent(position3).Should().BeFalse();
    }

    [Fact]
    public void AddScent_SamePositionTwice_ShouldStillWork()
    {
        // Arrange
        var grid = new MarsGrid(5, 3);
        var position = new Position(2, 1);

        grid.AddScent(position);
        grid.AddScent(position); // Add again

        grid.HasScent(position).Should().BeTrue();
    }

    [Fact]
    public void ValidateInitialPosition_WithValidPosition_ShouldNotThrow()
    {
        // Arrange
        var grid = new MarsGrid(5, 3);
        var position = new Position(2, 1);

        var action = () => grid.ValidateInitialPosition(position);
        action.Should().NotThrow();
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    [InlineData(6, 3)]
    [InlineData(5, 4)]
    public void ValidateInitialPosition_WithInvalidPosition_ShouldThrowArgumentException(int x, int y)
    {
        // Arrange
        var grid = new MarsGrid(5, 3);
        var position = new Position(x, y);

        var action = () => grid.ValidateInitialPosition(position);
        action.Should().Throw<ArgumentException>()
            .WithMessage($"Initial position ({x}, {y}) is outside the grid bounds (0,0) to (5,3)");
    }

    [Fact]
    public void ToString_ShouldIncludeGridInformation()
    {
        // Arrange
        var grid = new MarsGrid(10, 8);

        var result = grid.ToString();

        result.Should().Contain("Mars Grid");
        result.Should().Contain("(0,0) to (10,8)");
        result.Should().Contain("Scents: 0");
    }

    [Fact]
    public void ToString_WithScents_ShouldShowScentCount()
    {
        // Arrange
        var grid = new MarsGrid(5, 3);
        grid.AddScent(new Position(1, 1));
        grid.AddScent(new Position(2, 2));

        var result = grid.ToString();

        result.Should().Contain("Scents: 2");
    }
}