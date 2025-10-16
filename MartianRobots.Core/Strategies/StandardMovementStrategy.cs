using Microsoft.Extensions.Logging;

namespace MartianRobots.Core.Strategies;

/// <summary>
/// Standard movement strategy that respects grid boundaries and scents
/// </summary>
public sealed class StandardMovementStrategy : MovementStrategyBase
{
    protected override bool HandleBoundaryCollision(Robot robot, MarsGrid grid, Position targetPosition, ILogger? logger)
    {
        logger?.LogDebug("Handling boundary collision for robot at {Position}, target was {TargetPosition}",
            robot.Position, targetPosition);
            
        // Robot would fall off the grid
        if (!grid.HasScent(robot.Position))
        {
            logger?.LogDebug("No scent detected at position {Position}, robot will be lost", robot.Position);
            // No previous robot has fallen off from this position
            grid.AddScent(robot.Position);
            robot.MarkAsLost();
            logger?.LogWarning("Robot lost and scent added at position {Position}", robot.Position);
            return false;
        }

        // If there's a scent, ignore the command (robot stays in place)
        logger?.LogDebug("Scent detected at position {Position}, ignoring movement command", robot.Position);
        return true;
    }
}