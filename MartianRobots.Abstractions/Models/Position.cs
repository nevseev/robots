namespace MartianRobots.Abstractions.Models;

/// <summary>
/// Represents a position on the Mars grid with X and Y coordinates
/// </summary>
/// <param name="X">The X coordinate</param>
/// <param name="Y">The Y coordinate</param>
public readonly record struct Position(int X, int Y)
{
    /// <summary>
    /// Returns the position as a string in the format "X Y"
    /// </summary>
    public override string ToString() => $"{X} {Y}";
}