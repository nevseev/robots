using MartianRobots.Abstractions.Models;

namespace MartianRobots.Abstractions.Strategies;

/// <summary>
/// Strategy interface for robot movement behaviors
/// </summary>
public interface IMovementStrategy
{
    /// <summary>
    /// Attempts to move the robot according to this strategy
    /// </summary>
    /// <param name="robot">The robot to move</param>
    /// <param name="grid">The grid the robot operates in</param>
    /// <returns>True if the robot moved successfully, false if it was lost or blocked</returns>
    bool TryMove(Robot robot, MarsGrid grid);
}

/// <summary>
/// Abstract base class for movement strategies implementing common logic
/// Follows DRY principle by centralizing shared movement logic
/// </summary>
public abstract class MovementStrategyBase : IMovementStrategy
{
    public bool TryMove(Robot robot, MarsGrid grid)
    {
        if (!robot.TryMoveForward(out Position newPosition))
            return false;

        if (grid.IsValidPosition(newPosition))
        {
            robot.UpdatePosition(newPosition);
            return true;
        }

        // Delegate boundary handling to concrete strategies
        return HandleBoundaryCollision(robot, grid, newPosition);
    }

    /// <summary>
    /// Handles what happens when a robot tries to move outside the grid
    /// </summary>
    /// <param name="robot">The robot attempting to move</param>
    /// <param name="grid">The grid the robot operates in</param>
    /// <param name="targetPosition">The position the robot was trying to reach</param>
    /// <returns>True if the robot should continue, false if it was lost</returns>
    protected abstract bool HandleBoundaryCollision(Robot robot, MarsGrid grid, Position targetPosition);
}