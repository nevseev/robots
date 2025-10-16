using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Hosting;

namespace MartianRobots.Console;

internal static class Program
{
    [ExcludeFromCodeCoverage]
    private static int Main(string[] args)
    {
        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File($"logs/martian-robots-{DateTime.Now:yyyy-MM-dd}.log",
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        var host = CreateHostBuilder(args).Build();
        
        var logger = host.Services.GetRequiredService<ILogger<StartupLogger>>();
        logger.LogInformation("Mars Robot Simulation starting. Args: [{Args}]", string.Join(", ", args));

        try
        {
            var application = host.Services.GetRequiredService<Application>();
            var result = application.Run();
            
            logger.LogInformation("Mars Robot Simulation completed with exit code: {ExitCode}", result);
            return result;
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Unhandled exception in Mars Robot Simulation");
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
            host.Dispose();
        }
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .UseSerilog()
            .ConfigureServices((context, services) =>
            {
                services.AddSingleton<Application>();
            });
}

// Helper class for logger type parameter since Program is static
internal class StartupLogger { }