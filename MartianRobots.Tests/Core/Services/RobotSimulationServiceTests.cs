using MartianRobots.Core.Services;

namespace MartianRobots.Tests.Core.Services;

public class RobotSimulationServiceTests
{
    [Fact]
    public void SimulateRobots_WithValidInput_ShouldReturnCorrectResults()
    {
        // Arrange
        var input = new[]
        {
            "5 3",
            "1 1 E",
            "RFRFRFRF",
            "3 2 N",
            "FRRFLLFFRRFLL",
            "0 3 W",
            "LLFFFLFLFL"
        };

        var results = RobotSimulationService.SimulateRobots(input);

        results.Should().HaveCount(3);
        results[0].Should().Be("1 1 E");
        results[1].Should().Be("3 3 N LOST");
        results[2].Should().Be("2 3 S");
    }

    [Fact]
    public void SimulateRobots_WithSampleInput_ShouldMatchExpectedOutput()
    {
        // Arrange - This is the classic Mars Robots problem sample
        var input = new[]
        {
            "5 3",
            "1 1 E",
            "RFRFRFRF",
            "3 2 N", 
            "FRRFLLFFRRFLL",
            "0 3 W",
            "LLFFFLFLFL"
        };

        var results = RobotSimulationService.SimulateRobots(input);

        results[0].Should().Be("1 1 E");
        results[1].Should().Be("3 3 N LOST");
        results[2].Should().Be("2 3 S");
    }

    [Fact]
    public void SimulateRobots_WithEmptyInput_ShouldThrowArgumentException()
    {
        // Arrange
        var input = Array.Empty<string>();

        var action = () => RobotSimulationService.SimulateRobots(input);
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SimulateRobots_WithNullInput_ShouldThrowArgumentNullException()
    {
        var action = () => RobotSimulationService.SimulateRobots(null!);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void SimulateRobots_WithInvalidGridData_ShouldThrowArgumentException()
    {
        // Arrange
        var input = new[] { "invalid grid", "1 1 E", "RFRFRFRF" };

        var action = () => RobotSimulationService.SimulateRobots(input);
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SimulateRobots_WithInvalidRobotPosition_ShouldThrowArgumentException()
    {
        // Arrange
        var input = new[] { "5 3", "invalid position", "RFRFRFRF" };

        var action = () => RobotSimulationService.SimulateRobots(input);
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SimulateRobots_WithInvalidInstructions_ShouldThrowArgumentException()
    {
        // Arrange
        var input = new[] { "5 3", "1 1 E", "INVALID" };

        var action = () => RobotSimulationService.SimulateRobots(input);
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SimulateRobots_WithIncompleteRobotData_ShouldThrowArgumentException()
    {
        // Arrange - Missing instructions for the robot but has enough for initial validation
        var input = new[] { "5 3", "1 1 E", "RFRFRFRF", "3 2 N" };

        var action = () => RobotSimulationService.SimulateRobots(input);
        action.Should().Throw<ArgumentException>()
            .And.Message.Should().Contain("Each robot must have both position and instruction lines");
    }

    [Fact]
    public void SimulateRobots_WithRobotStartingOutsideGrid_ShouldThrowArgumentException()
    {
        // Arrange
        var input = new[] { "5 3", "6 4 E", "RFRFRFRF" };

        var action = () => RobotSimulationService.SimulateRobots(input);
        action.Should().Throw<ArgumentException>()
            .And.Message.Should().Contain("outside the grid bounds");
    }

    [Fact]
    public void SimulateRobots_WithScentBehavior_ShouldPreventSubsequentLoss()
    {
        // Arrange - Two robots trying the same losing move
        var input = new[]
        {
            "1 1",
            "1 1 N",
            "F", // This robot will be lost and leave a scent
            "1 1 N", 
            "F"  // This robot should be saved by the scent
        };

        var results = RobotSimulationService.SimulateRobots(input);

        results.Should().HaveCount(2);
        results[0].Should().Be("1 1 N LOST");
        results[1].Should().Be("1 1 N"); // Not lost due to scent
    }

    [Fact]
    public void SimulateRobots_WithSingleRobot_ShouldWork()
    {
        // Arrange
        var input = new[]
        {
            "3 3",
            "1 1 E",
            "RFRFRFRF"
        };

        var results = RobotSimulationService.SimulateRobots(input);

        results.Should().HaveCount(1);
        results[0].Should().Be("1 1 E");
    }

    [Fact]
    public void SimulateRobots_WithMultipleRobots_ShouldProcessInOrder()
    {
        // Arrange
        var input = new[]
        {
            "2 2",
            "0 0 N",
            "FRF",    // Move to (0,1), turn right to face E, move to (1,1)
            "1 1 E", 
            "FLF"     // Move to (2,1), turn left to face N, move to (2,2)
        };

        var results = RobotSimulationService.SimulateRobots(input);

        results.Should().HaveCount(2);
        results[0].Should().Be("1 1 E"); // First robot result
        results[1].Should().Be("2 2 N"); // Second robot result
    }

    [Fact]
    public void SimulateRobots_StaticMethod_ShouldWorkAsInstanceMethod()
    {
        // Arrange
        var input = new[] { "2 2", "0 0 N", "FRF" };
        var service = new RobotSimulationService();

        var staticResult = RobotSimulationService.SimulateRobots(input);
        var instanceResult = ((RobotSimulationTemplate)service).SimulateRobots(input);

        staticResult.Should().BeEquivalentTo(instanceResult);
    }
}