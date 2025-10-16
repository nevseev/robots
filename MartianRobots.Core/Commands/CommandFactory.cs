using System.Collections.Frozen;

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
    /// <returns>The corresponding command instance</returns>
    /// <exception cref="ArgumentException">Thrown when an invalid instruction is provided</exception>
    public static IRobotCommand GetCommand(char instruction)
    {
        if (Commands.TryGetValue(instruction, out var command))
            return command;
        
        throw new ArgumentException($"Invalid instruction: {instruction}");
    }

    /// <summary>
    /// Creates a sequence of commands from an instruction string
    /// </summary>
    /// <param name="instructions">String of instruction characters</param>
    /// <returns>List of command instances</returns>
    public static List<IRobotCommand> CreateCommands(string instructions) =>
        [.. instructions.Select(GetCommand)];

    /// <summary>
    /// Gets all supported instruction characters
    /// </summary>
    public static IEnumerable<char> SupportedInstructions => Commands.Keys;
}