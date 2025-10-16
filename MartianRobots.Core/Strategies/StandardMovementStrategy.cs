namespace MartianRobots.Core.Strategies;

/// <summary>
/// Standard movement strategy that respects grid boundaries and scents
/// </summary>
public sealed class StandardMovementStrategy : MovementStrategyBase
{
    protected override bool HandleBoundaryCollision(Robot robot, MarsGrid grid, Position targetPosition)
    {
        // Robot would fall off the grid
        if (!grid.HasScent(robot.Position))
        {
            // No previous robot has fallen off from this position
            grid.AddScent(robot.Position);
            robot.MarkAsLost();
            return false;
        }

        // If there's a scent, ignore the command (robot stays in place)
        return true;
    }
}