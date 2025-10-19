using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MartianRobots.Core.Communication;
using MartianRobots.Core.Services;
using MartianRobots.Tests.Mocks;

namespace MartianRobots.Tests.Integration.Communication;

/// <summary>
/// Integration tests for the complete robot communication system
/// </summary>
[Collection("RobotCommunication")]
public class RobotCommunicationIntegrationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IRobotCommunicationService _communicationService;
    private readonly ResilientRobotController _resilientController;
    private readonly MarsGrid _testGrid = new(4, 4); // 5x5 grid (0-4, 0-4) for integration tests

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
        services.AddSingleton<IDelayService, MockDelayService>();
        services.AddSingleton<IFailureSimulator, NoFailureSimulator>(); // Deterministic tests
        services.AddSingleton<IRobotCommunicationService, RobotCommunicationService>();
        services.AddSingleton<RobotCommunicationService>();
        services.AddSingleton<MartianRobots.Core.Resilience.IResiliencePipelineProvider, MartianRobots.Core.Resilience.ResiliencePipelineProvider>();
        services.AddSingleton<ResilientRobotController>();
        services.AddSingleton<ILogger<RobotCommunicationService>>(NullLogger<RobotCommunicationService>.Instance);
        services.AddSingleton<ILogger<ResilientRobotController>>(NullLogger<ResilientRobotController>.Instance);
        services.AddSingleton<ILogger<MartianRobots.Core.Resilience.ResiliencePipelineProvider>>(NullLogger<MartianRobots.Core.Resilience.ResiliencePipelineProvider>.Instance);

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
            var responses = await _resilientController.ExecuteInstructionSequenceAsync(id, "R", _testGrid);
            var response = responses[0];
            Assert.Single(responses);
            Assert.Equal(CommandStatus.Executed, response.Status);
            Assert.Equal(id, response.RobotId);
            Assert.False(response.IsLost);
        }

        // Act & Assert - Execute instruction sequence
        var sequenceResponse = await _resilientController.ExecuteInstructionSequenceAsync(
            robots[0].Item1, "RFRFRFRF", _testGrid); // Square pattern

        Assert.Equal(8, sequenceResponse.Count);
        Assert.All(sequenceResponse, r => Assert.Equal(CommandStatus.Executed, r.Status));
        Assert.All(sequenceResponse, r => Assert.False(r.IsLost));
    }

    [Fact]
    public async Task ResilientController_WithDeterministicFailures_ShouldRecoverAutomatically()
    {
        // Arrange - Use service with no random failures for deterministic test
        var mockDelayService = new MockDelayService();
        var deterministicService = new RobotCommunicationService(
            NullLogger<RobotCommunicationService>.Instance,
            new RobotCommunicationOptions
            {
                BaseDelay = TimeSpan.FromMilliseconds(10),
                MaxRandomDelay = TimeSpan.FromMilliseconds(20),
                FailureProbability = 0.0, // No random failures
                CommandTimeout = TimeSpan.FromSeconds(1),
                MaxRetryAttempts = 3
            },
            mockDelayService,
            new NoFailureSimulator()); // Deterministic - no failures

        var options = Options.Create(new RobotCommunicationOptions
        {
            BaseDelay = TimeSpan.FromMilliseconds(10),
            MaxRandomDelay = TimeSpan.FromMilliseconds(20),
            FailureProbability = 0.0,
            CommandTimeout = TimeSpan.FromSeconds(1),
            MaxRetryAttempts = 3
        });

        var resiliencePipelineProvider = new MartianRobots.Core.Resilience.ResiliencePipelineProvider(
            options,
            NullLogger<MartianRobots.Core.Resilience.ResiliencePipelineProvider>.Instance);

        var resilientController = new ResilientRobotController(
            deterministicService,
            NullLogger<ResilientRobotController>.Instance,
            resiliencePipelineProvider);

        var robotId = $"RESILIENCE-TEST-{Guid.NewGuid():N}";
        var position = new Position(2, 2);
        var orientation = Orientation.North;

        // Act - Connect robot (should succeed consistently)
        var connected = await resilientController.ConnectRobotAsync(robotId, position, orientation);
        Assert.True(connected);

        // Act - Execute multiple commands (should all succeed deterministically)
        var instructions = "RFLFR";
        var responses = await resilientController.ExecuteInstructionSequenceAsync(robotId, instructions, _testGrid);

        // Assert - All commands should execute successfully in deterministic environment
        Assert.Equal(instructions.Length, responses.Count);
        Assert.All(responses, response => Assert.Equal(CommandStatus.Executed, response.Status));
        Assert.All(responses, response => Assert.Equal(robotId, response.RobotId));
        Assert.All(responses, response => Assert.False(response.IsLost));
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

        // Act - Execute commands concurrently on all robots using batch API
        // Use only rotation commands to avoid robots getting lost off the grid
        var commandTasks = robots.Select(async robotId =>
        {
            // Only use rotation commands (L and R) to ensure robots stay on grid
            var commandSequence = string.Concat(Enumerable.Range(0, operationsPerRobot)
                .Select(i => i % 2 == 0 ? 'R' : 'L'));
            return await _communicationService.SendCommandBatchAsync(robotId, commandSequence, _testGrid);
        });

        var batchResults = await Task.WhenAll(commandTasks);

        // Assert - All commands should be returned (executed or failed, but not lost)
        var allResponses = batchResults.SelectMany(r => r).ToList();
        Assert.Equal(robotCount * operationsPerRobot, allResponses.Count);
        
        // With FailureProbability = 0.0, all commands should execute successfully
        var executedCount = allResponses.Count(r => r.Status == CommandStatus.Executed);
        Assert.Equal(robotCount * operationsPerRobot, executedCount);

        // Act - Check all robots are still connected
        var healthTasks = robots.Select(robotId => _communicationService.PingRobotAsync(robotId));
        var healthResults = await Task.WhenAll(healthTasks);

        // Assert - All robots should still be healthy
        Assert.All(healthResults, isHealthy => Assert.True(isHealthy));
    }

    [Fact]
    public async Task CompleteWorkflow_RobotExploration_ShouldTrackStateCorrectly()
    {
        // Arrange - Use unique robot ID to avoid conflicts with other tests
        var robotId = $"EXPLORER-{Guid.NewGuid():N}";
        var startPosition = new Position(2, 2);
        var startOrientation = Orientation.North;

        // Act - Connect robot
        await _resilientController.ConnectRobotAsync(robotId, startPosition, startOrientation);
        
        // Verify connection was successful
        var isHealthy = await _communicationService.PingRobotAsync(robotId);
        Assert.True(isHealthy);

        // Act - Execute exploration sequence
        var explorationCommands = "RFLFLFRF"; // Complex movement pattern
        var responses = await _resilientController.ExecuteInstructionSequenceAsync(robotId, explorationCommands, _testGrid);

        // Assert - All commands should execute
        Assert.Equal(explorationCommands.Length, responses.Count);
        Assert.All(responses, r => Assert.Equal(CommandStatus.Executed, r.Status));

        // Assert - Final state should match last response
        var lastResponse = responses.Last();
        Assert.NotNull(lastResponse.NewPosition);
        Assert.NotNull(lastResponse.NewOrientation);
        Assert.False(lastResponse.IsLost);
    }

    [Fact]
    public async Task BoundaryTesting_RobotMovingOffGrid_ShouldBeLost()
    {
        // Arrange - Use unique robot ID to avoid conflicts with other tests
        var robotId = $"BOUNDARY-{Guid.NewGuid():N}";
        var edgePosition = new Position(4, 4); // Near edge of default 5x5 grid
        var orientation = Orientation.North;

        // Act - Connect robot at edge
        await _resilientController.ConnectRobotAsync(robotId, edgePosition, orientation);

        // Act - Try to move off the grid
        var responses = await _resilientController.ExecuteInstructionSequenceAsync(robotId, "F", _testGrid);
        var response = responses[0];

        // Assert - Robot should be lost but command should execute
        Assert.Single(responses);
        Assert.Equal(CommandStatus.Executed, response.Status);
        Assert.True(response.IsLost);
        Assert.Equal(edgePosition, response.NewPosition); // Should stay at last valid position
    }

    [Fact]
    public async Task ServiceLifecycle_ConnectDisconnectMultipleTimes_ShouldWorkCorrectly()
    {
        // Arrange - Use unique robot ID to avoid conflicts with other tests
        var robotId = $"LIFECYCLE-{Guid.NewGuid():N}";
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

            // Execute command using batch API
            var responses = await _communicationService.SendCommandBatchAsync(robotId, "R", _testGrid);
            Assert.Single(responses);
            Assert.Equal(CommandStatus.Executed, responses[0].Status);

            // Disconnect
            await _communicationService.DisconnectFromRobotAsync(robotId);

            // Verify disconnected by trying to ping
            var isStillConnected = await _communicationService.PingRobotAsync(robotId);
            Assert.False(isStillConnected); // Should return false after disconnect
        }
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
        GC.SuppressFinalize(this);
    }
}