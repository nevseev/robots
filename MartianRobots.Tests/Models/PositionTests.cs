namespace MartianRobots.Tests.Models;

public class PositionTests
{
    [Fact]
    public void Constructor_ShouldSetXAndYCorrectly()
    {
        // Arrange & Act
        var position = new Position(5, 10);

        position.X.Should().Be(5);
        position.Y.Should().Be(10);
    }

    [Fact]
    public void ToString_ShouldReturnCorrectFormat()
    {
        // Arrange
        var position = new Position(3, 7);

        var result = position.ToString();
        result.Should().Be("3 7");
    }

    [Fact]
    public void Equality_ShouldReturnTrueForSameCoordinates()
    {
        // Arrange
        var position1 = new Position(2, 4);
        var position2 = new Position(2, 4);

        position1.Should().Be(position2);
        (position1 == position2).Should().BeTrue();
    }

    [Fact]
    public void Equality_ShouldReturnFalseForDifferentCoordinates()
    {
        // Arrange
        var position1 = new Position(2, 4);
        var position2 = new Position(3, 4);

        position1.Should().NotBe(position2);
        (position1 != position2).Should().BeTrue();
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(-5, 10)]
    [InlineData(50, -20)]
    public void Constructor_ShouldHandleVariousCoordinates(int x, int y)
    {
        var position = new Position(x, y);
        position.X.Should().Be(x);
        position.Y.Should().Be(y);
    }
}