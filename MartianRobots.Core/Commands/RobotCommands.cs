namespace MartianRobots.Core.Commands;

/// <summary>
/// Command to turn the robot left
/// </summary>
public sealed class TurnLeftCommand : IRobotCommand
{
    public void Execute(Robot robot, MarsGrid grid) => robot.TurnLeft();
}

/// <summary>
/// Command to turn the robot right
/// </summary>
public sealed class TurnRightCommand : IRobotCommand
{
    public void Execute(Robot robot, MarsGrid grid) => robot.TurnRight();
}

/// <summary>
/// Command to move the robot forward using standard movement strategy
/// Implements Command pattern with fixed strategy for Flyweight compatibility
/// </summary>
public sealed class MoveForwardCommand : IRobotCommand
{
    private static readonly IMovementStrategy DefaultStrategy = new Strategies.StandardMovementStrategy();

    public void Execute(Robot robot, MarsGrid grid)
    {
        DefaultStrategy.TryMove(robot, grid);
    }
}