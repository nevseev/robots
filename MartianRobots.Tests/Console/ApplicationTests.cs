using MartianRobots.Console;
using Microsoft.Extensions.Logging;

namespace MartianRobots.Tests.Console;

public class ApplicationTests
{
    private static ILogger<Application> CreateMockLogger()
    {
        return new Mock<ILogger<Application>>().Object;
    }
    [Fact]
    public void Constructor_WithValidStreams_ShouldNotThrow()
    {
        // Arrange
        using var input = new StringReader("");
        using var output = new StringWriter();
        using var error = new StringWriter();

        var action = () => new Application(input, output, error, CreateMockLogger());
        action.Should().NotThrow();
    }

    [Fact]
    public void Constructor_Default_ShouldNotThrow()
    {
        var action = () => new Application(System.Console.In, System.Console.Out, System.Console.Error, CreateMockLogger());
        action.Should().NotThrow();
    }

    [Fact]
    public void Constructor_WithNullInput_ShouldThrowArgumentNullException()
    {
        // Arrange
        using var output = new StringWriter();
        using var error = new StringWriter();

        var action = () => new Application(null!, output, error, CreateMockLogger());
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("input");
    }

    [Fact]
    public void Constructor_WithNullOutput_ShouldThrowArgumentNullException()
    {
        // Arrange
        using var input = new StringReader("");
        using var error = new StringWriter();

        var action = () => new Application(input, null!, error, CreateMockLogger());
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("output");
    }

    [Fact]
    public void Constructor_WithNullError_ShouldThrowArgumentNullException()
    {
        // Arrange
        using var input = new StringReader("");
        using var output = new StringWriter();

        var action = () => new Application(input, output, null!, CreateMockLogger());
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("error");
    }

    [Fact]
    public void Run_WithValidInput_ShouldReturnSuccessExitCode()
    {
        // Arrange
        var inputText = "5 3\n1 1 E\nRFRFRFRF\n3 2 N\nFRRFLLFFRRFLL\n0 3 W\nLLFFFLFLFL";
        using var input = new StringReader(inputText);
        using var output = new StringWriter();
        using var error = new StringWriter();
        var application = new Application(input, output, error, CreateMockLogger());

        var result = application.Run();

        result.Should().Be(0);
        error.ToString().Should().BeEmpty();
    }

