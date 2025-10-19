using MartianRobots.Core.Communication;

namespace MartianRobots.Tests.Core.Communication;

/// <summary>
/// Tests for custom exception classes in ResilientRobotController
/// </summary>
public class RobotExceptionTests
{
    [Fact]
    public void RobotConnectionException_WithMessage_ShouldSetMessage()
    {
        // Arrange
        var message = "Failed to connect to robot MARS-1";

        // Act
        var exception = new RobotConnectionException(message);

        // Assert
        exception.Message.Should().Be(message);
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void RobotConnectionException_WithMessageAndInnerException_ShouldSetBoth()
    {
        // Arrange
        var message = "Failed to connect to robot MARS-2";
        var innerException = new InvalidOperationException("Network error");

        // Act
        var exception = new RobotConnectionException(message, innerException);

        // Assert
        exception.Message.Should().Be(message);
        exception.InnerException.Should().BeSameAs(innerException);
    }

    [Fact]
    public void RobotConnectionException_ShouldBeThrowable()
    {
        // Arrange
        var message = "Connection timeout";

        // Act
        Action act = () => throw new RobotConnectionException(message);

        // Assert
        act.Should().Throw<RobotConnectionException>()
            .WithMessage(message);
    }

    [Fact]
    public void RobotConnectionException_ShouldBeCatchableAsException()
    {
        // Arrange
        var message = "Robot unreachable";
        Exception? caughtException = null;

        // Act
        try
        {
            throw new RobotConnectionException(message);
        }
        catch (Exception ex)
        {
            caughtException = ex;
        }

        // Assert
        caughtException.Should().NotBeNull();
        caughtException.Should().BeOfType<RobotConnectionException>();
        caughtException!.Message.Should().Be(message);
    }
}
