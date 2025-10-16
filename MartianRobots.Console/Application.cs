using MartianRobots.Core.Services;

namespace MartianRobots.Console;

/// <summary>
/// Main application logic for the Mars Robot simulation console app
/// </summary>
public class Application(TextReader input, TextWriter output, TextWriter error)
{
    private readonly TextReader _input = input ?? throw new ArgumentNullException(nameof(input));
    private readonly TextWriter _output = output ?? throw new ArgumentNullException(nameof(output));
    private readonly TextWriter _error = error ?? throw new ArgumentNullException(nameof(error));

    /// <summary>
    /// Default constructor using system console streams
    /// </summary>
    public Application() : this(System.Console.In, System.Console.Out, System.Console.Error)
    {
    }

    /// <summary>
    /// Runs the application pipeline
    /// </summary>
    /// <returns>Exit code (0 = success, 1 = error)</returns>
    public int Run()
    {
        try
        {
            var inputLines = ReadInput();
            
            if (inputLines.Count == 0)
            {
                _error.WriteLine("Error: No input provided");
                return 1;
            }

            var results = ProcessRobots(inputLines);
            WriteOutput(results);

            return 0;
        }
        catch (ArgumentException ex)
        {
            _error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
        catch (Exception ex)
        {
            _error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    /// <summary>
    /// Reads input lines from the input stream
    /// </summary>
    protected virtual List<string> ReadInput()
    {
        var inputLines = new List<string>();
        string? line;
        
        while ((line = _input.ReadLine()) is not null)
        {
            inputLines.Add(line);
        }

        return inputLines;
    }

    /// <summary>
    /// Processes robots using the simulation service
    /// </summary>
    protected virtual List<string> ProcessRobots(List<string> inputLines)
    {
        return RobotSimulationService.SimulateRobots([.. inputLines]);
    }

    /// <summary>
    /// Writes output results to the output stream
    /// </summary>
    protected virtual void WriteOutput(List<string> results)
    {
        foreach (var result in results)
        {
            _output.WriteLine(result);
        }
    }
}