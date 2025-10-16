using MartianRobots.Core.Services;
using Microsoft.Extensions.Logging;

namespace MartianRobots.Console;

/// <summary>
/// Main application logic for the Mars Robot simulation console app
/// </summary>
public class Application
{
    private readonly TextReader _input;
    private readonly TextWriter _output;
    private readonly TextWriter _error;
    private readonly ILogger<Application> _logger;

    public Application(ILogger<Application> logger) : this(System.Console.In, System.Console.Out, System.Console.Error, logger)
    {
    }

    public Application(TextReader input, TextWriter output, TextWriter error, ILogger<Application> logger)
    {
        _input = input ?? throw new ArgumentNullException(nameof(input));
        _output = output ?? throw new ArgumentNullException(nameof(output));
        _error = error ?? throw new ArgumentNullException(nameof(error));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Runs the application pipeline
    /// </summary>
    /// <returns>Exit code (0 = success, 1 = error)</returns>
    public int Run()
    {
        _logger.LogInformation("Starting Mars Robot simulation pipeline");
        
        try
        {
            _logger.LogDebug("Reading input from stream");
            var inputLines = ReadInput();
            _logger.LogInformation("Read {LineCount} lines of input", inputLines.Count);
            
            if (inputLines.Count == 0)
            {
                const string errorMsg = "No input provided";
                _logger.LogWarning("Simulation failed: {Error}", errorMsg);
                _error.WriteLine($"Error: {errorMsg}");
                return 1;
            }

            _logger.LogDebug("Processing robots with input data");
            var results = ProcessRobots(inputLines);
            _logger.LogInformation("Successfully processed {RobotCount} robots", results.Count);
            
            _logger.LogDebug("Writing output results");
            WriteOutput(results);
            _logger.LogInformation("Simulation completed successfully");

            return 0;
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Validation error during simulation: {ErrorMessage}", ex.Message);
            _error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during simulation: {ErrorMessage}", ex.Message);
            _error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    /// <summary>
    /// Reads input lines from the input stream
    /// </summary>
    protected virtual List<string> ReadInput()
    {
        _logger.LogDebug("Starting to read input lines from stream");
        var inputLines = new List<string>();
        string? line;
        int lineNumber = 0;
        
        while ((line = _input.ReadLine()) is not null)
        {
            lineNumber++;
            inputLines.Add(line);
            _logger.LogTrace("Read line {LineNumber}: {Line}", lineNumber, line);
        }

        _logger.LogDebug("Finished reading input. Total lines: {LineCount}", inputLines.Count);
        return inputLines;
    }

    /// <summary>
    /// Processes robots using the simulation service
    /// </summary>
    protected virtual List<string> ProcessRobots(List<string> inputLines)
    {
        _logger.LogDebug("Starting robot processing with {LineCount} input lines", inputLines.Count);
        
        try
        {
            // Create a logger for the simulation service
            var serviceLogger = Microsoft.Extensions.Logging.LoggerFactory
                .Create(builder => builder.AddConsole())
                .CreateLogger<RobotSimulationService>();
            
            var results = RobotSimulationService.SimulateRobotsWithLogger([.. inputLines], serviceLogger);
            _logger.LogDebug("Robot processing completed successfully with {ResultCount} results", results.Count);
            
            for (int i = 0; i < results.Count; i++)
            {
                _logger.LogTrace("Robot {RobotIndex} result: {Result}", i + 1, results[i]);
            }
            
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during robot processing");
            throw;
        }
    }

    /// <summary>
    /// Writes output results to the output stream
    /// </summary>
    protected virtual void WriteOutput(List<string> results)
    {
        _logger.LogDebug("Starting to write {ResultCount} output results", results.Count);
        
        for (int i = 0; i < results.Count; i++)
        {
            _output.WriteLine(results[i]);
            _logger.LogTrace("Wrote result {ResultIndex}: {Result}", i + 1, results[i]);
        }
        
        _logger.LogDebug("Finished writing all output results");
    }
}