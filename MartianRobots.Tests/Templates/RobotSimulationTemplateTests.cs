namespace MartianRobots.Tests.Templates;

public class RobotSimulationTemplateTests
{
    private readonly TestableRobotSimulationTemplate _template;
    private readonly Mock<IRobotCommand> _mockCommand;

    public RobotSimulationTemplateTests()
    {
        _mockCommand = new Mock<IRobotCommand>();
        _template = new TestableRobotSimulationTemplate(_mockCommand.Object);
    }

    [Fact]
    public void SimulateRobots_WithNullInput_ShouldThrowArgumentNullException()
    {
        var action = () => _template.SimulateRobots(null!);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void SimulateRobots_WithEmptyInput_ShouldThrowArgumentException()
    {
        var action = () => _template.SimulateRobots(Array.Empty<string>());
        action.Should().Throw<ArgumentException>()
            .WithMessage("Input cannot be empty");
    }

    [Fact]
    public void SimulateRobots_WithIncompleteRobotData_ShouldThrowArgumentException()
    {
        // Arrange - Grid line + robot position but no instructions
        var input = new[] { "5 3", "1 1 E" };

        var action = () => _template.SimulateRobots(input);
        action.Should().Throw<ArgumentException>()
            .WithMessage("Incomplete robot data at line 2");
    }

    [Fact]
    public void SimulateRobots_WithOddNumberOfLinesAfterGrid_ShouldThrowArgumentException()
    {
        // Arrange - Grid + robot + instructions + another robot without instructions
        var input = new[] { "5 3", "1 1 E", "RF", "2 2 N" };

        var action = () => _template.SimulateRobots(input);
        action.Should().Throw<ArgumentException>()
            .WithMessage("Incomplete robot data at line 4");
    }

    [Fact]
    public void SimulateRobots_WithValidSingleRobot_ShouldProcessCorrectly()
    {
        // Arrange
        var input = new[] { "5 3", "1 1 E", "RF" };
        
        // Set up mock to simulate R (turn right) and F (move forward)
        var commandCalls = 0;
        _mockCommand.Setup(c => c.Execute(It.IsAny<Robot>(), It.IsAny<MarsGrid>()))
            .Callback<Robot, MarsGrid>((robot, grid) =>
            {
                if (commandCalls == 0) // First command: R (turn right)
                {
                    robot.TurnRight();
                }
                else if (commandCalls == 1) // Second command: F (move forward)
                {
                    if (robot.TryMoveForward(out var newPosition))
                    {
                        robot.UpdatePosition(newPosition);
                    }
                }
                commandCalls++;
            });

        var result = _template.SimulateRobots(input);

        result.Should().HaveCount(1);
        result[0].Should().Be("1 0 S");
        _template.PreSimulationCalled.Should().BeTrue();
        _template.PostSimulationCalled.Should().BeTrue();
        _mockCommand.Verify(c => c.Execute(It.IsAny<Robot>(), It.IsAny<MarsGrid>()), Times.Exactly(2));
    }

    [Fact]
    public void SimulateRobots_WithMultipleRobots_ShouldProcessAll()
    {
        // Arrange
        var input = new[]
        {
            "5 3",
            "1 1 E", "R",
            "2 2 N", "F"
        };

        var robotCount = 0;
        _mockCommand.Setup(c => c.Execute(It.IsAny<Robot>(), It.IsAny<MarsGrid>()))
            .Callback<Robot, MarsGrid>((robot, grid) =>
            {
                if (robotCount == 0) // First robot: R (turn right)
                {
                    robot.TurnRight();
                }
                else // Second robot: F (move forward)
                {
                    if (robot.TryMoveForward(out var newPosition))
                    {
                        robot.UpdatePosition(newPosition);
                    }
                }
                robotCount++;
            });

        var result = _template.SimulateRobots(input);

        result.Should().HaveCount(2);
        result[0].Should().Be("1 1 S");
        result[1].Should().Be("2 3 N");
        _mockCommand.Verify(c => c.Execute(It.IsAny<Robot>(), It.IsAny<MarsGrid>()), Times.Exactly(2));
    }

    [Fact]
    public void SimulateRobots_WithLostRobot_ShouldStopProcessingCommands()
    {
        // Arrange - Set up command to make robot lost
        var input = new[] { "5 3", "1 1 E", "RRR" };
        _mockCommand.Setup(c => c.Execute(It.IsAny<Robot>(), It.IsAny<MarsGrid>()))
            .Callback<Robot, MarsGrid>((robot, grid) => robot.MarkAsLost());

        var result = _template.SimulateRobots(input);

        result.Should().HaveCount(1);
        result[0].Should().Be("1 1 E LOST");
        // Should only execute one command before stopping due to lost robot
        _mockCommand.Verify(c => c.Execute(It.IsAny<Robot>(), It.IsAny<MarsGrid>()), Times.Once);
    }

    [Fact]
    public void SimulateRobots_WithInvalidRobotPosition_ShouldThrowArgumentException()
    {
        // Arrange - Robot outside grid bounds
        var input = new[] { "5 3", "10 10 E", "F" };

        var action = () => _template.SimulateRobots(input);
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SimulateRobots_WithInvalidInstructions_ShouldThrowArgumentException()
    {
        // Arrange - Invalid instruction character
        var input = new[] { "5 3", "1 1 E", "X" };
        _template.ShouldThrowOnInvalidInstructions = true;

        var action = () => _template.SimulateRobots(input);
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SimulateRobots_WithEmptyInstructions_ShouldProcessRobotWithoutMoving()
    {
        // Arrange
        var input = new[] { "5 3", "1 1 E", "" };

        var result = _template.SimulateRobots(input);

        result.Should().HaveCount(1);
        result[0].Should().Be("1 1 E");
        _mockCommand.Verify(c => c.Execute(It.IsAny<Robot>(), It.IsAny<MarsGrid>()), Times.Never);
    }

    [Fact]
    public void SimulateRobots_ShouldCallAllHookMethods()
    {
        // Arrange
        var input = new[] { "5 3", "1 1 E", "F" };

        _template.SimulateRobots(input);

        _template.PreSimulationCalled.Should().BeTrue();
        _template.PostSimulationCalled.Should().BeTrue();
        _template.ValidateInputCalled.Should().BeTrue();
        _template.ValidateRobotCalled.Should().BeTrue();
        _template.ValidateInstructionsCalled.Should().BeTrue();
    }

    [Fact]
    public void ValidateRobot_WithValidRobot_ShouldNotThrow()
    {
        // Arrange
        var grid = new MarsGrid(5, 3);
        var robot = new Robot(new Position(2, 2), Orientation.North);

        var action = () => _template.PublicValidateRobot(robot, grid);
        action.Should().NotThrow();
    }

    [Fact]
    public void ValidateRobot_WithInvalidPosition_ShouldThrow()
    {
        // Arrange
        var grid = new MarsGrid(5, 3);
        var robot = new Robot(new Position(10, 10), Orientation.North);

        var action = () => _template.PublicValidateRobot(robot, grid);
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ProcessRobot_WithCommands_ShouldExecuteInOrder()
    {
        // Arrange
        var grid = new MarsGrid(5, 3);
        var robot = new Robot(new Position(1, 1), Orientation.East);
        var callOrder = new List<int>();
        
        var command1 = new Mock<IRobotCommand>();
        var command2 = new Mock<IRobotCommand>();
        
        command1.Setup(c => c.Execute(It.IsAny<Robot>(), It.IsAny<MarsGrid>()))
            .Callback(() => callOrder.Add(1));
        command2.Setup(c => c.Execute(It.IsAny<Robot>(), It.IsAny<MarsGrid>()))
            .Callback(() => callOrder.Add(2));

        _template.SetCommands(new[] { command1.Object, command2.Object });

        _template.PublicProcessRobot(robot, grid, "FF");

        callOrder.Should().Equal(1, 2);
        command1.Verify(c => c.Execute(robot, grid), Times.Once);
        command2.Verify(c => c.Execute(robot, grid), Times.Once);
    }

    [Fact]
    public void FormatResult_WithNormalRobot_ShouldReturnToString()
    {
        // Arrange
        var robot = new Robot(new Position(3, 2), Orientation.West);

        var result = _template.PublicFormatResult(robot);

        result.Should().Be("3 2 W");
    }

    [Fact]
    public void FormatResult_WithLostRobot_ShouldIncludeLostMarker()
    {
        // Arrange
        var robot = new Robot(new Position(3, 2), Orientation.West);
        robot.MarkAsLost();

        var result = _template.PublicFormatResult(robot);

        result.Should().Be("3 2 W LOST");
    }
}

/// <summary>
/// Testable implementation of RobotSimulationTemplate for testing purposes
/// </summary>
public class TestableRobotSimulationTemplate : RobotSimulationTemplate
{
    private readonly IRobotCommand _command;
    private IEnumerable<IRobotCommand> _commands = Array.Empty<IRobotCommand>();

    public bool PreSimulationCalled { get; private set; }
    public bool PostSimulationCalled { get; private set; }
    public bool ValidateInputCalled { get; private set; }
    public bool ValidateRobotCalled { get; private set; }
    public bool ValidateInstructionsCalled { get; private set; }
    public bool ShouldThrowOnInvalidInstructions { get; set; }

    public TestableRobotSimulationTemplate(IRobotCommand command)
    {
        _command = command;
    }

    public void SetCommands(IEnumerable<IRobotCommand> commands)
    {
        _commands = commands;
    }

    // Public wrappers for testing protected methods
    public void PublicValidateRobot(Robot robot, MarsGrid grid) => ValidateRobot(robot, grid);
    public void PublicProcessRobot(Robot robot, MarsGrid grid, string instructions) => ProcessRobot(robot, grid, instructions);
    public string PublicFormatResult(Robot robot) => FormatResult(robot);

    protected override void ValidateInput(string[] input)
    {
        ValidateInputCalled = true;
        base.ValidateInput(input);
    }

    protected override MarsGrid ParseGrid(string gridLine)
    {
        var parts = gridLine.Split(' ');
        return new MarsGrid(int.Parse(parts[0]), int.Parse(parts[1]));
    }

    protected override void PreSimulation(MarsGrid grid)
    {
        PreSimulationCalled = true;
    }

    protected override Robot ParseRobot(string positionLine)
    {
        var parts = positionLine.Split(' ');
        var x = int.Parse(parts[0]);
        var y = int.Parse(parts[1]);
        var orientation = OrientationExtensions.FromChar(parts[2][0]);
        return new Robot(new Position(x, y), orientation);
    }

    protected override void ValidateRobot(Robot robot, MarsGrid grid)
    {
        ValidateRobotCalled = true;
        base.ValidateRobot(robot, grid);
    }

    protected override void ValidateInstructions(string instructions)
    {
        ValidateInstructionsCalled = true;
        
        if (ShouldThrowOnInvalidInstructions && instructions.Contains('X'))
        {
            throw new ArgumentException("Invalid instruction");
        }
    }

    protected override IEnumerable<IRobotCommand> CreateCommands(string instructions)
    {
        if (_commands.Any())
        {
            return _commands;
        }

        return instructions.Select(_ => _command);
    }

    protected override void PostSimulation(MarsGrid grid, List<string> results)
    {
        PostSimulationCalled = true;
    }
}