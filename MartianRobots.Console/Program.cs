using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using MartianRobots.Abstractions.Models;
using MartianRobots.Abstractions.Services;
using MartianRobots.Core.Communication;
using MartianRobots.Core.Resilience;
using MartianRobots.Core.Services;

namespace MartianRobots.Console;

/// <summary>
/// Application entry point.
/// Intentionally excluded from code coverage - see README.md "Testing Strategy" section.
/// DI configuration is tested via integration tests; bootstrapping code provides minimal value to test directly.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class Program
{
    /// <summary>
    /// Main entry point - configures services and runs the robot simulation.
    /// </summary>
    private static async Task<int> Main(string[] args)
    {
        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .WriteTo.File($"logs/martian-robots-{DateTime.Now:yyyy-MM-dd}.log",
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        try
        {
            // Setup dependency injection
            var services = new ServiceCollection();
            ConfigureServices(services);
            var serviceProvider = services.BuildServiceProvider();

            // Determine input source
            string? inputFile = args.Length > 0 ? args[0] : null;

            // Create and run application
            var logger = serviceProvider.GetRequiredService<ILogger<Application>>();
            var app = new Application(serviceProvider, logger);
            return await app.RunAsync(inputFile);
        }
        catch (Exception ex)
        {
            Log.Logger.ForContext<StartupLogger>().Fatal(ex, "Fatal error during application startup");
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    /// <summary>
    /// Configures dependency injection for production use.
    /// Note: Tests use different configurations (fast delays, no randomness).
    /// See README.md "Configuration" section for test vs production differences.
    /// </summary>
    private static void ConfigureServices(IServiceCollection services)
    {
        // Add logging
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog(dispose: true);
        });

        // Add robot communication configuration
        services.AddSingleton(new RobotCommunicationOptions
        {
            BaseDelay = TimeSpan.FromMilliseconds(500),
            MaxRandomDelay = TimeSpan.FromSeconds(1),
            FailureProbability = 0.1, // 10% failure rate for demo
            CommandTimeout = TimeSpan.FromSeconds(10),
            MaxRetryAttempts = 3,
            CircuitBreakerThreshold = 5,
            CircuitBreakerSamplingDuration = TimeSpan.FromSeconds(30),
            CircuitBreakerMinimumThroughput = 10
        });

        services.AddSingleton(sp =>
            Options.Create(sp.GetRequiredService<RobotCommunicationOptions>()));

        // Add core services
        services.AddSingleton<IDelayService, DelayService>();
        services.AddSingleton<IFailureSimulator>(sp =>
        {
            var options = sp.GetRequiredService<RobotCommunicationOptions>();
            return new RandomFailureSimulator(options.FailureProbability);
        });
        services.AddSingleton<IRobotCommunicationService, RobotCommunicationService>();
        services.AddSingleton<IResiliencePipelineProvider, ResiliencePipelineProvider>();
        services.AddSingleton<IResilientRobotController, ResilientRobotController>();

        // Add demo runner
        services.AddSingleton<IRobotDemo, RobotDemo>();
    }
}

// Helper class for logger type parameter since Program is static
internal class StartupLogger { }