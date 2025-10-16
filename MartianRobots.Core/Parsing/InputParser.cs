namespace MartianRobots.Core.Parsing;

/// <summary>
/// Parser for input data following Single Responsibility and DRY principles
/// </summary>
public static class InputParser
{
    /// <summary>
    /// Parses grid dimensions from input line
    /// </summary>
    /// <param name="gridLine">Line containing grid dimensions</param>
    /// <returns>MarsGrid instance</returns>
    public static MarsGrid ParseGrid(string gridLine)
    {
        Validation.InputValidator.ValidateGridLine(gridLine);
        
        var gridParts = gridLine.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var maxX = int.Parse(gridParts[0]);
        var maxY = int.Parse(gridParts[1]);

        return new MarsGrid(maxX, maxY);
    }

    /// <summary>
    /// Parses robot from position line
    /// </summary>
    /// <param name="positionLine">Line containing robot position and orientation</param>
    /// <returns>Robot instance</returns>
    public static Robot ParseRobot(string positionLine)
    {
        Validation.InputValidator.ValidateRobotPosition(positionLine);
        
        var parts = positionLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var x = int.Parse(parts[0]);
        var y = int.Parse(parts[1]);
        var orientationChar = parts[2][0];

        var position = new Position(x, y);
        var orientation = OrientationExtensions.FromChar(orientationChar);

        return new Robot(position, orientation);
    }
}