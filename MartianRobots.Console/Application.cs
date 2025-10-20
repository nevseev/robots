using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MartianRobots.Console;

/// <summary>
/// Main application orchestration logic - testable without running actual services.
/// Separates the execution flow from the entry point to enable unit testing.
/// </summary>
internal sealed class Application(IServiceProvider serviceProvider, ILogger<Application> logger)
{
    /// <summary>
    /// Runs the robot simulation with the specified input file.
    /// Returns exit code: 0 for success, 1 for failure.
    /// </summary>
    public async Task<int> RunAsync(string? inputFile)
    {
        try
        {
            logger.LogInformation("=== Mars Robot Communication System ===");

            // Get and run the demo
            var demo = serviceProvider.GetRequiredService<IRobotDemo>();
            await demo.RunAsync(inputFile);

            return 0;
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Unhandled exception in Mars Robot application");
            return 1;
        }
    }
}
