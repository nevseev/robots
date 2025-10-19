using System.Collections.Frozen;
using Microsoft.Extensions.Logging;

namespace MartianRobots.Core.Commands;

/// <summary>
/// Factory for creating robot commands from instruction characters
/// Implements the Flyweight pattern to minimize memory usage by reusing command instances
/// </summary>
public static class CommandFactory
{
    // Commandinstances - shared across all clients
    private static readonly FrozenDictionary<char, IRobotCommand> Commands = new Dictionary<char, IRobotCommand>
    {
        ['L'] = new TurnLeftCommand(),
        ['R'] = new TurnRightCommand(),
        ['F'] = new MoveForwardCommand()
    }.ToFrozenDictionary();

    /// <summary>
    /// Gets a command instance for the given instruction character
    /// </summary>
    /// <param name="instruction">The instruction character (L, R, F)</param>
    /// <param name="logger">Optional logger for diagnostics</param>
    /// <returns>The corresponding command instance</returns>
    /// <exception cref="ArgumentException">Thrown when an invalid instruction is provided</exception>
    public static IRobotCommand GetCommand(char instruction, ILogger? logger = null)
    {
        logger?.LogDebug("Creating command for instruction: {Instruction}", instruction);
        
        if (Commands.TryGetValue(instruction, out var command))
        {
            logger?.LogDebug("Successfully retrieved {CommandType} for instruction {Instruction}", 
                command.GetType().Name, instruction);
            return command;
        }
        
        throw new ArgumentException($"Invalid instruction: {instruction}");
    }

    /// <summary>
    /// Creates a sequence of commands from an instruction string
    /// </summary>
    /// <param name="instructions">String of instruction characters</param>
    /// <param name="logger">Optional logger for diagnostics</param>
    /// <returns>List of command instances</returns>
    public static List<IRobotCommand> CreateCommands(string instructions, ILogger? logger = null)
    {
        logger?.LogDebug("Creating commands for instruction string: {Instructions}", instructions);
        
        var commands = instructions.Select(c => GetCommand(c, logger)).ToList();
        logger?.LogDebug("Successfully created {CommandCount} commands from instruction string: {Instructions}", 
            commands.Count, instructions);
        return commands;
    }
}