    [Fact]
    public void Run_WithValidInput_ShouldProduceCorrectOutput()
    {
        // Arrange - Sample input from problem description
        var inputText = "5 3\n1 1 E\nRFRFRFRF\n3 2 N\nFRRFLLFFRRFLL\n0 3 W\nLLFFFLFLFL";
        using var input = new StringReader(inputText);
        using var output = new StringWriter();
        using var error = new StringWriter();
        var application = new Application(input, output, error, CreateMockLogger());

        application.Run();

        var outputLines = output.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries);
        outputLines.Should().HaveCount(3);
        outputLines[0].Should().Be("1 1 E");
        outputLines[1].Should().Be("3 3 N LOST");
        outputLines[2].Should().Be("2 3 S");
    }

    [Fact]
    public void Run_WithEmptyInput_ShouldReturnErrorExitCode()
    {
        // Arrange
        using var input = new StringReader("");
        using var output = new StringWriter();
        using var error = new StringWriter();
        var application = new Application(input, output, error, CreateMockLogger());

        var result = application.Run();

        result.Should().Be(1);
        error.ToString().Should().Contain("Error: No input provided");
        output.ToString().Should().BeEmpty();
    }

    [Fact]
    public void Run_WithInvalidGridInput_ShouldReturnErrorExitCode()
    {
        // Arrange - Invalid grid format
        var inputText = "invalid grid\n1 1 E\nRF";
        using var input = new StringReader(inputText);
        using var output = new StringWriter();
        using var error = new StringWriter();
        var application = new Application(input, output, error, CreateMockLogger());

        var result = application.Run();

        result.Should().Be(1);
        error.ToString().Should().Contain("Error:");
        output.ToString().Should().BeEmpty();
    }

    [Fact]
    public void Run_WithInvalidRobotPosition_ShouldReturnErrorExitCode()
    {
        // Arrange - Invalid robot position format
        var inputText = "5 3\ninvalid robot\nRF";
        using var input = new StringReader(inputText);
        using var output = new StringWriter();
        using var error = new StringWriter();
        var application = new Application(input, output, error, CreateMockLogger());

        var result = application.Run();

        result.Should().Be(1);
        error.ToString().Should().Contain("Error:");
        output.ToString().Should().BeEmpty();
    }

    [Fact]
    public void Run_WithInvalidInstructions_ShouldReturnErrorExitCode()
    {
        // Arrange - Invalid instruction characters
        var inputText = "5 3\n1 1 E\nXYZ";
        using var input = new StringReader(inputText);
        using var output = new StringWriter();
        using var error = new StringWriter();
        var application = new Application(input, output, error, CreateMockLogger());

        var result = application.Run();

        result.Should().Be(1);
        error.ToString().Should().Contain("Error:");
        output.ToString().Should().BeEmpty();
    }

    [Fact]
    public void Run_WithIncompleteRobotData_ShouldReturnErrorExitCode()
    {
        // Arrange - Robot position without instructions
        var inputText = "5 3\n1 1 E";
        using var input = new StringReader(inputText);
        using var output = new StringWriter();
        using var error = new StringWriter();
        var application = new Application(input, output, error, CreateMockLogger());

        var result = application.Run();

        result.Should().Be(1);
        error.ToString().Should().Contain("Error:");
        output.ToString().Should().BeEmpty();
    }

    [Fact]
    public void Run_WithSingleRobot_ShouldProcessCorrectly()
    {
        // Arrange - Robot at (2,2) facing North, moves forward twice
        var inputText = "3 3\n2 2 N\nFF";
        using var input = new StringReader(inputText);
        using var output = new StringWriter();
        using var error = new StringWriter();
        var application = new Application(input, output, error, CreateMockLogger());

        application.Run();

        var outputLines = output.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries);
        outputLines.Should().HaveCount(1);
        outputLines[0].Should().Be("2 3 N LOST"); // First F moves to (2,3), second F would go off grid
    }

    [Fact]
    public void Run_WithMultipleRobots_ShouldProcessAllInOrder()
    {
        // Arrange
        var inputText = "5 3\n1 1 E\nR\n2 2 N\nF\n3 3 W\nL";
        using var input = new StringReader(inputText);
        using var output = new StringWriter();
        using var error = new StringWriter();
        var application = new Application(input, output, error, CreateMockLogger());

        application.Run();

        var outputLines = output.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries);
        outputLines.Should().HaveCount(3);
        outputLines[0].Should().Be("1 1 S");
        outputLines[1].Should().Be("2 3 N");
        outputLines[2].Should().Be("3 3 S");
    }

    [Fact]
    public void Run_WithRobotThatGetsLost_ShouldShowLostStatus()
    {
        // Arrange - Robot will get lost going off the edge
        var inputText = "2 2\n2 2 E\nF";
        using var input = new StringReader(inputText);
        using var output = new StringWriter();
        using var error = new StringWriter();
        var application = new Application(input, output, error, CreateMockLogger());

        application.Run();

        var outputLines = output.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries);
        outputLines.Should().HaveCount(1);
        outputLines[0].Should().Be("2 2 E LOST");
    }

    [Fact]
    public void Run_WithScentPrevention_ShouldRespectScents()
    {
        // Arrange - First robot gets lost, second robot should be protected by scent
        var inputText = "3 2\n3 2 N\nF\n3 2 N\nF";
        using var input = new StringReader(inputText);
        using var output = new StringWriter();
        using var error = new StringWriter();
        var application = new Application(input, output, error, CreateMockLogger());

        application.Run();

        var outputLines = output.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries);
        outputLines.Should().HaveCount(2);
        outputLines[0].Should().Be("3 2 N LOST");
        outputLines[1].Should().Be("3 2 N"); // Should not be lost due to scent
    }

    [Fact]
    public void Run_WithEmptyInstructions_ShouldReturnErrorExitCode()
    {
        // Arrange - Empty instructions should cause an error
        var inputText = "5 3\n2 2 E\n";
        using var input = new StringReader(inputText);
        using var output = new StringWriter();
        using var error = new StringWriter();
        var application = new Application(input, output, error, CreateMockLogger());

        var result = application.Run();

        result.Should().Be(1);
        error.ToString().Should().Contain("Error:");
        output.ToString().Should().BeEmpty();
    }

    [Fact]
    public void Run_WithSpaceOnlyInstructions_ShouldReturnErrorExitCode()
    {
        // Arrange - Instructions with just space get trimmed to empty, causing error
        var inputText = "5 3\n2 2 E\n ";
        using var input = new StringReader(inputText);
        using var output = new StringWriter();
        using var error = new StringWriter();
        var application = new Application(input, output, error, CreateMockLogger());

        var result = application.Run();

        result.Should().Be(1);
        error.ToString().Should().Contain("Error:");
        output.ToString().Should().BeEmpty();
    }

    [Fact]
    public void Run_WithInstructionsThatCancelOut_ShouldLeaveRobotInPlace()
    {
        // Arrange - LLLL (turn left 4 times) leaves robot in same position and orientation
        var inputText = "5 3\n2 2 E\nLLLL";
        using var input = new StringReader(inputText);
        using var output = new StringWriter();
        using var error = new StringWriter();
        var application = new Application(input, output, error, CreateMockLogger());

        var result = application.Run();

        result.Should().Be(0);
        var outputLines = output.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries);
        outputLines.Should().HaveCount(1);
        outputLines[0].Should().Be("2 2 E");
    }

    [Fact]
    public void Run_WithExceptionInProcessing_ShouldReturnErrorExitCode()
    {
        // Arrange - Create a mock that throws a non-ArgumentException
        var inputText = "5 3\n1 1 E\nRF";
        using var input = new StringReader(inputText);
        using var output = new StringWriter();
        using var error = new StringWriter();
        var application = new ExceptionThrowingApplication(input, output, error);

        var result = application.Run();

        result.Should().Be(1);
        error.ToString().Should().Contain("Error: Simulated exception");
        output.ToString().Should().BeEmpty();
    }

    [Fact]
    public void Run_PipelineFlow_ShouldExecuteInCorrectOrder()
    {
        // Arrange - Test the complete pipeline flow
        var inputText = "5 3\n1 1 E\nRFRFRFRF";
        using var input = new StringReader(inputText);
        using var output = new StringWriter();
        using var error = new StringWriter();
        var application = new TestableApplication(input, output, error);

        var result = application.Run();

        result.Should().Be(0);
        application.ReadInputCalled.Should().BeTrue();
        application.ProcessRobotsCalled.Should().BeTrue();
        application.WriteOutputCalled.Should().BeTrue();
    }
}

