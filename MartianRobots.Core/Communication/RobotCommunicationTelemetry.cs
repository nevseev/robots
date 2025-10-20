using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace MartianRobots.Core.Communication;

/// <summary>
/// OpenTelemetry instrumentation for robot communication service
/// </summary>
public sealed class RobotCommunicationTelemetry : IDisposable
{
    public const string ActivitySourceName = "MartianRobots.Communication";
    public const string MeterName = "MartianRobots.Communication";
    
    private readonly ActivitySource _activitySource;
    private readonly Meter _meter;
    
    // Counters
    private readonly Counter<long> _connectionAttemptsCounter;
    private readonly Counter<long> _connectionSuccessCounter;
    private readonly Counter<long> _connectionFailureCounter;
    private readonly Counter<long> _commandsExecutedCounter;
    private readonly Counter<long> _commandsFailedCounter;
    private readonly Counter<long> _robotsLostCounter;
    
    // Histograms
    private readonly Histogram<double> _connectionDurationHistogram;
    private readonly Histogram<double> _commandDurationHistogram;
    private readonly Histogram<double> _batchDurationHistogram;
    
    // Gauges (using ObservableGauge)
    private readonly ObservableGauge<int> _activeRobotsGauge;
    private int _activeRobots;

    public RobotCommunicationTelemetry()
    {
        _activitySource = new ActivitySource(ActivitySourceName, "1.0.0");
        _meter = new Meter(MeterName, "1.0.0");
        
        // Initialize counters
        _connectionAttemptsCounter = _meter.CreateCounter<long>(
            "robot.connection.attempts",
            description: "Total number of connection attempts to robots");
        
        _connectionSuccessCounter = _meter.CreateCounter<long>(
            "robot.connection.success",
            description: "Total number of successful robot connections");
        
        _connectionFailureCounter = _meter.CreateCounter<long>(
            "robot.connection.failures",
            description: "Total number of failed robot connections");
        
        _commandsExecutedCounter = _meter.CreateCounter<long>(
            "robot.commands.executed",
            description: "Total number of commands successfully executed");
        
        _commandsFailedCounter = _meter.CreateCounter<long>(
            "robot.commands.failed",
            description: "Total number of commands that failed");
        
        _robotsLostCounter = _meter.CreateCounter<long>(
            "robot.lost",
            description: "Total number of robots lost off the grid");
        
        // Initialize histograms
        _connectionDurationHistogram = _meter.CreateHistogram<double>(
            "robot.connection.duration",
            unit: "ms",
            description: "Duration of robot connection operations");
        
        _commandDurationHistogram = _meter.CreateHistogram<double>(
            "robot.command.duration",
            unit: "ms",
            description: "Duration of individual command execution");
        
        _batchDurationHistogram = _meter.CreateHistogram<double>(
            "robot.batch.duration",
            unit: "ms",
            description: "Duration of command batch execution");
        
        // Initialize gauge
        _activeRobotsGauge = _meter.CreateObservableGauge(
            "robot.active.count",
            () => _activeRobots,
            description: "Number of currently active robot connections");
    }

    public Activity? StartConnectionActivity(string robotId, Position position, Orientation orientation)
    {
        var activity = _activitySource.StartActivity("ConnectToRobot", ActivityKind.Client);
        activity?.SetTag("robot.id", robotId);
        activity?.SetTag("robot.position.x", position.X);
        activity?.SetTag("robot.position.y", position.Y);
        activity?.SetTag("robot.orientation", orientation.ToString());
        
        _connectionAttemptsCounter.Add(1, new KeyValuePair<string, object?>("robot.id", robotId));
        
        return activity;
    }

    public void RecordConnectionSuccess(string robotId, double durationMs)
    {
        _connectionSuccessCounter.Add(1, new KeyValuePair<string, object?>("robot.id", robotId));
        _connectionDurationHistogram.Record(durationMs, 
            new KeyValuePair<string, object?>("robot.id", robotId),
            new KeyValuePair<string, object?>("status", "success"));
        
        Interlocked.Increment(ref _activeRobots);
    }

