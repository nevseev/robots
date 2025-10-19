namespace MartianRobots.Abstractions.Models;

/// <summary>
/// Represents the Mars grid with boundaries and scent tracking
/// </summary>
public sealed class MarsGrid
{
    private readonly int _maxX;
    private readonly int _maxY;
    // Using ConcurrentDictionary as a thread-safe set - the bool value is unused
    private readonly ConcurrentDictionary<Position, bool> _scents = new();

    /// <summary>
    /// Creates a new Mars grid with the specified upper-right coordinates
    /// Lower-left coordinates are assumed to be (0, 0)
    /// </summary>
    /// <param name="maxX">Maximum X coordinate</param>
    /// <param name="maxY">Maximum Y coordinate</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when coordinates are invalid</exception>
    public MarsGrid(int maxX, int maxY)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maxX);
        ArgumentOutOfRangeException.ThrowIfNegative(maxY);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(maxX, 50);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(maxY, 50);

        _maxX = maxX;
        _maxY = maxY;
    }

    /// <summary>
    /// Checks if a position is within the valid grid boundaries
    /// </summary>
    /// <param name="position">The position to check</param>
    /// <returns>True if the position is valid, false otherwise</returns>
    public bool IsValidPosition(Position position) =>
        position.X >= 0 && position.X <= _maxX && 
        position.Y >= 0 && position.Y <= _maxY;

    /// <summary>
    /// Checks if a robot scent exists at the given position
    /// </summary>
    /// <param name="position">The position to check for scent</param>
    /// <returns>True if a scent exists, false otherwise</returns>
    public bool HasScent(Position position) => _scents.ContainsKey(position);

    /// <summary>
    /// Adds a robot scent at the given position
    /// </summary>
    /// <param name="position">The position where the scent should be added</param>
    public void AddScent(Position position) => _scents.TryAdd(position, true);

    /// <summary>
    /// Validates that an initial robot position is within the grid
    /// </summary>
    /// <param name="position">The initial position to validate</param>
    /// <exception cref="ArgumentException">Thrown if the position is outside the grid</exception>
    public void ValidateInitialPosition(Position position)
    {
        if (!IsValidPosition(position))
        {
            throw new ArgumentException($"Initial position ({position.X}, {position.Y}) is outside the grid bounds (0,0) to ({_maxX},{_maxY})");
        }
    }

    /// <summary>
    /// Gets the grid dimensions as a string
    /// </summary>
    public override string ToString() => 
        $"Mars Grid: (0,0) to ({_maxX},{_maxY}), Scents: {_scents.Count}";
}