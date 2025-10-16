using MartianRobots.Abstractions.Models;

namespace MartianRobots.Abstractions.Commands;

/// <summary>
/// Base interface for robot commands
/// </summary>
public interface IRobotCommand
{
    /// <summary>
    /// Executes the command on the robot within the given grid
    /// </summary>
    /// <param name="robot">The robot to execute the command on</param>
    /// <param name="grid">The grid the robot is operating in</param>
    void Execute(Robot robot, MarsGrid grid);
}