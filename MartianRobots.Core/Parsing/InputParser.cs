using Microsoft.Extensions.Logging;

namespace MartianRobots.Core.Parsing;

/// <summary>
/// Parser for input data following Single Responsibility and DRY principles
/// </summary>
public static class InputParser
{
    /// <summary>
    /// Parses grid dimensions from input line with optional logging
    /// </summary>
    /// <param name="gridLine">Line containing grid dimensions</param>
    /// <param name="logger">Optional logger for diagnostics</param>
    /// <returns>MarsGrid instance</returns>
    public static MarsGrid ParseGrid(string gridLine, ILogger? logger)
    {
        logger?.LogDebug("Parsing grid from line: {GridLine}", gridLine);
        
        Validation.InputValidator.ValidateGridLine(gridLine, logger);
        
        var gridParts = gridLine.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var maxX = int.Parse(gridParts[0]);
        var maxY = int.Parse(gridParts[1]);

        var grid = new MarsGrid(maxX, maxY);
        logger?.LogInformation("Successfully parsed grid with dimensions {MaxX}x{MaxY}", maxX, maxY);
        
        return grid;
    }

    /// <summary>
    /// Parses robot from position line with optional logging
    /// </summary>
    /// <param name="positionLine">Line containing robot position and orientation</param>
    /// <param name="logger">Optional logger for diagnostics</param>
    /// <returns>Robot instance</returns>
    public static Robot ParseRobot(string positionLine, ILogger? logger)
    {
        logger?.LogDebug("Parsing robot from position line: {PositionLine}", positionLine);
        
        Validation.InputValidator.ValidateRobotPosition(positionLine, logger);
        
        var parts = positionLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var x = int.Parse(parts[0]);
        var y = int.Parse(parts[1]);
        var orientationChar = parts[2][0];

        var position = new Position(x, y);
        var orientation = OrientationExtensions.FromChar(orientationChar);

        var robot = new Robot(position, orientation);
        logger?.LogInformation("Successfully parsed robot at position {Position} facing {Orientation}", 
            position, orientation);
        
        return robot;
    }
}