/// <summary>
/// Testable version of Application to verify method calls
/// </summary>
public class TestableApplication : Application
{
    public bool ReadInputCalled { get; private set; }
    public bool ProcessRobotsCalled { get; private set; }
    public bool WriteOutputCalled { get; private set; }

    public TestableApplication(TextReader input, TextWriter output, TextWriter error) 
        : base(input, output, error, new Mock<ILogger<Application>>().Object)
    {
    }

    protected override List<string> ReadInput()
    {
        ReadInputCalled = true;
        return base.ReadInput();
    }

    protected override List<string> ProcessRobots(List<string> inputLines)
    {
        ProcessRobotsCalled = true;
        return base.ProcessRobots(inputLines);
    }

    protected override void WriteOutput(List<string> results)
    {
        WriteOutputCalled = true;
        base.WriteOutput(results);
    }
}

/// <summary>
/// Application that throws an exception to test generic exception handling
/// </summary>
public class ExceptionThrowingApplication : Application
{
    public ExceptionThrowingApplication(TextReader input, TextWriter output, TextWriter error) 
        : base(input, output, error, new Mock<ILogger<Application>>().Object)
    {
    }

    protected override List<string> ProcessRobots(List<string> inputLines)
    {
        throw new InvalidOperationException("Simulated exception");
    }
}