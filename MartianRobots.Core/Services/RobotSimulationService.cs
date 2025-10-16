using MartianRobots.Core.Parsing;
using MartianRobots.Core.Validation;
using MartianRobots.Core.Commands;
using Microsoft.Extensions.Logging;

namespace MartianRobots.Core.Services;

/// <summary>
/// Concrete implementation of robot simulation using Template Method pattern
/// Simplified following KISS principle by delegating to specialized classes
/// </summary>
public sealed class RobotSimulationService : RobotSimulationTemplate
{
    private readonly ILogger<RobotSimulationService>? _logger;

    public RobotSimulationService(ILogger<RobotSimulationService>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Static method for backwards compatibility
    /// </summary>
    public static new List<string> SimulateRobots(string[] input)
    {
        var service = new RobotSimulationService();
        return service.SimulateRobotsInternal(input);
    }

    /// <summary>
    /// Static method with logger support
    /// </summary>
    public static List<string> SimulateRobotsWithLogger(string[] input, ILogger<RobotSimulationService>? logger)
    {
        var service = new RobotSimulationService(logger);
        return service.SimulateRobotsInternal(input);
    }

    /// <summary>
    /// Internal method to call the base template method
    /// </summary>
    private List<string> SimulateRobotsInternal(string[] input)
    {
        return base.SimulateRobots(input);
    }

    /// <summary>
    /// Parses grid dimensions from the first line of input
    /// </summary>
    protected override MarsGrid ParseGrid(string gridLine)
    {
        _logger?.LogDebug("Parsing grid from line: {GridLine}", gridLine);
        var grid = InputParser.ParseGrid(gridLine, _logger);
        _logger?.LogInformation("Successfully parsed grid from line: {GridLine}", gridLine);
        return grid;
    }

    /// <summary>
    /// Parses a robot from a position line
    /// </summary>
    protected override Robot ParseRobot(string positionLine)
    {
        _logger?.LogDebug("Parsing robot from line: {PositionLine}", positionLine);
        var robot = InputParser.ParseRobot(positionLine, _logger);
        _logger?.LogDebug("Successfully parsed robot at position ({X}, {Y}) facing {Orientation}", 
            robot.Position.X, robot.Position.Y, robot.Orientation.ToChar());
        return robot;
    }

    /// <summary>
    /// Validates instruction string
    /// </summary>
    protected override void ValidateInstructions(string instructions)
    {
        _logger?.LogDebug("Validating instructions: {Instructions}", instructions);
        InputValidator.ValidateInstructions(instructions, _logger);
        _logger?.LogDebug("Instructions validation successful for: {Instructions}", instructions);
    }

    /// <summary>
    /// Enhanced input validation using dedicated validator
    /// </summary>
    protected override void ValidateInput(string[] input)
    {
        _logger?.LogDebug("Validating input structure with {LineCount} lines", input.Length);
        InputValidator.ValidateInputStructure(input, _logger);
        _logger?.LogDebug("Input structure validation successful");
    }

    /// <summary>
    /// Creates commands from instruction string
    /// </summary>
    protected override IEnumerable<IRobotCommand> CreateCommands(string instructions)
    {
        _logger?.LogDebug("Creating commands from instructions: {Instructions}", instructions);
        var commands = CommandFactory.CreateCommands(instructions, _logger);
        _logger?.LogDebug("Successfully created {CommandCount} commands from instructions: {Instructions}", 
            commands.Count(), instructions);
        return commands;
    }
}