using System.Diagnostics;

namespace MartianRobots.Abstractions.Models;

/// <summary>
/// Represents the orientation/direction a robot is facing
/// </summary>
public enum Orientation
{
    North = 0,
    East = 1,
    South = 2,
    West = 3
}

/// <summary>
/// Extension methods for the Orientation enum
/// </summary>
public static class OrientationExtensions
{
    /// <summary>
    /// Converts orientation to string representation (N, E, S, W)
    /// </summary>
    public static string ToChar(this Orientation orientation) => orientation switch
    {
        Orientation.North => "N",
        Orientation.East => "E",
        Orientation.South => "S",
        Orientation.West => "W",
        _ => throw new UnreachableException($"Invalid orientation: {orientation}")
    };

    /// <summary>
    /// Parses a character to orientation
    /// </summary>
    public static Orientation FromChar(char orientationChar) => orientationChar switch
    {
        'N' => Orientation.North,
        'E' => Orientation.East,
        'S' => Orientation.South,
        'W' => Orientation.West,
        _ => throw new ArgumentException($"Invalid orientation character: {orientationChar}")
    };

    /// <summary>
    /// Turns the orientation left (counterclockwise)
    /// </summary>
    public static Orientation TurnLeft(this Orientation orientation) => orientation switch
    {
        Orientation.North => Orientation.West,
        Orientation.East => Orientation.North,
        Orientation.South => Orientation.East,
        Orientation.West => Orientation.South,
        _ => throw new UnreachableException($"Invalid orientation: {orientation}")
    };

    /// <summary>
    /// Turns the orientation right (clockwise)
    /// </summary>
    public static Orientation TurnRight(this Orientation orientation) => orientation switch
    {
        Orientation.North => Orientation.East,
        Orientation.East => Orientation.South,
        Orientation.South => Orientation.West,
        Orientation.West => Orientation.North,
        _ => throw new UnreachableException($"Invalid orientation: {orientation}")
    };

    /// <summary>
    /// Gets the movement delta for moving forward in this orientation
    /// </summary>
    public static Position GetMovementDelta(this Orientation orientation) => orientation switch
    {
        Orientation.North => new(0, 1),
        Orientation.East => new(1, 0),
        Orientation.South => new(0, -1),
        Orientation.West => new(-1, 0),
        _ => throw new UnreachableException($"Invalid orientation: {orientation}")
    };
}