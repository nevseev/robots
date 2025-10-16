using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MartianRobots.Core.Communication;

namespace MartianRobots.Tests.Integration.Communication;

/// <summary>
/// Integration tests for the complete robot communication system
/// </summary>
public class RobotCommunicationIntegrationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IRobotCommunicationService _communicationService;
    private readonly ResilientRobotController _resilientController;

    public RobotCommunicationIntegrationTests()
    {
        var services = new ServiceCollection();
        
        // Add resilience services
        services.AddResilienceEnricher();
        
        // Add test configuration with no failures for deterministic tests
        services.AddSingleton(new RobotCommunicationOptions
        {
            BaseDelay = TimeSpan.FromMilliseconds(10), // Fast for tests
            MaxRandomDelay = TimeSpan.FromMilliseconds(20),
            FailureProbability = 0.0, // No random failures in integration tests
            CommandTimeout = TimeSpan.FromSeconds(5),
            MaxRetryAttempts = 3,
            CircuitBreakerThreshold = 5,
            CircuitBreakerSamplingDuration = TimeSpan.FromSeconds(30),
            CircuitBreakerMinimumThroughput = 10
        });

        // Add communication services
        services.AddSingleton<IRobotCommunicationService, RobotCommunicationService>();
        services.AddSingleton<RobotCommunicationService>();
        services.AddSingleton<ResilientRobotController>();
        services.AddSingleton<ILogger<RobotCommunicationService>>(NullLogger<RobotCommunicationService>.Instance);
        services.AddSingleton<ILogger<ResilientRobotController>>(NullLogger<ResilientRobotController>.Instance);

        _serviceProvider = services.BuildServiceProvider();
        _communicationService = _serviceProvider.GetRequiredService<IRobotCommunicationService>();
        _resilientController = _serviceProvider.GetRequiredService<ResilientRobotController>();
    }

    [Fact]
    public async Task EndToEndScenario_MultipleRobots_ShouldWorkCorrectly()
    {
        // Arrange
        var robots = new[]
        {
            ("INTEGRATION-ROBOT-1", new Position(1, 1), Orientation.North),
            ("INTEGRATION-ROBOT-2", new Position(2, 2), Orientation.East),
            ("INTEGRATION-ROBOT-3", new Position(3, 3), Orientation.South)
        };

        // Act & Assert - Connect all robots
        foreach (var (id, position, orientation) in robots)
        {
            var connected = await _resilientController.ConnectRobotAsync(id, position, orientation);
            Assert.True(connected, $"Failed to connect robot {id}");
        }

        // Act & Assert - Check all robots are healthy
        foreach (var (id, _, _) in robots)
        {
            var isHealthy = await _resilientController.HealthCheckRobotAsync(id);
            Assert.True(isHealthy, $"Robot {id} is not healthy");
        }

        // Act & Assert - Execute commands on each robot
        foreach (var (id, _, _) in robots)
        {
            var response = await _resilientController.SendCommandWithResilienceAsync(id, 'R');
            Assert.Equal(CommandStatus.Executed, response.Status);
            Assert.Equal(id, response.RobotId);
            Assert.False(response.IsLost);
        }

        // Act & Assert - Execute instruction sequence
        var sequenceResponse = await _resilientController.ExecuteInstructionSequenceAsync(
            robots[0].Item1, "RFRFRFRF"); // Square pattern

        Assert.Equal(8, sequenceResponse.Count);
        Assert.All(sequenceResponse, r => Assert.Equal(CommandStatus.Executed, r.Status));
        Assert.All(sequenceResponse, r => Assert.False(r.IsLost));
    }

    [Fact]
    public async Task ResilientController_WithTemporaryFailures_ShouldRecoverAutomatically()
    {
        // Arrange - Use service with some failure probability
        var failureProneService = new RobotCommunicationService(
            NullLogger<RobotCommunicationService>.Instance,
            new RobotCommunicationOptions
            {
                BaseDelay = TimeSpan.FromMilliseconds(10),
                MaxRandomDelay = TimeSpan.FromMilliseconds(20),
                FailureProbability = 0.3, // 30% failure rate
                CommandTimeout = TimeSpan.FromSeconds(1),
                MaxRetryAttempts = 5 // More retries to overcome failures
            });

        var resilientController = new ResilientRobotController(
            failureProneService,
            NullLogger<ResilientRobotController>.Instance,
            Options.Create(new RobotCommunicationOptions
            {
                BaseDelay = TimeSpan.FromMilliseconds(10),
                MaxRandomDelay = TimeSpan.FromMilliseconds(20),
                FailureProbability = 0.3,
                CommandTimeout = TimeSpan.FromSeconds(1),
                MaxRetryAttempts = 5
            }));

        const string robotId = "FAILURE-TEST-ROBOT";
        var position = new Position(2, 2);
        var orientation = Orientation.North;

        // Act - Connect robot (may require retries)
        var connected = await resilientController.ConnectRobotAsync(robotId, position, orientation);
        Assert.True(connected);

        // Act - Execute multiple commands (some may fail and retry)
        var commands = new[] { 'R', 'F', 'L', 'F', 'R' };
        var successfulCommands = 0;

        foreach (var command in commands)
        {
            try
            {
                var response = await resilientController.SendCommandWithResilienceAsync(robotId, command);
                if (response.Status == CommandStatus.Executed)
                {
                    successfulCommands++;
                }
            }
            catch (RobotCommandException)
            {
                // Some commands may still fail after all retries - this is expected with 30% failure rate
            }
        }

        // Assert - Should have executed at least some commands successfully due to resilience
        Assert.True(successfulCommands > 0, "Resilient controller should succeed on at least some commands");
    }

    [Fact]
    public async Task CommunicationService_ConcurrentOperations_ShouldBeThreadSafe()
    {
        // Arrange
        const int robotCount = 10;
        const int operationsPerRobot = 5;
        
        var robots = Enumerable.Range(1, robotCount)
            .Select(i => $"CONCURRENT-ROBOT-{i}")
            .ToArray();

        // Act - Connect all robots concurrently
        var connectTasks = robots.Select(async (robotId, index) =>
        {
            var position = new Position(index % 5, index / 5);
            var orientation = (Orientation)(index % 4);
            return await _communicationService.ConnectToRobotAsync(robotId, position, orientation);
        });

        var connectResults = await Task.WhenAll(connectTasks);

        // Assert - All connections should succeed
        Assert.All(connectResults, result => Assert.True(result));

        // Act - Execute commands concurrently on all robots
        var commandTasks = robots.SelectMany(robotId =>
            Enumerable.Range(0, operationsPerRobot).Select(async i =>
            {
                var command = i % 2 == 0 ? 'R' : 'F';
                return await _communicationService.SendCommandAsync(robotId, command);
            })
        );

        var commandResults = await Task.WhenAll(commandTasks);

        // Assert - All commands should execute successfully
        Assert.Equal(robotCount * operationsPerRobot, commandResults.Length);
        Assert.All(commandResults, response => Assert.Equal(CommandStatus.Executed, response.Status));

        // Act - Check all robots are still connected
        var healthTasks = robots.Select(robotId => _communicationService.PingRobotAsync(robotId));
        var healthResults = await Task.WhenAll(healthTasks);

        // Assert - All robots should still be healthy
        Assert.All(healthResults, isHealthy => Assert.True(isHealthy));
    }

    [Fact]
    public async Task CompleteWorkflow_RobotExploration_ShouldTrackStateCorrectly()
    {
        // Arrange
        const string robotId = "EXPLORER-ROBOT";
        var startPosition = new Position(2, 2);
        var startOrientation = Orientation.North;

        // Act - Connect robot
        await _resilientController.ConnectRobotAsync(robotId, startPosition, startOrientation);
        
        // Act - Get initial state
        var initialState = await _resilientController.GetRobotStateAsync(robotId);
        Assert.NotNull(initialState);
        Assert.Equal(startPosition, initialState.Position);
        Assert.Equal(startOrientation, initialState.Orientation);
        Assert.Equal(ConnectionState.Connected, initialState.ConnectionState);

        // Act - Execute exploration sequence
        var explorationCommands = "RFLFLFRF"; // Complex movement pattern
        var responses = await _resilientController.ExecuteInstructionSequenceAsync(robotId, explorationCommands);

        // Assert - All commands should execute
        Assert.Equal(explorationCommands.Length, responses.Count);
        Assert.All(responses, r => Assert.Equal(CommandStatus.Executed, r.Status));

        // Act - Get final state
        var finalState = await _resilientController.GetRobotStateAsync(robotId);
        Assert.NotNull(finalState);
        Assert.Equal(ConnectionState.Connected, finalState.ConnectionState);

        // Assert - Final state should match last response
        var lastResponse = responses.Last();
        Assert.Equal(lastResponse.NewPosition, finalState.Position);
        Assert.Equal(lastResponse.NewOrientation, finalState.Orientation);
        Assert.Equal(lastResponse.IsLost, finalState.IsLost);
    }

    [Fact]
    public async Task BoundaryTesting_RobotMovingOffGrid_ShouldBeLost()
    {
        // Arrange
        const string robotId = "BOUNDARY-ROBOT";
        var edgePosition = new Position(4, 4); // Near edge of default 5x5 grid
        var orientation = Orientation.North;

        // Act - Connect robot at edge
        await _resilientController.ConnectRobotAsync(robotId, edgePosition, orientation);

        // Act - Try to move off the grid
        var response = await _resilientController.SendCommandWithResilienceAsync(robotId, 'F');

        // Assert - Robot should be lost but command should execute
        Assert.Equal(CommandStatus.Executed, response.Status);
        Assert.True(response.IsLost);
        Assert.Equal(edgePosition, response.NewPosition); // Should stay at last valid position

        // Act - Check robot state
        var robotState = await _resilientController.GetRobotStateAsync(robotId);
        Assert.NotNull(robotState);
        Assert.True(robotState.IsLost);
        Assert.Equal(edgePosition, robotState.Position);
    }

    [Fact]
    public async Task ServiceLifecycle_ConnectDisconnectMultipleTimes_ShouldWorkCorrectly()
    {
        // Arrange
        const string robotId = "LIFECYCLE-ROBOT";
        var position = new Position(1, 1);
        var orientation = Orientation.East;

        // Act & Assert - Multiple connect/disconnect cycles
        for (var cycle = 0; cycle < 3; cycle++)
        {
            // Connect
            var connected = await _communicationService.ConnectToRobotAsync(robotId, position, orientation);
            Assert.True(connected, $"Failed to connect in cycle {cycle}");

            // Verify connected
            var isHealthy = await _communicationService.PingRobotAsync(robotId);
            Assert.True(isHealthy, $"Robot not healthy after connect in cycle {cycle}");

            // Execute command
            var response = await _communicationService.SendCommandAsync(robotId, 'R');
            Assert.Equal(CommandStatus.Executed, response.Status);

            // Disconnect
            await _communicationService.DisconnectFromRobotAsync(robotId);

            // Verify disconnected
            var state = await _communicationService.GetRobotStateAsync(robotId);
            Assert.Null(state); // Should be removed after disconnect
        }
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
        GC.SuppressFinalize(this);
    }
}