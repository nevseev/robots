using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MartianRobots.Console;

namespace MartianRobots.Tests.Console;

/// <summary>
/// Tests for Application orchestration logic - tests the order of operations without running real services.
/// These are FAST unit tests that verify the application flow using mocks.
/// </summary>
public class ApplicationTests
{
    [Fact]
    public async Task RunAsync_ShouldLogStartupMessage()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<Application>>();
        var mockDemo = new Mock<IRobotDemo>();
        
        var serviceProvider = BuildServiceProvider(mockDemo.Object);
        var app = new Application(serviceProvider, mockLogger.Object);

        // Act
        await app.RunAsync(null);

        // Assert
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Mars Robot Communication System")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "Should log startup message");
    }

    [Fact]
    public async Task RunAsync_ShouldGetRobotDemoFromServiceProvider()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<Application>>();
        var mockDemo = new Mock<IRobotDemo>();
        
        // Setup to track calls
        var wasCalled = false;
        mockDemo.Setup(x => x.RunAsync(It.IsAny<string>()))
            .Callback(() => wasCalled = true)
            .Returns(Task.CompletedTask);
        
        var serviceProvider = BuildServiceProvider(mockDemo.Object);
        var app = new Application(serviceProvider, mockLogger.Object);

        // Act
        await app.RunAsync(null);

        // Assert
        wasCalled.Should().BeTrue("Should retrieve and execute IRobotDemo from DI");
    }

    [Fact]
    public async Task RunAsync_ShouldPassInputFileToDemo()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<Application>>();
        var mockDemo = new Mock<IRobotDemo>();
        
        string? passedFile = null;
        mockDemo.Setup(x => x.RunAsync(It.IsAny<string>()))
            .Callback<string?>(file => passedFile = file)
            .Returns(Task.CompletedTask);
        
        var serviceProvider = BuildServiceProvider(mockDemo.Object);
        var app = new Application(serviceProvider, mockLogger.Object);
        var inputFile = "test-input.txt";

        // Act
        await app.RunAsync(inputFile);

        // Assert
        passedFile.Should().Be(inputFile, "Should pass input file to demo");
    }

    [Fact]
    public async Task RunAsync_OnSuccess_ShouldReturnZero()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<Application>>();
        var mockDemo = new Mock<IRobotDemo>();
        
        var serviceProvider = BuildServiceProvider(mockDemo.Object);
        var app = new Application(serviceProvider, mockLogger.Object);

        // Act
        var exitCode = await app.RunAsync(null);

        // Assert
        exitCode.Should().Be(0, "Should return exit code 0 on success");
    }

    [Fact]
    public async Task RunAsync_WhenDemoThrows_ShouldLogCriticalAndReturnOne()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<Application>>();
        var mockDemo = new Mock<IRobotDemo>();
        
        var expectedException = new InvalidOperationException("Test exception");
        mockDemo.Setup(x => x.RunAsync(It.IsAny<string>()))
            .ThrowsAsync(expectedException);
        
        var serviceProvider = BuildServiceProvider(mockDemo.Object);
        var app = new Application(serviceProvider, mockLogger.Object);

        // Act
        var exitCode = await app.RunAsync(null);

        // Assert
        exitCode.Should().Be(1, "Should return exit code 1 on exception");
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Critical,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Unhandled exception")),
                expectedException,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "Should log critical error with exception");
    }

    [Fact]
    public async Task RunAsync_ShouldExecuteInCorrectOrder()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<Application>>();
        var mockDemo = new Mock<IRobotDemo>();
        
        var callOrder = new List<string>();
        
        mockLogger.Setup(x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Mars Robot")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
            .Callback(() => callOrder.Add("Log startup"));
        
        mockDemo.Setup(x => x.RunAsync(It.IsAny<string>()))
            .Callback(() => callOrder.Add("Run demo"))
            .Returns(Task.CompletedTask);
        
        var serviceProvider = BuildServiceProvider(mockDemo.Object);
        var app = new Application(serviceProvider, mockLogger.Object);

        // Act
        await app.RunAsync(null);

        // Assert
        callOrder.Should().Equal(new[] { "Log startup", "Run demo" }, 
            "Should execute in correct order: log startup, then run demo");
    }

    [Fact]
    public async Task RunAsync_WhenDemoThrows_ShouldNotPropagateException()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<Application>>();
        var mockDemo = new Mock<IRobotDemo>();
        
        mockDemo.Setup(x => x.RunAsync(It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("Test exception"));
        
        var serviceProvider = BuildServiceProvider(mockDemo.Object);
        var app = new Application(serviceProvider, mockLogger.Object);

        // Act
        Func<Task> act = async () => await app.RunAsync(null);

        // Assert
        await act.Should().NotThrowAsync("Application should catch and handle all exceptions");
    }

    [Fact]
    public async Task RunAsync_WithNullInputFile_ShouldPassNullToDemo()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<Application>>();
        var mockDemo = new Mock<IRobotDemo>();
        
        string? passedFile = "NOT_NULL";
        mockDemo.Setup(x => x.RunAsync(It.IsAny<string>()))
            .Callback<string?>(file => passedFile = file)
            .Returns(Task.CompletedTask);
        
        var serviceProvider = BuildServiceProvider(mockDemo.Object);
        var app = new Application(serviceProvider, mockLogger.Object);

        // Act
        await app.RunAsync(null);

        // Assert
        passedFile.Should().BeNull("Should pass null input file to demo (stdin mode)");
    }

    [Theory]
    [InlineData("input.txt")]
    [InlineData("data/robots.txt")]
    [InlineData("/absolute/path/file.txt")]
    public async Task RunAsync_WithVariousInputFiles_ShouldPassToDemo(string inputFile)
    {
        // Arrange
        var mockLogger = new Mock<ILogger<Application>>();
        var mockDemo = new Mock<IRobotDemo>();
        
        string? passedFile = null;
        mockDemo.Setup(x => x.RunAsync(It.IsAny<string>()))
            .Callback<string?>(file => passedFile = file)
            .Returns(Task.CompletedTask);
        
        var serviceProvider = BuildServiceProvider(mockDemo.Object);
        var app = new Application(serviceProvider, mockLogger.Object);

        // Act
        await app.RunAsync(inputFile);

        // Assert
        passedFile.Should().Be(inputFile, $"Should pass input file '{inputFile}' to demo");
    }

    private static IServiceProvider BuildServiceProvider(IRobotDemo demo)
    {
        var services = new ServiceCollection();
        services.AddSingleton(demo);
        return services.BuildServiceProvider();
    }
}
