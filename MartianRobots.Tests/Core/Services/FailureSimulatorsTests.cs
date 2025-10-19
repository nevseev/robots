using MartianRobots.Core.Services;

namespace MartianRobots.Tests.Core.Services;

/// <summary>
/// Unit tests for failure simulator implementations
/// </summary>
public class FailureSimulatorsTests
{
    #region NoFailureSimulator Tests

    [Fact]
    public void NoFailureSimulator_ShouldSimulateFailure_ShouldAlwaysReturnFalse()
    {
        // Arrange
        var simulator = new NoFailureSimulator();

        // Act & Assert - call multiple times to ensure deterministic behavior
        for (int i = 0; i < 100; i++)
        {
            simulator.ShouldSimulateFailure().Should().BeFalse();
        }
    }

    #endregion

    #region AlwaysFailSimulator Tests

    [Fact]
    public void AlwaysFailSimulator_ShouldSimulateFailure_ShouldAlwaysReturnTrue()
    {
        // Arrange
        var simulator = new AlwaysFailSimulator();

        // Act & Assert - call multiple times to ensure deterministic behavior
        for (int i = 0; i < 100; i++)
        {
            simulator.ShouldSimulateFailure().Should().BeTrue();
        }
    }

    #endregion

    #region RandomFailureSimulator Tests

    [Fact]
    public void RandomFailureSimulator_WithZeroProbability_ShouldNeverFail()
    {
        // Arrange
        var simulator = new RandomFailureSimulator(0.0);

        // Act & Assert - call multiple times, should never fail
        for (int i = 0; i < 100; i++)
        {
            simulator.ShouldSimulateFailure().Should().BeFalse();
        }
    }

    [Fact]
    public void RandomFailureSimulator_WithMaxProbability_ShouldAlwaysFail()
    {
        // Arrange
        var simulator = new RandomFailureSimulator(1.0);

        // Act & Assert - call multiple times, should always fail
        for (int i = 0; i < 100; i++)
        {
            simulator.ShouldSimulateFailure().Should().BeTrue();
        }
    }

    [Fact]
    public void RandomFailureSimulator_WithMidProbability_ShouldFailSometimes()
    {
        // Arrange
        var simulator = new RandomFailureSimulator(0.5);
        var failureCount = 0;

        // Act - call many times to get statistical distribution
        for (int i = 0; i < 1000; i++)
        {
            if (simulator.ShouldSimulateFailure())
            {
                failureCount++;
            }
        }

        // Assert - with 0.5 probability, expect roughly 400-600 failures out of 1000
        // Using a generous range to avoid flaky tests
        failureCount.Should().BeInRange(300, 700);
    }

    [Fact]
    public void RandomFailureSimulator_WithNegativeProbability_ShouldThrowArgumentOutOfRangeException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => new RandomFailureSimulator(-0.1));

        exception.Should().NotBeNull();
    }

    [Fact]
    public void RandomFailureSimulator_WithProbabilityGreaterThanOne_ShouldThrowArgumentOutOfRangeException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => new RandomFailureSimulator(1.1));

        exception.Should().NotBeNull();
    }

    [Fact]
    public void RandomFailureSimulator_WithValidProbability_ShouldConstruct()
    {
        // Act
        var simulator1 = new RandomFailureSimulator(0.0);
        var simulator2 = new RandomFailureSimulator(0.5);
        var simulator3 = new RandomFailureSimulator(1.0);

        // Assert - construction should succeed
        simulator1.Should().NotBeNull();
        simulator2.Should().NotBeNull();
        simulator3.Should().NotBeNull();
    }

    #endregion
}
