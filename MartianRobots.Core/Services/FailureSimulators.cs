namespace MartianRobots.Core.Services;

/// <summary>
/// Random failure simulator for production use
/// Simulates failures based on configured probability
/// </summary>
public sealed class RandomFailureSimulator : IFailureSimulator
{
    private readonly Random _random = new();
    private readonly double _failureProbability;

    public RandomFailureSimulator(double failureProbability)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(failureProbability);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(failureProbability, 1.0);
        
        _failureProbability = failureProbability;
    }

    public bool ShouldSimulateFailure()
    {
        return _random.NextDouble() < _failureProbability;
    }
}

/// <summary>
/// No-failure simulator for testing or production scenarios where failures should not occur
/// </summary>
public sealed class NoFailureSimulator : IFailureSimulator
{
    public bool ShouldSimulateFailure() => false;
}

/// <summary>
/// Always-fail simulator for testing error handling
/// </summary>
public sealed class AlwaysFailSimulator : IFailureSimulator
{
    public bool ShouldSimulateFailure() => true;
}
