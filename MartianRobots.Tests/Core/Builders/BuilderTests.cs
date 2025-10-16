using MartianRobots.Core.Builders;

namespace MartianRobots.Tests.Core.Builders;

public class BuilderTests
{
    public class RobotBuilderTests
    {
        [Fact]
        public void Build_WithPositionAndOrientation_ShouldCreateCorrectRobot()
        {
            // Arrange
            var builder = new RobotBuilder();

            var robot = builder
                .AtPosition(3, 4)
                .Facing(Orientation.East)
                .Build();

            robot.Position.Should().Be(new Position(3, 4));
            robot.Orientation.Should().Be(Orientation.East);
            robot.IsLost.Should().BeFalse();
        }

        [Fact]
        public void Build_WithDefaultOrientation_ShouldFaceNorth()
        {
            // Arrange
            var builder = new RobotBuilder();

            var robot = builder
                .AtPosition(1, 2)
                .Build();

            robot.Position.Should().Be(new Position(1, 2));
            robot.Orientation.Should().Be(Orientation.North);
            robot.IsLost.Should().BeFalse();
        }

        [Fact]
        public void AtPosition_ShouldReturnBuilderForChaining()
        {
            // Arrange
            var builder = new RobotBuilder();

            var result = builder.AtPosition(1, 2);

            result.Should().BeSameAs(builder);
        }

        [Fact]
        public void Facing_ShouldReturnBuilderForChaining()
        {
            // Arrange
            var builder = new RobotBuilder();

            var result = builder.Facing(Orientation.South);

            result.Should().BeSameAs(builder);
        }

        [Theory]
        [InlineData(Orientation.North)]
        [InlineData(Orientation.East)]
        [InlineData(Orientation.South)]
        [InlineData(Orientation.West)]
        public void Facing_WithAllOrientations_ShouldSetCorrectly(Orientation orientation)
        {
            // Arrange
            var builder = new RobotBuilder();

            var robot = builder
                .AtPosition(0, 0)
                .Facing(orientation)
                .Build();

            robot.Orientation.Should().Be(orientation);
        }

        [Fact]
        public void Build_CalledMultipleTimes_ShouldCreateSeparateInstances()
        {
            // Arrange
            var builder = new RobotBuilder()
                .AtPosition(1, 2)
                .Facing(Orientation.East);

            var robot1 = builder.Build();
            var robot2 = builder.Build();

            robot1.Should().NotBeSameAs(robot2);
            robot1.Position.Should().Be(robot2.Position);
            robot1.Orientation.Should().Be(robot2.Orientation);
        }

        [Theory]
        [InlineData(-5, 10)]
        [InlineData(0, 0)]
        [InlineData(100, 200)]
        public void AtPosition_WithVariousCoordinates_ShouldWork(int x, int y)
        {
            // Arrange
            var builder = new RobotBuilder();

            var robot = builder
                .AtPosition(x, y)
                .Build();

            robot.Position.Should().Be(new Position(x, y));
        }
    }

    public class MarsGridBuilderTests
    {
        [Fact]
        public void Build_WithValidDimensions_ShouldCreateCorrectGrid()
        {
            // Arrange
            var builder = new MarsGridBuilder();

            var grid = builder
                .WithDimensions(5, 3)
                .Build();

            grid.IsValidPosition(new Position(5, 3)).Should().BeTrue();
            grid.IsValidPosition(new Position(6, 3)).Should().BeFalse();
            grid.IsValidPosition(new Position(5, 4)).Should().BeFalse();
        }

        [Fact]
        public void WithDimensions_ShouldReturnBuilderForChaining()
        {
            // Arrange
            var builder = new MarsGridBuilder();

            var result = builder.WithDimensions(10, 15);

            result.Should().BeSameAs(builder);
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(1, 1)]
        [InlineData(50, 50)]
        public void WithDimensions_WithValidValues_ShouldCreateCorrectGrid(int maxX, int maxY)
        {
            // Arrange
            var builder = new MarsGridBuilder();

            var grid = builder
                .WithDimensions(maxX, maxY)
                .Build();

            grid.IsValidPosition(new Position(maxX, maxY)).Should().BeTrue();
            grid.IsValidPosition(new Position(0, 0)).Should().BeTrue();
        }

        [Theory]
        [InlineData(-1, 5)]
        [InlineData(5, -1)]
        [InlineData(51, 50)]
        [InlineData(50, 51)]
        public void Build_WithInvalidDimensions_ShouldThrowArgumentOutOfRangeException(int maxX, int maxY)
        {
            // Arrange
            var builder = new MarsGridBuilder();

            var action = () => builder
                .WithDimensions(maxX, maxY)
                .Build();
            
            action.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void Build_CalledMultipleTimes_ShouldCreateSeparateInstances()
        {
            // Arrange
            var builder = new MarsGridBuilder()
                .WithDimensions(5, 5);

            var grid1 = builder.Build();
            var grid2 = builder.Build();

            grid1.Should().NotBeSameAs(grid2);
            // Both grids should have the same behavior
            grid1.IsValidPosition(new Position(5, 5)).Should().Be(grid2.IsValidPosition(new Position(5, 5)));
        }

        [Fact]
        public void Build_WithoutSettingDimensions_ShouldUseDefaultValues()
        {
            // Arrange
            var builder = new MarsGridBuilder();

            var grid = builder.Build();

            // Assert - Should use default values (0, 0)
            grid.IsValidPosition(new Position(0, 0)).Should().BeTrue();
            grid.IsValidPosition(new Position(1, 0)).Should().BeFalse();
            grid.IsValidPosition(new Position(0, 1)).Should().BeFalse();
        }
    }
}