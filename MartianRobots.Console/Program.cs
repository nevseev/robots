using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Hosting;
using MartianRobots.Console.Communication;

namespace MartianRobots.Console;

internal static class Program
{
    [ExcludeFromCodeCoverage]
    private static async Task<int> Main(string[] args)
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
        
        try
        {
            // Check if this is a communication demo request
            if (args.Contains("--communication-demo") || args.Contains("--comm-demo"))
            {
                logger.LogInformation("Starting Mars Robot Communication System Demo");
                
                var demo = host.Services.GetRequiredService<RobotCommunicationDemo>();
                var cts = new CancellationTokenSource();
                
                // Handle Ctrl+C gracefully
                System.Console.CancelKeyPress += (_, e) =>
                {
                    e.Cancel = true;
                    cts.Cancel();
                    logger.LogInformation("Cancellation requested by user");
                };

                await demo.RunDemoAsync(cts.Token);
                
                logger.LogInformation("Mars Robot Communication Demo completed successfully");
                return 0;
            }
            else
            {
                // Run original simulation
                logger.LogInformation("Mars Robot Simulation starting. Args: [{Args}]", string.Join(", ", args));
                
                var application = host.Services.GetRequiredService<Application>();
                var result = application.Run();
                
                logger.LogInformation("Mars Robot Simulation completed with exit code: {ExitCode}", result);
                return result;
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Operation was cancelled by user");
            return 0;
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Unhandled exception in Mars Robot application");
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
                // Original simulation services
                services.AddSingleton<Application>();
                
                // Robot communication services
                services.AddRobotCommunication();
            });
}

// Helper class for logger type parameter since Program is static
internal class StartupLogger { }