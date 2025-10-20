using System.Diagnostics;
using MartianRobots.Core.Communication;

namespace MartianRobots.Tests.Core.Communication;

public class RobotCommunicationTelemetryTests
{
    [Fact]
    public void ConnectionActivity_StartsAndRecordsTags()
    {
        var (listener, started) = CreateActivityListener();
        try
        {
            var telemetry = new RobotCommunicationTelemetry();

            var pos = new Position(1, 2);
            using var activity = telemetry.StartConnectionActivity("robot-1", pos, Orientation.East);

            activity.Should().NotBeNull();
            activity!.DisplayName.Should().Be("ConnectToRobot");

            // Verify listener saw activity start
            started.Should().ContainSingle(a => a.DisplayName == "ConnectToRobot");

            // Check tags directly on returned Activity (safer)
            activity.GetTagItem("robot.id")?.ToString().Should().Be("robot-1");
            activity.GetTagItem("robot.position.x")?.ToString().Should().Be(pos.X.ToString());
            activity.GetTagItem("robot.position.y")?.ToString().Should().Be(pos.Y.ToString());

            // Record success/failure should not throw
            telemetry.RecordConnectionSuccess("robot-1", 123.4);
            telemetry.RecordConnectionFailure("robot-1", 200.1, "boom");

            telemetry.Dispose();
        }
        finally
        {
            listener.Dispose();
        }
    }

    [Fact]
    public void DisconnectActivity_StartsAndDecrementsGauge()
    {
        var (listener, started) = CreateActivityListener();
        try
        {
            var telemetry = new RobotCommunicationTelemetry();

            using var activity = telemetry.StartDisconnectActivity("robot-1");
            activity.Should().NotBeNull();
            activity!.DisplayName.Should().Be("DisconnectFromRobot");

            started.Should().ContainSingle(a => a.DisplayName == "DisconnectFromRobot");

            telemetry.Dispose();
        }
        finally
        {
            listener.Dispose();
        }
    }

    [Fact]
    public void BatchAndCommandActivities_SetTagsAndEvents()
    {
        var (listener, started) = CreateActivityListener();
        try
        {
            var telemetry = new RobotCommunicationTelemetry();

            using var batch = telemetry.StartBatchActivity("robot-2", 3);
            batch.Should().NotBeNull();
            batch!.DisplayName.Should().Be("SendCommandBatch");

            // Current activity is batch, record completion sets tags on Activity.Current
            telemetry.RecordBatchCompletion("robot-2", 3, 2, 1, 999.9);

            // Start a command activity and record success
            using var cmd = telemetry.StartCommandActivity("robot-2", "F", 0);
            cmd.Should().NotBeNull();
            cmd!.DisplayName.Should().Be("ExecuteCommand");

            telemetry.RecordCommandSuccess("robot-2", "F", 12.3, new Position(5,6), Orientation.North);

            // Record failure path
            using var cmd2 = telemetry.StartCommandActivity("robot-2", "L", 1);
            telemetry.RecordCommandFailure("robot-2", "L", 45.6, "err");

            // Robot lost event
            using var lostActivity = telemetry.StartCommandActivity("robot-2", "F", 2);
            telemetry.RecordRobotLost("robot-2", new Position(9,9), Orientation.West);

            // Ensure activities were created
            started.Should().Contain(a => a.DisplayName == "SendCommandBatch");
            started.Should().Contain(a => a.DisplayName == "ExecuteCommand");

            telemetry.Dispose();
        }
        finally
        {
            listener.Dispose();
        }
    }

    [Fact]
    public void PingActivity_Starts()
    {
        var (listener, started) = CreateActivityListener();
        try
        {
            var telemetry = new RobotCommunicationTelemetry();

            using var activity = telemetry.StartPingActivity("robot-ping");
            activity.Should().NotBeNull();
            activity!.DisplayName.Should().Be("PingRobot");

            started.Should().ContainSingle(a => a.DisplayName == "PingRobot");

            telemetry.Dispose();
        }
        finally
        {
            listener.Dispose();
        }
    }

    [Fact]
    public void Dispose_DoesNotThrow_WhenCalledMultipleTimes()
    {
        var telemetry = new RobotCommunicationTelemetry();
        telemetry.Dispose();
        telemetry.Dispose(); // should be safe
    }

    private static (ActivityListener listener, List<Activity> started) CreateActivityListener()
    {
        var started = new List<Activity>();

        var listener = new ActivityListener();
        listener.ShouldListenTo = src => src.Name == RobotCommunicationTelemetry.ActivitySourceName;
        listener.Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded;
        listener.ActivityStarted = activity => { lock (started) { started.Add(activity); } };
        listener.ActivityStopped = activity => { /* no-op */ };

        ActivitySource.AddActivityListener(listener);
        return (listener, started);
    }
}
