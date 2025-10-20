namespace MartianRobots.Console;

/// <summary>
/// Interface for robot simulation demo execution.
/// Enables testability and dependency injection.
/// </summary>
public interface IRobotDemo
{
    /// <summary>
    /// Runs the robot simulation with the specified input file.
    /// </summary>
    /// <param name="inputFile">Path to input file, or null to read from stdin</param>
    Task RunAsync(string? inputFile = null);
}
