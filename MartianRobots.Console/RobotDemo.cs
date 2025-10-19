using Microsoft.Extensions.Logging;
using MartianRobots.Abstractions.Models;
using MartianRobots.Core.Communication;
using MartianRobots.Core.Parsing;

namespace MartianRobots.Console;

/// <summary>
/// Application that runs robot simulations using the Mars Robot Communication System
/// </summary>
internal sealed class RobotDemo(IResilientRobotController controller, ILogger<RobotDemo> logger)
{
    public async Task RunAsync(string? inputFile = null)
    {
        logger.LogInformation("");
        logger.LogInformation("Mars Robot Communication System");
        logger.LogInformation("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        logger.LogInformation("");

        try
        {
            // Read input from file or stdin
            var lines = await ReadInputAsync(inputFile);
            
            if (lines.Count == 0)
            {
                logger.LogError("No input provided");
                return;
            }

            // Parse grid (first line)
            var grid = InputParser.ParseGrid(lines[0], logger);
            logger.LogInformation("");

            // Process robots (pairs of lines: position + instructions)
            int robotNumber = 1;
            for (int i = 1; i < lines.Count; i += 2)
            {
                if (i + 1 >= lines.Count)
                {
                    logger.LogWarning("Incomplete robot data at line {LineNumber}", i + 1);
                    break;
                }

                var positionLine = lines[i];
                var instructionsLine = lines[i + 1];

                var robot = InputParser.ParseRobot(positionLine, logger);
                var robotId = $"MARS-ROVER-{robotNumber}";

                await RunRobotScenario(
                    robotId,
                    robot.Position,
                    robot.Orientation,
                    instructionsLine.Trim(),
                    grid);

                robotNumber++;
                logger.LogInformation("");
            }

            logger.LogInformation("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            logger.LogInformation("Simulation completed");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Simulation failed");
            throw;
        }
        finally
        {
            controller.Dispose();
        }
    }

    private async Task<List<string>> ReadInputAsync(string? inputFile)
    {
        var lines = new List<string>();

        if (!string.IsNullOrEmpty(inputFile))
        {
            // Read from file
            if (!File.Exists(inputFile))
            {
                logger.LogError("Input file not found: {FilePath}", inputFile);
                return lines;
            }

            logger.LogInformation("Reading input from file: {FilePath}", inputFile);
            var allLines = await File.ReadAllLinesAsync(inputFile);
            lines.AddRange(allLines.Where(line => !string.IsNullOrWhiteSpace(line)));
        }
        else
        {
            // Read from stdin
            logger.LogInformation("Reading input from stdin (Ctrl+D or Ctrl+Z to finish)...");
            string? line;
            while ((line = System.Console.ReadLine()) != null)
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    lines.Add(line);
                }
            }
        }

        return lines;
    }

    private async Task RunRobotScenario(
        string robotId,
        Position initialPosition,
        Orientation initialOrientation,
        string instructions,
        MarsGrid grid)
    {
        logger.LogInformation("  Robot: {RobotId}", robotId);
        logger.LogInformation("   Starting at: ({X}, {Y}) facing {Orientation}",
            initialPosition.X, initialPosition.Y, initialOrientation);
        logger.LogInformation("   Instructions: {Instructions}", instructions);

        try
        {
            // Connect to robot
            logger.LogDebug("   Connecting to robot...");
            var connected = await controller.ConnectRobotAsync(
                robotId,
                initialPosition,
                initialOrientation);

            if (!connected)
            {
                logger.LogError("     Failed to connect to robot {RobotId}", robotId);
                return;
            }

            logger.LogDebug("     Connected successfully");

            // Execute instruction sequence
            logger.LogDebug("   Executing {Count} commands...", instructions.Length);
            var responses = await controller.ExecuteInstructionSequenceAsync(
                robotId,
                instructions,
                grid);

            // Display results
            var lastSuccessfulResponse = responses.LastOrDefault(r => r.Status == CommandStatus.Executed);
            
            if (lastSuccessfulResponse != null)
            {
                if (lastSuccessfulResponse.IsLost)
                {
                    logger.LogWarning("     Final Position: {X} {Y} {Orientation} LOST",
                        lastSuccessfulResponse.NewPosition?.X,
                        lastSuccessfulResponse.NewPosition?.Y,
                        lastSuccessfulResponse.NewOrientation);
                }
                else
                {
                    logger.LogInformation("     Final Position: {X} {Y} {Orientation}",
                        lastSuccessfulResponse.NewPosition?.X,
                        lastSuccessfulResponse.NewPosition?.Y,
                        lastSuccessfulResponse.NewOrientation);
                }

                var executedCount = responses.Count(r => r.Status == CommandStatus.Executed);
                var failedCount = responses.Count(r => r.Status == CommandStatus.Failed);
                
                if (failedCount > 0)
                {
                    logger.LogDebug("   Commands: {ExecutedCount} succeeded, {FailedCount} failed out of {TotalCount}",
                        executedCount, failedCount, instructions.Length);
                }
                else
                {
                    logger.LogDebug("   Commands executed: {ExecutedCount}/{TotalCount}",
                        executedCount, instructions.Length);
                }
            }
            else
            {
                logger.LogError("     No commands were executed successfully");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "     Error executing scenario for robot {RobotId}", robotId);
        }
    }
}
