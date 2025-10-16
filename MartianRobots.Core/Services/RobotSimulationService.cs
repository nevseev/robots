using MartianRobots.Core.Parsing;
using MartianRobots.Core.Validation;
using MartianRobots.Core.Commands;

namespace MartianRobots.Core.Services;

/// <summary>
/// Concrete implementation of robot simulation using Template Method pattern
/// Simplified following KISS principle by delegating to specialized classes
/// </summary>
public sealed class RobotSimulationService : RobotSimulationTemplate
{
    /// <summary>
    /// Parses grid dimensions from the first line of input
    /// </summary>
    protected override MarsGrid ParseGrid(string gridLine) => InputParser.ParseGrid(gridLine);

    /// <summary>
    /// Parses a robot from a position line
    /// </summary>
    protected override Robot ParseRobot(string positionLine) => InputParser.ParseRobot(positionLine);

    /// <summary>
    /// Validates instruction string
    /// </summary>
    protected override void ValidateInstructions(string instructions) => InputValidator.ValidateInstructions(instructions);

    /// <summary>
    /// Enhanced input validation using dedicated validator
    /// </summary>
    protected override void ValidateInput(string[] input) => InputValidator.ValidateInputStructure(input);

    /// <summary>
    /// Creates commands from instruction string
    /// </summary>
    protected override IEnumerable<IRobotCommand> CreateCommands(string instructions) => CommandFactory.CreateCommands(instructions);

    /// <summary>
    /// Static method for compatibility with existing code
    /// </summary>
    public static new List<string> SimulateRobots(string[] input)
    {
        var service = new RobotSimulationService();
        return ((RobotSimulationTemplate)service).SimulateRobots(input);
    }
}