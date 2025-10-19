using System.Reflection;

namespace MartianRobots.Tests.Console;

/// <summary>
/// Tests for the Program class to verify basic structure
/// </summary>
[Collection("ProgramTests")] // Ensure these tests don't run in parallel
public class ProgramTests
{
    [Fact]
    public void Main_ShouldHaveCorrectSignature()
    {
        // Arrange & Act
        var programType = Type.GetType("MartianRobots.Console.Program, MartianRobots.Console");
        
        // Assert
        programType.Should().NotBeNull("Program class should exist");
        
        var mainMethod = programType!.GetMethod("Main", BindingFlags.Static | BindingFlags.NonPublic);
        mainMethod.Should().NotBeNull("Main method should exist");
        mainMethod!.ReturnType.Should().Be(typeof(Task<int>), "Main should return Task<int> (async)");
        
        var parameters = mainMethod.GetParameters();
        parameters.Should().HaveCount(1, "Main should have one parameter");
        parameters[0].ParameterType.Should().Be(typeof(string[]), "Parameter should be string[]");
    }

    [Fact]
    public void Program_ShouldBeStaticClass()
    {
        // Arrange & Act
        var programType = Type.GetType("MartianRobots.Console.Program, MartianRobots.Console");
        
        // Assert
        programType.Should().NotBeNull("Program class should exist");
        programType!.IsAbstract.Should().BeTrue("Program should be static (abstract in IL)");
        programType.IsSealed.Should().BeTrue("Program should be static (sealed in IL)");
    }
}