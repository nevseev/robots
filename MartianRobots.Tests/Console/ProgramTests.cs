using System.Reflection;

namespace MartianRobots.Tests.Console;

/// <summary>
/// Tests for the Program class to verify communication demo startup
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
        mainMethod!.ReturnType.Should().Be(typeof(Task<int>), "Main should return Task<int>");
        
        var parameters = mainMethod.GetParameters();
        parameters.Should().HaveCount(1, "Main should have one parameter");
        parameters[0].ParameterType.Should().Be(typeof(string[]), "Parameter should be string[]");
    }

    [Fact]
    public void CreateHostBuilder_ShouldBeAccessible()
    {
        // Arrange & Act
        var programType = Type.GetType("MartianRobots.Console.Program, MartianRobots.Console");
        
        // Assert
        programType.Should().NotBeNull("Program class should exist");
        
        var createHostBuilderMethod = programType!.GetMethod("CreateHostBuilder", BindingFlags.Static | BindingFlags.NonPublic);
        createHostBuilderMethod.Should().NotBeNull("CreateHostBuilder method should exist");
    }
}