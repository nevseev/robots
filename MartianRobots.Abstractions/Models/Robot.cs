namespace MartianRobots.Abstractions.Models;

/// <summary>
/// Represents a robot with position, orientation, and lost status
/// </summary>
public sealed class Robot(Position position, Orientation orientation)
{
    public Position Position { get; private set; } = position;
    public Orientation Orientation { get; private set; } = orientation;
    public bool IsLost { get; private set; }

    /// <summary>
    /// Turns the robot left
    /// </summary>
    public void TurnLeft()
    {
        if (!IsLost)
        {
            Orientation = Orientation.TurnLeft();
        }
    }

    /// <summary>
    /// Turns the robot right
    /// </summary>
    public void TurnRight()
    {
        if (!IsLost)
        {
            Orientation = Orientation.TurnRight();
        }
    }

    /// <summary>
    /// Attempts to move the robot forward
    /// </summary>
    /// <param name="newPosition">The new position if the move is valid</param>
    /// <returns>True if the robot should move to the new position, false if the move should be ignored</returns>
    public bool TryMoveForward(out Position newPosition)
    {
        if (IsLost)
        {
            newPosition = Position;
            return false;
        }

        var delta = Orientation.GetMovementDelta();
        newPosition = new(Position.X + delta.X, Position.Y + delta.Y);
        return true;
    }

    /// <summary>
    /// Updates the robot's position
    /// </summary>
    public void UpdatePosition(Position newPosition)
    {
        if (!IsLost)
        {
            Position = newPosition;
        }
    }

    /// <summary>
    /// Marks the robot as lost
    /// </summary>
    public void MarkAsLost() => IsLost = true;

    /// <summary>
    /// Returns the robot's status as a string
    /// </summary>
    public override string ToString()
    {
        var result = $"{Position} {Orientation.ToChar()}";
        return IsLost ? $"{result} LOST" : result;
    }
}