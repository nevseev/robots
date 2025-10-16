using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MartianRobots.Core.Communication;
using MartianRobots.Abstractions.Models;

namespace MartianRobots.Console.Communication;

/// <summary>
/// Demonstration of real-time robot communication with resilience patterns
/// </summary>
public class RobotCommunicationDemo
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RobotCommunicationDemo> _logger;

    public RobotCommunicationDemo(IServiceProvider serviceProvider, ILogger<RobotCommunicationDemo> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Runs the robot communication demonstration
    /// </summary>
    public async Task RunDemoAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("=== Mars Robot Communication System Demo ===");

        // Get the resilient robot controller
        var robotController = _serviceProvider.GetRequiredService<IResilientRobotController>();

        // Demonstrate connecting to multiple robots
        await DemonstrateRobotConnectionsAsync(robotController, cancellationToken);

        // Demonstrate sending commands with resilience
        await DemonstrateResilientCommandsAsync(robotController, cancellationToken);

        // Demonstrate health monitoring
        await DemonstrateHealthMonitoringAsync(robotController, cancellationToken);

        // Demonstrate instruction sequences
        await DemonstrateInstructionSequencesAsync(robotController, cancellationToken);

        _logger.LogInformation("=== Demo Complete ===");
    }

    private async Task DemonstrateRobotConnectionsAsync(IResilientRobotController controller, CancellationToken cancellationToken)
    {
        _logger.LogInformation("\n--- Robot Connection Demo ---");

        var robots = new[]
        {
            new { Id = "MARS-ROVER-1", Position = new Position(1, 1), Orientation = Orientation.North },
            new { Id = "MARS-ROVER-2", Position = new Position(3, 2), Orientation = Orientation.East },
            new { Id = "MARS-ROVER-3", Position = new Position(0, 0), Orientation = Orientation.South }
        };

        foreach (var robot in robots)
        {
            try
            {
                _logger.LogInformation("Connecting to robot {RobotId}...", robot.Id);
                var connected = await controller.ConnectRobotAsync(robot.Id, robot.Position, robot.Orientation, cancellationToken);
                
                if (connected)
                {
                    _logger.LogInformation("✅ Successfully connected to robot {RobotId}", robot.Id);
                }
                else
                {
                    _logger.LogWarning("❌ Failed to connect to robot {RobotId}", robot.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Exception connecting to robot {RobotId}", robot.Id);
            }

            // Small delay between connections
            await Task.Delay(1000, cancellationToken);
        }
    }

    private async Task DemonstrateResilientCommandsAsync(IResilientRobotController controller, CancellationToken cancellationToken)
    {
        _logger.LogInformation("\n--- Resilient Command Demo ---");

        var robotId = "MARS-ROVER-1";
        var commands = new[] { 'R', 'F', 'R', 'F', 'R', 'F', 'R', 'F' }; // Square pattern

        foreach (var command in commands)
        {
            try
            {
                _logger.LogInformation("Sending command '{Command}' to robot {RobotId}...", command, robotId);
                var response = await controller.SendCommandWithResilienceAsync(robotId, command, cancellationToken);

                switch (response.Status)
                {
                    case CommandStatus.Executed:
                        _logger.LogInformation("✅ Command executed successfully. Robot at ({X}, {Y}) facing {Orientation}", 
                            response.NewPosition?.X, response.NewPosition?.Y, response.NewOrientation);
                        break;
                    case CommandStatus.Failed:
                        _logger.LogWarning("❌ Command failed: {Error}", response.ErrorMessage);
                        break;
                    case CommandStatus.TimedOut:
                        _logger.LogWarning("⏰ Command timed out");
                        break;
                }

                if (response.IsLost)
                {
                    _logger.LogWarning("🔴 Robot {RobotId} is lost!", robotId);
                    break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Exception sending command '{Command}' to robot {RobotId}", command, robotId);
            }

            // Small delay between commands
            await Task.Delay(800, cancellationToken);
        }
    }

    private async Task DemonstrateHealthMonitoringAsync(IResilientRobotController controller, CancellationToken cancellationToken)
    {
        _logger.LogInformation("\n--- Health Monitoring Demo ---");

        var robotIds = new[] { "MARS-ROVER-1", "MARS-ROVER-2", "MARS-ROVER-3" };

        foreach (var robotId in robotIds)
        {
            try
            {
                _logger.LogInformation("Checking health of robot {RobotId}...", robotId);
                var isHealthy = await controller.HealthCheckRobotAsync(robotId, cancellationToken);

                if (isHealthy)
                {
                    _logger.LogInformation("✅ Robot {RobotId} is healthy", robotId);
                    
                    // Get robot state
                    var robotState = await controller.GetRobotStateAsync(robotId, cancellationToken);
                    if (robotState != null)
                    {
                        _logger.LogInformation("📍 Robot {RobotId} status: {Status}, Position: ({X}, {Y}), Orientation: {Orientation}", 
                            robotId, robotState.ConnectionState, robotState.Position.X, robotState.Position.Y, robotState.Orientation);
                    }
                }
                else
                {
                    _logger.LogWarning("❌ Robot {RobotId} is not responding", robotId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Exception checking health of robot {RobotId}", robotId);
            }

            await Task.Delay(500, cancellationToken);
        }
    }

    private async Task DemonstrateInstructionSequencesAsync(IResilientRobotController controller, CancellationToken cancellationToken)
    {
        _logger.LogInformation("\n--- Instruction Sequence Demo ---");

        var robotId = "MARS-ROVER-2";
        var instructionSequences = new[]
        {
            "RFRFRFRF", // Square pattern
            "FRRFLLFFRRFLL", // Complex pattern
            "FFFFFFFFFFFFFFFFFF" // Test boundary (might get lost)
        };

        foreach (var sequence in instructionSequences)
        {
            try
            {
                _logger.LogInformation("Executing instruction sequence '{Sequence}' on robot {RobotId}...", sequence, robotId);
                var responses = await controller.ExecuteInstructionSequenceAsync(robotId, sequence, cancellationToken);

                _logger.LogInformation("Sequence execution completed: {SuccessCount}/{TotalCount} commands successful",
                    responses.Count(r => r.Status == CommandStatus.Executed), sequence.Length);

                // Show final position if available
                var lastSuccessful = responses.LastOrDefault(r => r.Status == CommandStatus.Executed);
                if (lastSuccessful?.NewPosition != null)
                {
                    _logger.LogInformation("📍 Final position: ({X}, {Y}) facing {Orientation}",
                        lastSuccessful.NewPosition.Value.X, lastSuccessful.NewPosition.Value.Y, lastSuccessful.NewOrientation);
                }

                // Check if robot was lost
                if (responses.Any(r => r.IsLost))
                {
                    _logger.LogWarning("🔴 Robot {RobotId} was lost during sequence execution", robotId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Exception executing instruction sequence on robot {RobotId}", robotId);
            }

            _logger.LogInformation(""); // Empty line for readability
            await Task.Delay(2000, cancellationToken);
        }
    }
}

/// <summary>
/// Extension methods for setting up robot communication services
/// </summary>
public static class RobotCommunicationServiceExtensions
{
    public static IServiceCollection AddRobotCommunication(this IServiceCollection services)
    {
        // Add resilience services
        services.AddResilienceEnricher();

        // Add logging (if not already configured)
        services.AddLogging();

        // Add configuration options
        services.AddSingleton(new RobotCommunicationOptions
        {
            BaseDelay = TimeSpan.FromMilliseconds(500),
            MaxRandomDelay = TimeSpan.FromMilliseconds(1000),
            FailureProbability = 0.1,
            MaxRetryAttempts = 3,
            CommandTimeout = TimeSpan.FromSeconds(5)
        });

        // Add communication services
        services.AddSingleton<IRobotCommunicationService, RobotCommunicationService>();
        services.AddSingleton<RobotCommunicationService>();
        services.AddSingleton<IResilientRobotController, ResilientRobotController>();
        services.AddSingleton<ResilientRobotController>();
        services.AddTransient<RobotCommunicationDemo>();

        return services;
    }
}