    public void RecordConnectionFailure(string robotId, double durationMs, string? errorMessage = null)
    {
        _connectionFailureCounter.Add(1, 
            new KeyValuePair<string, object?>("robot.id", robotId),
            new KeyValuePair<string, object?>("error", errorMessage ?? "unknown"));
        
        _connectionDurationHistogram.Record(durationMs,
            new KeyValuePair<string, object?>("robot.id", robotId),
            new KeyValuePair<string, object?>("status", "failure"));
    }

    public Activity? StartDisconnectActivity(string robotId)
    {
        var activity = _activitySource.StartActivity("DisconnectFromRobot", ActivityKind.Client);
        activity?.SetTag("robot.id", robotId);
        
        Interlocked.Decrement(ref _activeRobots);
        
        return activity;
    }

    public Activity? StartBatchActivity(string robotId, int commandCount)
    {
        var activity = _activitySource.StartActivity("SendCommandBatch", ActivityKind.Client);
        activity?.SetTag("robot.id", robotId);
        activity?.SetTag("command.count", commandCount);
        
        return activity;
    }

    public void RecordBatchCompletion(string robotId, int totalCommands, int successCount, int failureCount, double durationMs)
    {
        _batchDurationHistogram.Record(durationMs,
            new KeyValuePair<string, object?>("robot.id", robotId),
            new KeyValuePair<string, object?>("total.commands", totalCommands));
        
        Activity.Current?.SetTag("commands.success", successCount);
        Activity.Current?.SetTag("commands.failed", failureCount);
    }

    public Activity? StartCommandActivity(string robotId, string command, int commandIndex)
    {
        var activity = _activitySource.StartActivity("ExecuteCommand", ActivityKind.Internal);
        activity?.SetTag("robot.id", robotId);
        activity?.SetTag("command.type", command);
        activity?.SetTag("command.index", commandIndex);
        
        return activity;
    }

    public void RecordCommandSuccess(string robotId, string command, double durationMs, Position newPosition, Orientation newOrientation)
    {
        _commandsExecutedCounter.Add(1,
            new KeyValuePair<string, object?>("robot.id", robotId),
            new KeyValuePair<string, object?>("command.type", command));
        
        _commandDurationHistogram.Record(durationMs,
            new KeyValuePair<string, object?>("robot.id", robotId),
            new KeyValuePair<string, object?>("command.type", command),
            new KeyValuePair<string, object?>("status", "success"));
        
        Activity.Current?.SetTag("robot.position.x", newPosition.X);
        Activity.Current?.SetTag("robot.position.y", newPosition.Y);
        Activity.Current?.SetTag("robot.orientation", newOrientation.ToString());
    }

    public void RecordCommandFailure(string robotId, string command, double durationMs, string errorMessage)
    {
        _commandsFailedCounter.Add(1,
            new KeyValuePair<string, object?>("robot.id", robotId),
            new KeyValuePair<string, object?>("command.type", command),
            new KeyValuePair<string, object?>("error", errorMessage));
        
        _commandDurationHistogram.Record(durationMs,
            new KeyValuePair<string, object?>("robot.id", robotId),
            new KeyValuePair<string, object?>("command.type", command),
            new KeyValuePair<string, object?>("status", "failure"));
        
        Activity.Current?.SetTag("error.message", errorMessage);
        Activity.Current?.SetStatus(ActivityStatusCode.Error, errorMessage);
    }

    public void RecordRobotLost(string robotId, Position lastPosition, Orientation lastOrientation)
    {
        _robotsLostCounter.Add(1,
            new KeyValuePair<string, object?>("robot.id", robotId));
        
        Activity.Current?.SetTag("robot.lost", true);
        Activity.Current?.SetTag("robot.last.position.x", lastPosition.X);
        Activity.Current?.SetTag("robot.last.position.y", lastPosition.Y);
        Activity.Current?.SetTag("robot.last.orientation", lastOrientation.ToString());
        Activity.Current?.AddEvent(new ActivityEvent("RobotLost", 
            tags: new ActivityTagsCollection
            {
                { "position.x", lastPosition.X },
                { "position.y", lastPosition.Y },
                { "orientation", lastOrientation.ToString() }
            }));
    }

    public Activity? StartPingActivity(string robotId)
    {
        var activity = _activitySource.StartActivity("PingRobot", ActivityKind.Client);
        activity?.SetTag("robot.id", robotId);
        
        return activity;
    }

    public void Dispose()
    {
        _activitySource.Dispose();
        _meter.Dispose();
    }
}
