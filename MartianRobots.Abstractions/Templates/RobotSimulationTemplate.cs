using MartianRobots.Abstractions.Commands;
using MartianRobots.Abstractions.Models;

namespace MartianRobots.Abstractions.Templates;

/// <summary>
/// Abstract base class implementing Template Method pattern for robot simulation
/// </summary>
public abstract class RobotSimulationTemplate
{
    /// <summary>
    /// Template method defining the simulation algorithm
    /// </summary>
    public List<string> SimulateRobots(string[] input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ValidateInput(input);
        
        var grid = ParseGrid(input[0]);
        var results = new List<string>();
        
        PreSimulation(grid);
        
        for (int i = 1; i < input.Length; i += 2)
        {
            if (i + 1 >= input.Length)
                throw new ArgumentException($"Incomplete robot data at line {i + 1}");

            var robot = ParseRobot(input[i]);
            var instructions = input[i + 1].Trim();
            
            ValidateRobot(robot, grid);
            ValidateInstructions(instructions);
            
            ProcessRobot(robot, grid, instructions);
            results.Add(FormatResult(robot));
        }
        
        PostSimulation(grid, results);
        
        return results;
    }

    /// <summary>
    /// Validates the input array
    /// </summary>
    protected virtual void ValidateInput(string[] input)
    {
        if (input.Length == 0)
            throw new ArgumentException("Input cannot be empty");
    }

    /// <summary>
    /// Parses the grid from the first line of input
    /// </summary>
    protected abstract MarsGrid ParseGrid(string gridLine);

    /// <summary>
    /// Called before simulation starts
    /// </summary>
    protected virtual void PreSimulation(MarsGrid grid) { }

    /// <summary>
    /// Parses a robot from a position line
    /// </summary>
    protected abstract Robot ParseRobot(string positionLine);

    /// <summary>
    /// Validates the robot's initial state
    /// </summary>
    protected virtual void ValidateRobot(Robot robot, MarsGrid grid)
    {
        grid.ValidateInitialPosition(robot.Position);
    }

    /// <summary>
    /// Validates the instruction string
    /// </summary>
    protected abstract void ValidateInstructions(string instructions);

    /// <summary>
    /// Processes a single robot with its instructions
    /// </summary>
    protected virtual void ProcessRobot(Robot robot, MarsGrid grid, string instructions)
    {
        var commands = CreateCommands(instructions);
        
        foreach (var command in commands)
        {
            command.Execute(robot, grid);
                        
            if (robot.IsLost)
                break;
        }
    }

    /// <summary>
    /// Creates commands from instruction string - to be implemented by concrete classes
    /// </summary>
    protected abstract IEnumerable<IRobotCommand> CreateCommands(string instructions);

    /// <summary>
    /// Formats the final result for a robot
    /// </summary>
    protected virtual string FormatResult(Robot robot) => robot.ToString();

    /// <summary>
    /// Called after simulation completes
    /// </summary>
    protected virtual void PostSimulation(MarsGrid grid, List<string> results) { }
}