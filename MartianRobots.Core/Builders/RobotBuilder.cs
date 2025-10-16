namespace MartianRobots.Core.Builders;

/// <summary>
/// Builder for creating robots with various configurations
/// </summary>
public sealed class RobotBuilder
{
    private Position _position;
    private Orientation _orientation = Orientation.North;

    /// <summary>
    /// Sets the robot's position
    /// </summary>
    public RobotBuilder AtPosition(int x, int y)
    {
        _position = new Position(x, y);
        return this;
    }

    /// <summary>
    /// Sets the robot's orientation
    /// </summary>
    public RobotBuilder Facing(Orientation orientation)
    {
        _orientation = orientation;
        return this;
    }

    /// <summary>
    /// Builds the robot with the configured settings
    /// </summary>
    public Robot Build() => new(_position, _orientation);
}

/// <summary>
/// Builder for creating Mars grid with validation
/// </summary>
public sealed class MarsGridBuilder
{
    private int _maxX;
    private int _maxY;

    /// <summary>
    /// Sets the grid dimensions
    /// </summary>
    public MarsGridBuilder WithDimensions(int maxX, int maxY)
    {
        _maxX = maxX;
        _maxY = maxY;
        return this;
    }

    /// <summary>
    /// Builds the grid with the configured dimensions
    /// </summary>
    public MarsGrid Build() => new(_maxX, _maxY);
}