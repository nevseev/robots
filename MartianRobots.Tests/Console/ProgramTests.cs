using System.Reflection;

namespace MartianRobots.Tests.Console;

public class ProgramTests
{
    [Fact]
    public void Main_ShouldCreateApplicationAndCallRun()
    {
        // Arrange - Redirect console streams to capture behavior
        var originalIn = System.Console.In;
        var originalOut = System.Console.Out;
        var originalError = System.Console.Error;

        try
        {
            using var inputReader = new StringReader("5 3\n1 1 E\nR");
            using var outputWriter = new StringWriter();
            using var errorWriter = new StringWriter();

            System.Console.SetIn(inputReader);
            System.Console.SetOut(outputWriter);
            System.Console.SetError(errorWriter);

            // Get the Program type and Main method via reflection
            var programType = Type.GetType("MartianRobots.Console.Program, MartianRobots.Console");
            programType.Should().NotBeNull("Program class should exist");

            var mainMethod = programType!.GetMethod("Main", BindingFlags.Static | BindingFlags.NonPublic);
            mainMethod.Should().NotBeNull("Main method should exist");

            // Act - Call Main method
            var result = mainMethod!.Invoke(null, new object[] { new string[0] });

            // Assert
            result.Should().Be(0);
            outputWriter.ToString().Should().Contain("1 1 S");
        }
        finally
        {
            // Restore original console streams
            System.Console.SetIn(originalIn);
            System.Console.SetOut(originalOut);
            System.Console.SetError(originalError);
        }
    }

    [Fact]
    public void Main_WithInvalidInput_ShouldReturnErrorCode()
    {
        // Arrange
        var originalIn = System.Console.In;
        var originalOut = System.Console.Out;
        var originalError = System.Console.Error;

        try
        {
            using var inputReader = new StringReader(""); // Empty input
            using var outputWriter = new StringWriter();
            using var errorWriter = new StringWriter();

            System.Console.SetIn(inputReader);
            System.Console.SetOut(outputWriter);
            System.Console.SetError(errorWriter);

            // Get the Program type and Main method via reflection
            var programType = Type.GetType("MartianRobots.Console.Program, MartianRobots.Console");
            var mainMethod = programType!.GetMethod("Main", BindingFlags.Static | BindingFlags.NonPublic);

            // Act
            var result = mainMethod!.Invoke(null, new object[] { new string[0] });

            // Assert
            result.Should().Be(1);
            errorWriter.ToString().Should().Contain("Error:");
        }
        finally
        {
            // Restore original console streams
            System.Console.SetIn(originalIn);
            System.Console.SetOut(originalOut);
            System.Console.SetError(originalError);
        }
    }
}