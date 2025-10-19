using MartianRobots.Abstractions.Models;
using Microsoft.Extensions.Logging;

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
    /// <param name="logger">Optional logger for diagnostics</param>
    /// <returns>True if the robot moved successfully, false if it was lost or blocked</returns>
    bool TryMove(Robot robot, MarsGrid grid, ILogger? logger = null);
}

/// <summary>
/// Abstract base class for movement strategies implementing common logic
/// </summary>
public abstract class MovementStrategyBase : IMovementStrategy
{
    public bool TryMove(Robot robot, MarsGrid grid, ILogger? logger = null)
    {
        logger?.LogDebug("Attempting to move robot at position {Position} facing {Orientation}",
            robot.Position, robot.Orientation);
            
        if (!robot.TryMoveForward(out Position newPosition))
        {
            logger?.LogDebug("Robot cannot move forward from position {Position} facing {Orientation}",
                robot.Position, robot.Orientation);
            return false;
        }

        logger?.LogDebug("Target position for robot movement: {TargetPosition}", newPosition);

        if (grid.IsValidPosition(newPosition))
        {
            robot.UpdatePosition(newPosition);
            logger?.LogDebug("Robot successfully moved to position {Position}", newPosition);
            return true;
        }

        logger?.LogDebug("Target position {TargetPosition} is outside grid boundaries, handling boundary collision",
            newPosition);

        // Delegate boundary handling to concrete strategies
        var result = HandleBoundaryCollision(robot, grid, newPosition, logger);
        
        if (result)
        {
            logger?.LogDebug("Boundary collision handled successfully, robot remained at position {Position}",
                robot.Position);
        }
        else
        {
            logger?.LogWarning("Robot lost during boundary collision at position {Position}", robot.Position);
        }
        
        return result;
    }

    /// <summary>
    /// Handles what happens when a robot tries to move outside the grid
    /// </summary>
    /// <param name="robot">The robot attempting to move</param>
    /// <param name="grid">The grid the robot operates in</param>
    /// <param name="targetPosition">The position the robot was trying to reach</param>
    /// <param name="logger">Optional logger for diagnostics</param>
    /// <returns>True if the robot should continue, false if it was lost</returns>
    protected abstract bool HandleBoundaryCollision(Robot robot, MarsGrid grid, Position targetPosition, ILogger? logger);
}