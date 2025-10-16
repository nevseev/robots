using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MartianRobots.Console.Communication;
using MartianRobots.Core.Communication;

namespace MartianRobots.Tests.Console.Communication;

public class RobotCommunicationDemoTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly Mock<IResilientRobotController> _mockController;
    private readonly RobotCommunicationDemo _demo;

    public RobotCommunicationDemoTests()
    {
        var mockCommunicationService = new Mock<IRobotCommunicationService>();
        
        var services = new ServiceCollection();
        services.AddResilienceEnricher();
        services.AddSingleton(mockCommunicationService.Object);
        services.AddSingleton<ILogger<ResilientRobotController>>(NullLogger<ResilientRobotController>.Instance);
        
        _serviceProvider = services.BuildServiceProvider();
        
        _mockController = new Mock<IResilientRobotController>();

        var demoServices = new ServiceCollection();
        demoServices.AddSingleton(_mockController.Object);
        demoServices.AddSingleton<ILogger<RobotCommunicationDemo>>(NullLogger<RobotCommunicationDemo>.Instance);
        var demoProvider = demoServices.BuildServiceProvider();

        _demo = new RobotCommunicationDemo(demoProvider, NullLogger<RobotCommunicationDemo>.Instance);
    }

    [Fact]
    public async Task RunDemoAsync_ShouldExecuteWithoutExceptions()
    {
        // Arrange
        SetupMockControllerForSuccessfulDemo();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)); // Increased timeout

        // Act & Assert
        await _demo.RunDemoAsync(cts.Token); // Should not throw
    }

    [Fact]
    public async Task RunDemoAsync_WithCancellation_ShouldRespectCancellationToken()
    {
        // Arrange
        SetupMockControllerForSuccessfulDemo();
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(
            () => _demo.RunDemoAsync(cts.Token));
    }

    [Fact]
    public void RobotCommunicationServiceExtensions_AddRobotCommunication_ShouldRegisterServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddRobotCommunication();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var communicationService = serviceProvider.GetService<IRobotCommunicationService>();
        var resilientController = serviceProvider.GetService<ResilientRobotController>();
        var demo = serviceProvider.GetService<RobotCommunicationDemo>();
        var options = serviceProvider.GetService<RobotCommunicationOptions>();

        Assert.NotNull(communicationService);
        Assert.NotNull(resilientController);
        Assert.NotNull(demo);
        Assert.NotNull(options);
    }

    [Fact]
    public void RobotCommunicationServiceExtensions_AddRobotCommunication_ShouldConfigureOptionsCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddRobotCommunication();
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetService<RobotCommunicationOptions>();

        // Assert
        Assert.NotNull(options);
        Assert.Equal(TimeSpan.FromMilliseconds(500), options.BaseDelay);
        Assert.Equal(TimeSpan.FromMilliseconds(1000), options.MaxRandomDelay);
        Assert.Equal(0.1, options.FailureProbability);
        Assert.Equal(3, options.MaxRetryAttempts);
        Assert.Equal(TimeSpan.FromSeconds(5), options.CommandTimeout);
    }

    [Fact]
    public void RobotCommunicationServiceExtensions_AddRobotCommunication_ShouldReturnServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddRobotCommunication();

        // Assert
        Assert.Same(services, result); // Should return the same instance for fluent interface
    }

    private void SetupMockControllerForSuccessfulDemo()
    {
        // Setup successful connections
        _mockController
            .Setup(c => c.ConnectRobotAsync(It.IsAny<string>(), It.IsAny<Position>(), It.IsAny<Orientation>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Setup successful commands
        _mockController
            .Setup(c => c.SendCommandWithResilienceAsync(It.IsAny<string>(), It.IsAny<char>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CommandResponse
            {
                CommandId = Guid.NewGuid().ToString(),
                RobotId = "TEST-ROBOT",
                Status = CommandStatus.Executed,
                NewPosition = new Position(2, 2),
                NewOrientation = Orientation.North,
                IsLost = false
            });

        // Setup successful health checks
        _mockController
            .Setup(c => c.HealthCheckRobotAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Setup robot state retrieval
        _mockController
            .Setup(c => c.GetRobotStateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RobotInstance
            {
                Id = "TEST-ROBOT",
                Position = new Position(2, 2),
                Orientation = Orientation.North,
                ConnectionState = ConnectionState.Connected,
                IsLost = false
            });

        // Setup instruction sequence execution
        _mockController
            .Setup(c => c.ExecuteInstructionSequenceAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CommandResponse>
            {
                new()
                {
                    CommandId = Guid.NewGuid().ToString(),
                    RobotId = "TEST-ROBOT",
                    Status = CommandStatus.Executed,
                    NewPosition = new Position(3, 3),
                    NewOrientation = Orientation.East,
                    IsLost = false
                }
            });
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
        GC.SuppressFinalize(this);
    }
}