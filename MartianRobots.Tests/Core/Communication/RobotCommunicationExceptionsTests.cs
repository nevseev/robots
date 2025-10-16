using MartianRobots.Core.Communication;

namespace MartianRobots.Tests.Core.Communication;

public class RobotCommunicationExceptionsTests
{
    [Fact]
    public void RobotConnectionException_WithMessage_ShouldSetMessageCorrectly()
    {
        // Arrange
        const string message = "Failed to connect to robot";

        // Act
        var exception = new RobotConnectionException(message);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Null(exception.InnerException);
    }

    [Fact]
    public void RobotConnectionException_WithMessageAndInnerException_ShouldSetBothCorrectly()
    {
        // Arrange
        const string message = "Failed to connect to robot";
        var innerException = new InvalidOperationException("Network error");

        // Act
        var exception = new RobotConnectionException(message, innerException);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Equal(innerException, exception.InnerException);
    }

    [Fact]
    public void RobotCommandException_WithMessage_ShouldSetMessageCorrectly()
    {
        // Arrange
        const string message = "Command execution failed";

        // Act
        var exception = new RobotCommandException(message);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Null(exception.InnerException);
    }

    [Fact]
    public void RobotCommandException_WithMessageAndInnerException_ShouldSetBothCorrectly()
    {
        // Arrange
        const string message = "Command execution failed";
        var innerException = new TimeoutException("Command timed out");

        // Act
        var exception = new RobotCommandException(message, innerException);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Equal(innerException, exception.InnerException);
    }

    [Fact]
    public void RobotNotFoundException_WithMessage_ShouldSetMessageCorrectly()
    {
        // Arrange
        const string message = "Robot MARS-ROVER-1 not found";

        // Act
        var exception = new RobotNotFoundException(message);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Null(exception.InnerException);
    }

    [Fact]
    public void RobotNotFoundException_WithMessageAndInnerException_ShouldSetBothCorrectly()
    {
        // Arrange
        const string message = "Robot MARS-ROVER-1 not found";
        var innerException = new ArgumentException("Invalid robot ID");

        // Act
        var exception = new RobotNotFoundException(message, innerException);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Equal(innerException, exception.InnerException);
    }

    [Fact]
    public void AllCommunicationExceptions_ShouldInheritFromException()
    {
        // Act & Assert
        Assert.True(typeof(RobotConnectionException).IsSubclassOf(typeof(Exception)));
        Assert.True(typeof(RobotCommandException).IsSubclassOf(typeof(Exception)));
        Assert.True(typeof(RobotNotFoundException).IsSubclassOf(typeof(Exception)));
    }

    [Fact]
    public void ExceptionTypes_ShouldBePublic()
    {
        // Act & Assert
        Assert.True(typeof(RobotConnectionException).IsPublic);
        Assert.True(typeof(RobotCommandException).IsPublic);
        Assert.True(typeof(RobotNotFoundException).IsPublic);
    }
}