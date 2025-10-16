using System.Reflection;

namespace MartianRobots.Tests.Console;

/// <summary>
/// Tests for the Program class that need to run in isolation due to console redirection
/// </summary>
[Collection("ProgramTests")] // Ensure these tests don't run in parallel
public class ProgramTests
{
    [Fact]
    public async Task Main_ShouldCreateApplicationAndCallRun()
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

            // Act - Call Main method and await the Task
            var task = (Task<int>)mainMethod!.Invoke(null, new object[] { new string[0] })!;
            var result = await task;

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
            
            // Give time for any background tasks to complete
            await Task.Delay(100);
        }
    }

    [Fact]
    public async Task Main_WithInvalidInput_ShouldReturnErrorCode()
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

            // Act - Call Main method and await the Task
            var task = (Task<int>)mainMethod!.Invoke(null, new object[] { new string[0] })!;
            var result = await task;

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
            
            // Give time for any background tasks to complete
            await Task.Delay(100);
        }
    }
}