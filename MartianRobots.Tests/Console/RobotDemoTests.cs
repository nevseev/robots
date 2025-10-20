using Microsoft.Extensions.Logging;
using MartianRobots.Abstractions.Models;
using MartianRobots.Console;
using MartianRobots.Core.Communication;
using Moq;

namespace MartianRobots.Tests.Console;

public class RobotDemoTests : IDisposable
{
    private readonly Mock<IResilientRobotController> _mockController;
    private readonly Mock<ILogger<RobotDemo>> _mockLogger;
    private readonly RobotDemo _demo;
    private readonly string _testFilePath;

    public RobotDemoTests()
    {
        _mockController = new Mock<IResilientRobotController>();
        _mockLogger = new Mock<ILogger<RobotDemo>>();
        _demo = new RobotDemo(_mockController.Object, _mockLogger.Object);
        
        // Create a temporary test file
        _testFilePath = Path.Combine(Path.GetTempPath(), $"robot-test-{Guid.NewGuid()}.txt");
    }

    public void Dispose()
    {
        if (File.Exists(_testFilePath))
        {
            File.Delete(_testFilePath);
        }
    }

    [Fact]
    public async Task RunAsync_WithValidFile_ShouldProcessRobots()
    {
        // Arrange
        var input = """
            5 3
            1 1 E
            RFRFRFRF
            """;
        await File.WriteAllTextAsync(_testFilePath, input);

        _mockController
            .Setup(c => c.ConnectRobotAsync(
                It.IsAny<string>(),
                It.IsAny<Position>(),
                It.IsAny<Orientation>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var response = new CommandResponse
        {
            Status = CommandStatus.Executed,
            NewPosition = new Position(1, 1),
            NewOrientation = Orientation.East,
            IsLost = false
        };

        _mockController
            .Setup(c => c.ExecuteInstructionSequenceAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<MarsGrid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CommandResponse> { response });

        // Act
        await _demo.RunAsync(_testFilePath);

        // Assert
        _mockController.Verify(c => c.ConnectRobotAsync(
            "MARS-ROVER-1",
            It.Is<Position>(p => p.X == 1 && p.Y == 1),
            Orientation.East,
            It.IsAny<CancellationToken>()), Times.Once);

        _mockController.Verify(c => c.ExecuteInstructionSequenceAsync(
            "MARS-ROVER-1",
            "RFRFRFRF",
            It.IsAny<MarsGrid>(),
            It.IsAny<CancellationToken>()), Times.Once);

        _mockController.Verify(c => c.Dispose(), Times.Once);
    }

    [Fact]
    public async Task RunAsync_WithMultipleRobots_ShouldProcessAll()
    {
        // Arrange
        var input = """
            5 3
            1 1 E
            RFRFRFRF
            3 2 N
            FRRFLLFFRRFLL
            """;
        await File.WriteAllTextAsync(_testFilePath, input);

        _mockController
            .Setup(c => c.ConnectRobotAsync(
                It.IsAny<string>(),
                It.IsAny<Position>(),
                It.IsAny<Orientation>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var response = new CommandResponse
        {
            Status = CommandStatus.Executed,
            NewPosition = new Position(1, 1),
            NewOrientation = Orientation.East,
            IsLost = false
        };

        _mockController
            .Setup(c => c.ExecuteInstructionSequenceAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<MarsGrid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CommandResponse> { response });

        // Act
        await _demo.RunAsync(_testFilePath);

        // Assert
        _mockController.Verify(c => c.ConnectRobotAsync(
            "MARS-ROVER-1",
            It.Is<Position>(p => p.X == 1 && p.Y == 1),
            Orientation.East,
            It.IsAny<CancellationToken>()), Times.Once);

        _mockController.Verify(c => c.ConnectRobotAsync(
            "MARS-ROVER-2",
            It.Is<Position>(p => p.X == 3 && p.Y == 2),
            Orientation.North,
            It.IsAny<CancellationToken>()), Times.Once);

        _mockController.Verify(c => c.ExecuteInstructionSequenceAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<MarsGrid>(),
            It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task RunAsync_WhenConnectionFails_ShouldLogErrorAndNotExecuteCommands()
    {
        // Arrange
        var input = """
            5 3
            1 1 E
            RFRFRFRF
            """;
        await File.WriteAllTextAsync(_testFilePath, input);

        _mockController
            .Setup(c => c.ConnectRobotAsync(
                It.IsAny<string>(),
                It.IsAny<Position>(),
                It.IsAny<Orientation>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        await _demo.RunAsync(_testFilePath);

        // Assert
        _mockController.Verify(c => c.ConnectRobotAsync(
            It.IsAny<string>(),
            It.IsAny<Position>(),
            It.IsAny<Orientation>(),
            It.IsAny<CancellationToken>()), Times.Once);

        _mockController.Verify(c => c.ExecuteInstructionSequenceAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<MarsGrid>(),
            It.IsAny<CancellationToken>()), Times.Never);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to connect")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task RunAsync_WhenRobotIsLost_ShouldLogWarning()
    {
        // Arrange
        var input = """
            5 3
            3 3 N
            FFF
            """;
        await File.WriteAllTextAsync(_testFilePath, input);

        _mockController
            .Setup(c => c.ConnectRobotAsync(
                It.IsAny<string>(),
                It.IsAny<Position>(),
                It.IsAny<Orientation>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var lostResponse = new CommandResponse
        {
            Status = CommandStatus.Executed,
            NewPosition = new Position(3, 4),
            NewOrientation = Orientation.North,
            IsLost = true
        };

        _mockController
            .Setup(c => c.ExecuteInstructionSequenceAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<MarsGrid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CommandResponse> { lostResponse });

        // Act
        await _demo.RunAsync(_testFilePath);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("LOST")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task RunAsync_WithNoSuccessfulCommands_ShouldLogError()
    {
        // Arrange
        var input = """
            5 3
            1 1 E
            RFRFRFRF
            """;
        await File.WriteAllTextAsync(_testFilePath, input);

        _mockController
            .Setup(c => c.ConnectRobotAsync(
                It.IsAny<string>(),
                It.IsAny<Position>(),
                It.IsAny<Orientation>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockController
            .Setup(c => c.ExecuteInstructionSequenceAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<MarsGrid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CommandResponse>());

        // Act
        await _demo.RunAsync(_testFilePath);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No commands were executed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task RunAsync_WithFileNotFound_ShouldLogErrorAndReturn()
    {
        // Arrange
        var nonExistentFile = Path.Combine(Path.GetTempPath(), $"does-not-exist-{Guid.NewGuid()}.txt");

        // Act
        await _demo.RunAsync(nonExistentFile);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Input file not found")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockController.Verify(c => c.ConnectRobotAsync(
            It.IsAny<string>(),
            It.IsAny<Position>(),
            It.IsAny<Orientation>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RunAsync_WithEmptyInput_ShouldLogErrorAndReturn()
    {
        // Arrange
        await File.WriteAllTextAsync(_testFilePath, "");

        // Act
        await _demo.RunAsync(_testFilePath);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No input provided")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockController.Verify(c => c.Dispose(), Times.Once);
    }

    [Fact]
    public async Task RunAsync_WithIncompleteRobotData_ShouldLogWarning()
    {
        // Arrange
        var input = """
            5 3
            1 1 E
            """; // Missing instructions line
        await File.WriteAllTextAsync(_testFilePath, input);

        // Act
        await _demo.RunAsync(_testFilePath);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Incomplete robot data")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task RunAsync_WhenRobotScenarioThrows_ShouldLogErrorAndContinue()
    {
        // Arrange
        var input = """
            5 3
            1 1 E
            RFRFRFRF
            """;
        await File.WriteAllTextAsync(_testFilePath, input);

        var expectedException = new InvalidOperationException("Test exception");
        _mockController
            .Setup(c => c.ConnectRobotAsync(
                It.IsAny<string>(),
                It.IsAny<Position>(),
                It.IsAny<Orientation>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // Act - should not throw, handles exception internally
        await _demo.RunAsync(_testFilePath);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error executing scenario")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockController.Verify(c => c.Dispose(), Times.Once);
    }

    [Fact]
    public async Task RunAsync_WithMixedSuccessAndFailedCommands_ShouldLogDebugInfo()
    {
        // Arrange
        var input = """
            5 3
            1 1 E
            RFRFRFRF
            """;
        await File.WriteAllTextAsync(_testFilePath, input);

        _mockController
            .Setup(c => c.ConnectRobotAsync(
                It.IsAny<string>(),
                It.IsAny<Position>(),
                It.IsAny<Orientation>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var responses = new List<CommandResponse>
        {
            new() { Status = CommandStatus.Executed, NewPosition = new Position(1, 1), NewOrientation = Orientation.South, IsLost = false },
            new() { Status = CommandStatus.Failed, NewPosition = new Position(1, 1), NewOrientation = Orientation.South, IsLost = false, ErrorMessage = "Communication error" },
            new() { Status = CommandStatus.Executed, NewPosition = new Position(1, 0), NewOrientation = Orientation.South, IsLost = false }
        };

        _mockController
            .Setup(c => c.ExecuteInstructionSequenceAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<MarsGrid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(responses);

        // Act
        await _demo.RunAsync(_testFilePath);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("succeeded") && v.ToString()!.Contains("failed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task RunAsync_WithWhitespaceLines_ShouldIgnoreThem()
    {
        // Arrange
        var input = """
            
            5 3
            
            1 1 E
            RFRFRFRF
            
            """;
        await File.WriteAllTextAsync(_testFilePath, input);

        _mockController
            .Setup(c => c.ConnectRobotAsync(
                It.IsAny<string>(),
                It.IsAny<Position>(),
                It.IsAny<Orientation>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var response = new CommandResponse
        {
            Status = CommandStatus.Executed,
            NewPosition = new Position(1, 1),
            NewOrientation = Orientation.East,
            IsLost = false
        };

        _mockController
            .Setup(c => c.ExecuteInstructionSequenceAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<MarsGrid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CommandResponse> { response });

        // Act
        await _demo.RunAsync(_testFilePath);

        // Assert
        _mockController.Verify(c => c.ConnectRobotAsync(
            "MARS-ROVER-1",
            It.IsAny<Position>(),
            It.IsAny<Orientation>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RunAsync_AlwaysDisposesController_EvenOnSuccess()
    {
        // Arrange
        var input = """
            5 3
            1 1 E
            RF
            """;
        await File.WriteAllTextAsync(_testFilePath, input);

        _mockController
            .Setup(c => c.ConnectRobotAsync(
                It.IsAny<string>(),
                It.IsAny<Position>(),
                It.IsAny<Orientation>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var response = new CommandResponse
        {
            Status = CommandStatus.Executed,
            NewPosition = new Position(1, 1),
            NewOrientation = Orientation.South,
            IsLost = false
        };

        _mockController
            .Setup(c => c.ExecuteInstructionSequenceAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<MarsGrid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CommandResponse> { response });

        // Act
        await _demo.RunAsync(_testFilePath);

        // Assert
        _mockController.Verify(c => c.Dispose(), Times.Once);
    }

    [Fact]
    public async Task RunAsync_WithInstructionsHavingWhitespace_ShouldTrimThem()
    {
        // Arrange
        var input = """
            5 3
            1 1 E
               RFRFRFRF   
            """;
        await File.WriteAllTextAsync(_testFilePath, input);

        _mockController
            .Setup(c => c.ConnectRobotAsync(
                It.IsAny<string>(),
                It.IsAny<Position>(),
                It.IsAny<Orientation>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var response = new CommandResponse
        {
            Status = CommandStatus.Executed,
            NewPosition = new Position(1, 1),
            NewOrientation = Orientation.East,
            IsLost = false
        };

        _mockController
            .Setup(c => c.ExecuteInstructionSequenceAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<MarsGrid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CommandResponse> { response });

        // Act
        await _demo.RunAsync(_testFilePath);

        // Assert
        _mockController.Verify(c => c.ExecuteInstructionSequenceAsync(
            It.IsAny<string>(),
            "RFRFRFRF", // Should be trimmed
            It.IsAny<MarsGrid>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RunAsync_WithNullFile_ShouldReadFromStdin()
    {
        // Arrange
        var input = "5 3\n1 1 E\nRFRFRFRF\n";
        var inputStream = new StringReader(input);
        System.Console.SetIn(inputStream);

        _mockController
            .Setup(c => c.ConnectRobotAsync(
                It.IsAny<string>(),
                It.IsAny<Position>(),
                It.IsAny<Orientation>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var response = new CommandResponse
        {
            RobotId = "robot1",
            Status = CommandStatus.Executed,
            NewPosition = new Position(1, 1),
            NewOrientation = Orientation.East,
            IsLost = false
        };

        _mockController
            .Setup(c => c.ExecuteInstructionSequenceAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<MarsGrid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CommandResponse> { response });

        // Act
        await _demo.RunAsync(null);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Reading input from stdin")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockController.Verify(c => c.ConnectRobotAsync(
            It.IsAny<string>(),
            It.IsAny<Position>(),
            It.IsAny<Orientation>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RunAsync_WithStdinEmptyLines_ShouldSkipEmptyLines()
    {
        // Arrange
        var input = "5 3\n\n1 1 E\n\nRFRFRFRF\n\n";
        var inputStream = new StringReader(input);
        System.Console.SetIn(inputStream);

        _mockController
            .Setup(c => c.ConnectRobotAsync(
                It.IsAny<string>(),
                It.IsAny<Position>(),
                It.IsAny<Orientation>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var response = new CommandResponse
        {
            RobotId = "robot1",
            Status = CommandStatus.Executed,
            NewPosition = new Position(1, 1),
            NewOrientation = Orientation.East,
            IsLost = false
        };

        _mockController
            .Setup(c => c.ExecuteInstructionSequenceAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<MarsGrid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CommandResponse> { response });

        // Act
        await _demo.RunAsync(null);

        // Assert - should process robot despite empty lines
        _mockController.Verify(c => c.ExecuteInstructionSequenceAsync(
            It.IsAny<string>(),
            "RFRFRFRF",
            It.IsAny<MarsGrid>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RunAsync_WithStdinWhitespaceLines_ShouldIgnoreWhitespace()
    {
        // Arrange
        var input = "5 3\n   \n1 1 E\n\t\nRFRFRFRF\n  \n";
        var inputStream = new StringReader(input);
        System.Console.SetIn(inputStream);

        _mockController
            .Setup(c => c.ConnectRobotAsync(
                It.IsAny<string>(),
                It.IsAny<Position>(),
                It.IsAny<Orientation>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var response = new CommandResponse
        {
            RobotId = "robot1",
            Status = CommandStatus.Executed,
            NewPosition = new Position(1, 1),
            NewOrientation = Orientation.East,
            IsLost = false
        };

        _mockController
            .Setup(c => c.ExecuteInstructionSequenceAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<MarsGrid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CommandResponse> { response });

        // Act
        await _demo.RunAsync(null);

        // Assert
        _mockController.Verify(c => c.ConnectRobotAsync(
            It.IsAny<string>(),
            It.IsAny<Position>(),
            It.IsAny<Orientation>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RunAsync_WhenExceptionOccurs_ShouldLogErrorAndRethrow()
    {
        // Arrange - Create a malformed file that will cause parsing to fail
        var testFilePath = Path.Combine(Path.GetTempPath(), $"robot-test-{Guid.NewGuid()}.txt");
        await File.WriteAllTextAsync(testFilePath, "invalid grid\n1 1 E\nRFRFRFRF\n");

        try
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () => await _demo.RunAsync(testFilePath));

            // Verify LogError was called with "Simulation failed"
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Simulation failed")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);

            // Verify Dispose was still called (finally block)
            _mockController.Verify(c => c.Dispose(), Times.Once);
        }
        finally
        {
            if (File.Exists(testFilePath))
                File.Delete(testFilePath);
        }
    }

    [Fact]
    public async Task RunAsync_WithLostRobot_ShouldLogWarningWithLOST()
    {
        // Arrange
        var input = """
            5 3
            3 3 N
            FFF
            """;
        await File.WriteAllTextAsync(_testFilePath, input);

        _mockController
            .Setup(c => c.ConnectRobotAsync(
                It.IsAny<string>(),
                It.IsAny<Position>(),
                It.IsAny<Orientation>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var lostResponse = new CommandResponse
        {
            Status = CommandStatus.Executed,
            NewPosition = new Position(3, 6),
            NewOrientation = Orientation.North,
            IsLost = true
        };

        _mockController
            .Setup(c => c.ExecuteInstructionSequenceAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<MarsGrid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CommandResponse> { lostResponse });

        // Act
        await _demo.RunAsync(_testFilePath);

        // Assert - Verify Warning log with LOST
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Final Position") && v.ToString()!.Contains("LOST")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task RunAsync_WithFailedCommands_ShouldLogFailedCount()
    {
        // Arrange
        var input = """
            5 3
            1 1 E
            RFRFRFRF
            """;
        await File.WriteAllTextAsync(_testFilePath, input);

        _mockController
            .Setup(c => c.ConnectRobotAsync(
                It.IsAny<string>(),
                It.IsAny<Position>(),
                It.IsAny<Orientation>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var responses = new List<CommandResponse>
        {
            new CommandResponse
            {
                Status = CommandStatus.Executed,
                NewPosition = new Position(1, 2),
                NewOrientation = Orientation.North,
                IsLost = false
            },
            new CommandResponse
            {
                Status = CommandStatus.Failed,
                NewPosition = new Position(1, 2),
                NewOrientation = Orientation.North,
                ErrorMessage = "Command failed",
                IsLost = false
            },
            new CommandResponse
            {
                Status = CommandStatus.Executed,
                NewPosition = new Position(1, 2),
                NewOrientation = Orientation.East,
                IsLost = false
            }
        };

        _mockController
            .Setup(c => c.ExecuteInstructionSequenceAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<MarsGrid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(responses);

        // Act
        await _demo.RunAsync(_testFilePath);

        // Assert - Verify failed count is logged (at Debug level, not Warning)
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("succeeded") && v.ToString()!.Contains("failed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task RunAsync_WithSuccessfulRobot_ShouldLogFinalPositionWithoutLOST()
    {
        // Arrange
        var input = """
            5 3
            1 1 E
            RFRFRF
            """;
        await File.WriteAllTextAsync(_testFilePath, input);

        _mockController
            .Setup(c => c.ConnectRobotAsync(
                It.IsAny<string>(),
                It.IsAny<Position>(),
                It.IsAny<Orientation>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var successResponse = new CommandResponse
        {
            Status = CommandStatus.Executed,
            NewPosition = new Position(2, 2),
            NewOrientation = Orientation.South,
            IsLost = false
        };

        _mockController
            .Setup(c => c.ExecuteInstructionSequenceAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<MarsGrid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CommandResponse> { successResponse });

        // Act
        await _demo.RunAsync(_testFilePath);

        // Assert - Verify Information log without LOST (else branch of IsLost check)
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Final Position") && v.ToString()!.Contains("2") && !v.ToString()!.Contains("LOST")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task RunAsync_WithAllCommandsSuccessful_ShouldLogExecutedCount()
    {
        // Arrange
        var input = """
            5 3
            1 1 E
            RFR
            """;
        await File.WriteAllTextAsync(_testFilePath, input);

        _mockController
            .Setup(c => c.ConnectRobotAsync(
                It.IsAny<string>(),
                It.IsAny<Position>(),
                It.IsAny<Orientation>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // All commands successful, no failures
        var responses = new List<CommandResponse>
        {
            new CommandResponse
            {
                Status = CommandStatus.Executed,
                NewPosition = new Position(1, 1),
                NewOrientation = Orientation.South,
                IsLost = false
            },
            new CommandResponse
            {
                Status = CommandStatus.Executed,
                NewPosition = new Position(2, 1),
                NewOrientation = Orientation.South,
                IsLost = false
            },
            new CommandResponse
            {
                Status = CommandStatus.Executed,
                NewPosition = new Position(2, 1),
                NewOrientation = Orientation.West,
                IsLost = false
            }
        };

        _mockController
            .Setup(c => c.ExecuteInstructionSequenceAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<MarsGrid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(responses);

        // Act
        await _demo.RunAsync(_testFilePath);

        // Assert - Verify Debug log with executed count (else branch of failedCount > 0)
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Commands executed") && v.ToString()!.Contains("3/3")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task RunAsync_WithNullPosition_ShouldLogWithNullValues()
    {
        // Arrange
        var input = """
            5 3
            1 1 E
            F
            """;
        await File.WriteAllTextAsync(_testFilePath, input);

        _mockController
            .Setup(c => c.ConnectRobotAsync(
                It.IsAny<string>(),
                It.IsAny<Position>(),
                It.IsAny<Orientation>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Response with null NewPosition to cover the null-conditional branch
        var responseWithNullPosition = new CommandResponse
        {
            Status = CommandStatus.Executed,
            NewPosition = null, // This triggers the ?. null check branches
            NewOrientation = Orientation.East,
            IsLost = false
        };

        _mockController
            .Setup(c => c.ExecuteInstructionSequenceAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<MarsGrid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CommandResponse> { responseWithNullPosition });

        // Act
        await _demo.RunAsync(_testFilePath);

        // Assert - Verify logging happens even with null position
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Final Position")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task RunAsync_WithLostRobotAndNullPosition_ShouldLogWarningWithNullValues()
    {
        // Arrange
        var input = """
            5 3
            3 3 N
            FFF
            """;
        await File.WriteAllTextAsync(_testFilePath, input);

        _mockController
            .Setup(c => c.ConnectRobotAsync(
                It.IsAny<string>(),
                It.IsAny<Position>(),
                It.IsAny<Orientation>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Lost robot with null position to cover all branches on line 153
        var lostResponseWithNullPosition = new CommandResponse
        {
            Status = CommandStatus.Executed,
            NewPosition = null, // Null to trigger ?. branches
            NewOrientation = Orientation.North,
            IsLost = true // Triggers the if (IsLost) branch
        };

        _mockController
            .Setup(c => c.ExecuteInstructionSequenceAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<MarsGrid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CommandResponse> { lostResponseWithNullPosition });

        // Act
        await _demo.RunAsync(_testFilePath);

        // Assert - Verify Warning log with LOST even when position is null
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("LOST")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
