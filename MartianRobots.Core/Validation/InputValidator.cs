using Microsoft.Extensions.Logging;

namespace MartianRobots.Core.Validation;

/// <summary>
/// Validator for robot input data following Single Responsibility Principle
/// </summary>
public static class InputValidator
{
    private static readonly char[] ValidInstructions = ['L', 'R', 'F'];

    /// <summary>
    /// Validates instruction string with optional logging
    /// </summary>
    /// <param name="instructions">The instruction string to validate</param>
    /// <param name="logger">Optional logger for diagnostics</param>
    /// <exception cref="ArgumentException">Thrown when instructions are invalid</exception>
    public static void ValidateInstructions(string instructions, ILogger? logger)
    {
        logger?.LogDebug("Validating instruction string: {Instructions}", instructions);
        
        if (string.IsNullOrEmpty(instructions))
            throw new ArgumentException("Instruction string cannot be empty");

        if (instructions.Length >= 100)
            throw new ArgumentException("Instruction string must be less than 100 characters");

        if (instructions.Any(c => !ValidInstructions.Contains(c)))
        {
            var invalidChar = instructions.First(c => !ValidInstructions.Contains(c));
            throw new ArgumentException($"Invalid instruction character: {invalidChar}. Only L, R, and F are allowed.");
        }
        
        logger?.LogDebug("Instruction string validation successful: {Instructions}", instructions);
    }

    /// <summary>
    /// Validates grid dimension input with optional logging
    /// </summary>
    /// <param name="gridLine">Line containing grid dimensions</param>
    /// <param name="logger">Optional logger for diagnostics</param>
    /// <exception cref="ArgumentException">Thrown when grid dimensions are invalid</exception>
    public static void ValidateGridLine(string gridLine, ILogger? logger)
    {
        logger?.LogDebug("Validating grid line: {GridLine}", gridLine);
        
        var gridParts = gridLine.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        if (gridParts.Length != 2)
            throw new ArgumentException("Grid dimensions must contain exactly two integers");

        if (!int.TryParse(gridParts[0], out _) || !int.TryParse(gridParts[1], out _))
            throw new ArgumentException("Grid dimensions must be valid integers");
        
        logger?.LogDebug("Grid line validation successful: {GridLine}", gridLine);
    }

    /// <summary>
    /// Validates robot position input with optional logging
    /// </summary>
    /// <param name="positionLine">Line containing robot position and orientation</param>
    /// <param name="logger">Optional logger for diagnostics</param>
    /// <exception cref="ArgumentException">Thrown when position data is invalid</exception>
    public static void ValidateRobotPosition(string positionLine, ILogger? logger)
    {
        logger?.LogDebug("Validating robot position line: {PositionLine}", positionLine);
        
        var parts = positionLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        if (parts.Length != 3)
            throw new ArgumentException("Robot position must contain exactly three parts: x y orientation");

        if (!int.TryParse(parts[0], out _) || !int.TryParse(parts[1], out _))
            throw new ArgumentException("Robot coordinates must be valid integers");

        if (parts[2].Length != 1)
            throw new ArgumentException("Robot orientation must be a single character");

        if (!"NESW".Contains(parts[2][0]))
            throw new ArgumentException($"Invalid orientation character: {parts[2][0]}. Must be N, E, S, or W");
        
        logger?.LogDebug("Robot position validation successful: {PositionLine}", positionLine);
    }

    /// <summary>
    /// Validates the overall input structure with optional logging
    /// </summary>
    /// <param name="input">Input array to validate</param>
    /// <param name="logger">Optional logger for diagnostics</param>
    /// <exception cref="ArgumentException">Thrown when input structure is invalid</exception>
    public static void ValidateInputStructure(string[] input, ILogger? logger)
    {
        logger?.LogDebug("Validating input structure with {LineCount} lines", input?.Length ?? 0);
        
        ArgumentNullException.ThrowIfNull(input);
        
        if (input.Length == 0)
            throw new ArgumentException("Input cannot be empty");

        if (input.Length < 3)
            throw new ArgumentException("Input must contain at least grid dimensions and one robot definition");

        if ((input.Length - 1) % 2 != 0)
            throw new ArgumentException("Each robot must have both position and instruction lines");
        
        logger?.LogDebug("Input structure validation successful: {LineCount} lines", input.Length);
    }
}