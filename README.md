# Martian Robots - Mars Surface Navigation System

A sophisticated .NET application for simulating Mars rover navigation with realistic communication patterns, resilience strategies, and comprehensive test coverage.

[![.NET Version](https://img.shields.io/badge/.NET-9.0-purple)](https://dotnet.microsoft.com/)
[![Test Coverage](https://img.shields.io/badge/Coverage-100%25%20lines%2C%2099.5%25%20branches-brightgreen)](https://github.com/nevseev/robots)
[![Tests](https://img.shields.io/badge/Tests-361%20passing-success)](https://github.com/nevseev/robots)

## 📋 Table of Contents

- [Overview](#overview)
- [Quick Start](#quick-start)
- [OpenTelemetry & Observability](#opentelemetry--observability)
- [Architecture](#architecture)
- [Design Patterns & Decisions](#design-patterns--decisions)
- [Testing Strategy](#testing-strategy)
- [Project Structure](#project-structure)
- [Configuration](#configuration)
- [Development](#development)
- [Performance](#performance)

---

## 🎯 Overview

This project simulates Mars rover operations with a focus on **realistic communication challenges** and **resilient system design**. It demonstrates advanced software engineering practices including:

- ✅ **100% line coverage, 99.5% branch coverage** (361 comprehensive tests)
- ⚡ **Sub-second test execution** (361 tests in ~0.8s)
- 🔄 **Resilience patterns** with Microsoft.Extensions.Resilience (retry, circuit breaker, timeout)
- 📊 **OpenTelemetry instrumentation** with distributed tracing and metrics
- 🧪 **Deterministic testing** with zero conditional logic in tests
- 🏗️ **Clean Architecture** with clear separation of concerns
- 📦 **Dependency Injection** throughout
- 🎯 **Interface-based design** for testability

### Problem Domain

Robots navigate a rectangular Mars grid, receiving commands to move and turn. The system must:
- Track robot positions and orientations (N, S, E, W)
- Prevent robots from falling off the grid
- Mark positions as "scented" when robots are lost (warn future robots)
- Simulate realistic Mars communication (delays, failures, retry logic)

---

## 🚀 Quick Start

### Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download)
- macOS, Linux, or Windows

### Run the Application

```bash
# With a sample file
dotnet run --project MartianRobots.Console sample-simulation.txt

# With stdin (pipe)
cat sample-simulation.txt | dotnet run --project MartianRobots.Console

# Interactive input
dotnet run --project MartianRobots.Console
# Type input, then Ctrl+D (Unix/Mac) or Ctrl+Z (Windows)
```

### Input Format

```
5 3          # Grid dimensions (5x3)
1 1 E        # Robot start: position (1,1) facing East
RFRFRFRF     # Commands: R=Right, L=Left, F=Forward

3 2 N        # Second robot
FRRFLLFFRRFLL
```

### Run Tests

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --settings coverlet.runsettings --collect:"XPlat Code Coverage"

# Run specific test category
dotnet test --filter "FullyQualifiedName~Integration"
```

### Build

```bash
# Build entire solution
dotnet build

# Build specific project
dotnet build MartianRobots.Core
```

---

## 📊 OpenTelemetry & Observability

The robot communication service is **fully instrumented with OpenTelemetry** for production-grade observability with distributed tracing and metrics.

### What's Instrumented

#### Distributed Traces (Spans)

Every operation creates detailed trace spans:

1. **ConnectToRobot** - Connection establishment
   - Tags: `robot.id`, `robot.position.x`, `robot.position.y`, `robot.orientation`
   - Records connection success/failure with retry tracking

2. **DisconnectFromRobot** - Robot disconnection
   - Tags: `robot.id`

3. **SendCommandBatch** - Batch command execution
   - Tags: `robot.id`, `command.count`, `commands.success`, `commands.failed`
   - Parent span with child spans for each command

4. **ExecuteCommand** - Individual command execution
   - Tags: `robot.id`, `command.type`, `command.index`, position and orientation
   - Records when robot is lost with special events

5. **PingRobot** - Health check operations
   - Tags: `robot.id`

#### Metrics

**Counters:**
- `robot.connection.attempts` - Total connection attempts
- `robot.connection.success` - Successful connections
- `robot.connection.failures` - Failed connections (with error labels)
- `robot.commands.executed` - Successfully executed commands
- `robot.commands.failed` - Failed commands (with error labels)
- `robot.lost` - Number of robots lost off the grid

**Histograms (for percentile analysis):**
- `robot.connection.duration` (ms) - Connection operation duration
- `robot.command.duration` (ms) - Individual command execution time
- `robot.batch.duration` (ms) - Batch operation duration

**Gauges:**
- `robot.active.count` - Number of currently connected robots (real-time)

All metrics include dimensions (robot.id, command.type, status, error) for filtering and aggregation.

### Running with Telemetry

#### Option 1: Console Exporter (Default)

Telemetry is exported to console by default:

```bash
dotnet run --project MartianRobots.Console sample-simulation.txt
```

Trace and metric data appears in console alongside application logs.

#### Option 2: Jaeger (Recommended)

Use the included Docker Compose setup for full observability stack:

```bash
# 1. Start Jaeger and observability stack
docker-compose up -d

# 2. Run with OTLP exporter
export OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317
dotnet run --project MartianRobots.Console sample-simulation.txt

# Or use the convenience script:
./run-with-telemetry.sh
```

**Access points:**
- **Jaeger UI**: http://localhost:16686 (traces)
- **Prometheus**: http://localhost:9090 (metrics - optional)
- **Grafana**: http://localhost:3000 (dashboards - optional, admin/admin)

**View traces in Jaeger:**
1. Open http://localhost:16686
2. Select "MartianRobots" service
3. Click "Find Traces"
4. Explore distributed traces with full operation hierarchy!

#### Option 3: Custom OTLP Backend

Export to any OpenTelemetry-compatible backend (Honeycomb, Lightstep, New Relic, etc.):

```bash
export OTEL_EXPORTER_OTLP_ENDPOINT=https://your-endpoint:4317
export OTEL_EXPORTER_OTLP_HEADERS="x-api-key=your-api-key"
dotnet run --project MartianRobots.Console sample-simulation.txt
```

### Example Trace View

Traces show the complete flow with timing:

```
SendCommandBatch (MARS-ROVER-1, 8 commands) [8.2s]
├── ExecuteCommand (R, index=0) [502ms]
│   └── Tags: robot.position.x=1, robot.position.y=1, robot.orientation=South
├── ExecuteCommand (F, index=1) [518ms]
│   └── Tags: robot.position.x=1, robot.position.y=0, robot.orientation=South
├── ExecuteCommand (R, index=2) [495ms]
│   └── Tags: robot.position.x=1, robot.position.y=0, robot.orientation=West
└── ... [5 more commands]
```

### Architecture

```
RobotCommunicationService
    └── Uses: RobotCommunicationTelemetry
            ├── ActivitySource (for traces)
            └── Meter (for metrics)

Program.cs
    └── Configures: OpenTelemetry SDK
            ├── TracerProvider (ConsoleExporter + OtlpExporter)
            └── MeterProvider (ConsoleExporter + OtlpExporter)
```

### Performance Impact

- **Overhead**: ~5-10% with full tracing (AlwaysOnSampler)
- **Batching**: Async export doesn't block operations
- **Testing**: Telemetry is optional (nullable) - tests run at full speed
- **Production**: Consider probabilistic sampling (10%) for high-volume scenarios

### Cleanup

```bash
# Stop observability stack
docker-compose down

# Remove volumes too
docker-compose down -v
```

---

## 🏗️ Architecture

### High-Level Design

```
┌─────────────────────────────────────────────────────────────┐
│                    MartianRobots.Console                     │
│                  (Entry Point & DI Setup)                    │
└───────────────────────────┬─────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                     MartianRobots.Core                       │
│  ┌─────────────────┐  ┌──────────────────────────────────┐ │
│  │ RobotDemo       │  │ Communication Layer              │ │
│  │ (Orchestration) │─→│ - RobotCommunicationService      │ │
│  └─────────────────┘  │ - ResilientRobotController       │ │
│                       │ - ResiliencePipelineProvider     │ │
│  ┌─────────────────┐  └──────────────────────────────────┘ │
│  │ Business Logic  │                                        │
│  │ - Commands      │  ┌──────────────────────────────────┐ │
│  │ - Strategies    │  │ Supporting Services              │ │
│  │ - Validation    │  │ - DelayService                   │ │
│  │ - Parsing       │  │ - FailureSimulators              │ │
│  └─────────────────┘  └──────────────────────────────────┘ │
└───────────────────────────┬─────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                MartianRobots.Abstractions                    │
│          (Interfaces, Models, Core Contracts)                │
│  - Position, Orientation, MarsGrid                          │
│  - IRobotCommand, IMovementStrategy                         │
│  - IDelayService, IFailureSimulator                         │
└─────────────────────────────────────────────────────────────┘
```

### Layer Responsibilities

#### **MartianRobots.Abstractions**
- **Pure interfaces and models** - no implementation
- **No external dependencies**
- **Shared contracts** across all layers
- Examples: `Position`, `Orientation`, `MarsGrid`, `IRobotCommand`

#### **MartianRobots.Core**
- **All business logic and implementations**
- **Communication simulation** (delays, failures, retries)
- **Resilience patterns** (Microsoft.Extensions.Resilience)
- **Input parsing and validation**
- **Command pattern implementation**

#### **MartianRobots.Console**
- **Application entry point** only
- **Dependency injection setup**
- **Configuration** (logging, services)
- **Minimal logic** (marked `[ExcludeFromCodeCoverage]`)

#### **MartianRobots.Tests**
- **361 comprehensive tests** (including OpenTelemetry instrumentation tests)
- **100% coverage** on Core and Telemetry
- **71.97% coverage** on Console (RobotDemo orchestration)
- **Unit, integration, and structural tests**
- **Zero conditional logic** (fully deterministic)

---

## 🎨 Design Patterns & Decisions

### 1. Command Pattern

**Why:** Encapsulate robot commands (L, R, F) as objects for flexibility and testability.

```csharp
public interface IRobotCommand
{
    void Execute(Robot robot, MarsGrid grid);
}

public class TurnLeftCommand : IRobotCommand { }
public class TurnRightCommand : IRobotCommand { }
public class MoveForwardCommand : IRobotCommand { }
```

**Benefits:**
- Easy to add new commands
- Commands are testable in isolation
- Supports undo/redo (future enhancement)
- Clear separation of concerns

### 2. Strategy Pattern

**Why:** Different movement strategies (standard, cautious, aggressive) with same interface.

```csharp
public interface IMovementStrategy
{
    bool TryMove(Robot robot, MarsGrid grid);
}

public class StandardMovementStrategy : IMovementStrategy
{
    // Respects scented positions (warns robots of danger)
}
```

**Benefits:**
- Swap strategies at runtime
- Easy to test different behaviors
- Open/Closed Principle compliance

### 3. Dependency Injection (DI)

**Why:** Testability, flexibility, and loose coupling.

**All services use constructor injection:**
```csharp
public sealed class RobotCommunicationService(
    ILogger<RobotCommunicationService> logger,
    RobotCommunicationOptions options,
    IDelayService delayService,
    IFailureSimulator failureSimulator) : IRobotCommunicationService
```

**Benefits:**
- Easy to mock dependencies in tests
- Clear dependency graph
- Supports testing with fake implementations

### 4. Resilience Patterns (Microsoft.Extensions.Resilience)

**Why:** Simulate realistic Mars communication (250+ million km distance, 3-22 min delay).

**Implemented patterns:**
- ✅ **Retry with exponential backoff**
- ✅ **Circuit breaker** (fail-fast after threshold)
- ✅ **Timeout handling**
- ✅ **Cancellation support**

```csharp
public class ResiliencePipelineProvider : IResiliencePipelineProvider
{
    // Auto-detects test vs production:
    // - Tests: 1ms retry delays
    // - Production: 1s retry delays with exponential backoff
}
```

**Benefits:**
- Realistic simulation of space communication
- Configurable failure rates
- Graceful degradation
- Built on Microsoft's official resilience library

### 5. Interface-Based Design

**Why:** Testability and flexibility.

**Every implementation has an interface:**
- `IDelayService` → `DelayService` (production) / `MockDelayService` (tests)
- `IFailureSimulator` → `RandomFailureSimulator` / `NoFailureSimulator` / `AlwaysFailSimulator`
- `IRobotCommunicationService` → `RobotCommunicationService`

**Benefits:**
- 100% deterministic tests (no randomness)
- Easy to swap implementations
- Clear contracts

### 6. Primary Constructors (C# 12)

**Why:** Reduced boilerplate, cleaner code.

```csharp
// Old style
public class MyService
{
    private readonly ILogger _logger;
    public MyService(ILogger logger) => _logger = logger;
}

// Primary constructor
public class MyService(ILogger logger)
{
    // logger is automatically a private readonly field
}
```

**Benefits:**
- Less boilerplate code
- Clearer intent
- Modern C# idioms

### 7. Builder Pattern

**Why:** Complex object construction with validation.

```csharp
public class RobotBuilder
{
    public RobotBuilder WithPosition(int x, int y) { }
    public RobotBuilder FacingDirection(Orientation orientation) { }
    public Robot Build() { }
}
```

**Benefits:**
- Fluent API for readability
- Validation during construction
- Immutability after build

---

## 🧪 Testing Strategy

### Overview

- **Total Tests:** 356
- **Execution Time:** ~1.5 seconds  
- **Line Coverage:** 100% (761/761 lines) ✨
- **Branch Coverage:** 99.5% (205/206 branches)
- **By Project:**
  - **Console:** 100% lines, 100% branches ✨
  - **Abstractions:** 100% lines, 100% branches ✨  
  - **Core:** 100% lines, 99.1% branches
- **Philosophy:** Deterministic, fast, maintainable

**Note:** The single missing branch (0.5%) is in the ResiliencePipelineProvider's retry logging lambda - a compiler-generated branch point that appears unreachable through normal test execution patterns.

### What We Test

#### ✅ **Always Test**
1. **Business Logic** (100% coverage goal)
   - Command execution
   - Movement strategies
   - Grid boundary detection
   - Scent tracking

2. **Service Layer** (99%+ coverage)
   - Robot communication service
   - Resilient controller
   - Delay service
   - Failure simulators

3. **Integration Points** (Full coverage)
   - End-to-end robot navigation
   - DI container configuration
   - Command batch execution

4. **Error Handling** (All paths tested)
   - Exception catch blocks
   - Failure scenarios
   - Cancellation handling

#### 📊 **Coverage Breakdown**

**By Project:**
- **MartianRobots.Console:** 100% line coverage, 100% branch coverage ✨
- **MartianRobots.Abstractions:** 100% line coverage, 100% branch coverage ✨
- **MartianRobots.Core:** 100% line coverage, 99.1% branch coverage

**Test Categories (354 tests):**

1. **RobotDemoTests** (22 tests)
   - File I/O scenarios, multiple robots, error handling
   - Stdin input modes (null file, empty lines, whitespace)
   - Exception handling with logging and disposal
   - Lost robot warnings and failed command statistics
   - Null position handling for complete branch coverage

2. **ApplicationTests** (11 tests)
   - Application lifecycle and orchestration
   - Exception handling and cleanup
   - Dependency injection validation

3. **MovementStrategyBaseTests** (11 tests)
   - Logger integration for all movement paths
   - Boundary collision logging, scent detection

4. **ProgramTests** (5 tests)
   - Main entry point with valid/invalid inputs
   - Exception handling, DI container configuration

5. **OrientationTests** (15 tests, +4 for defensive code)
   - All valid orientation operations
   - **UnreachableException** tests for invalid enum values
   - Complete coverage of defensive error handling

6. **CommandFactoryTests** (13 tests, +2 for logging)
   - Command creation and caching (Flyweight pattern)
   - **Logger integration** tests for debug logging paths

7. **ResiliencePipelineProviderTests** (8 tests, +1 for retry logging)
   - Retry behavior with different exception types
   - **Retry logging** with real logger (not NullLogger)

#### ❌ **Intentionally NOT Tested**

1. **Framework Code**
   - Logging framework internals (Serilog)
   - DI container implementation details
   - **Why:** Already tested by framework authors

2. **Simple DTOs/Models**
   - Auto-properties without logic
   - Record types with compiler-generated code
   - **Why:** No custom logic to test

### Testing Principles

#### 1. **Zero Conditional Logic in Tests**

❌ **Bad** (has conditionals):
```csharp
[Fact]
public void Test()
{
    if (result.Success) 
    {
        Assert.True(result.Value > 0);
    }
}
```

✅ **Good** (deterministic):
```csharp
[Fact]
public void WhenSuccessful_ShouldHavePositiveValue()
{
    // Arrange with mocks to guarantee success
    var result = sut.Execute();
    
    // Assert
    result.Success.Should().BeTrue();
    result.Value.Should().BePositive();
}
```

#### 2. **Deterministic Test Data**

All randomness is **eliminated** via interfaces:

```csharp
// Tests use NoFailureSimulator (always succeeds)
services.AddSingleton<IFailureSimulator, NoFailureSimulator>();

// Production uses RandomFailureSimulator (10% failure rate)
services.AddSingleton<IFailureSimulator>(sp => 
    new RandomFailureSimulator(0.1));
```

#### 3. **Fast Tests via Mocking**

```csharp
// MockDelayService executes instantly (no real delays)
public class MockDelayService : IDelayService
{
    public Task DelayAsync(TimeSpan delay, CancellationToken ct)
    {
        // Records call but returns immediately
        return Task.CompletedTask;
    }
}
```

#### 4. **Test Organization**

```
MartianRobots.Tests/
├── Console/           # Console app structural tests
├── Core/
│   ├── Builders/      # Builder pattern tests
│   ├── Commands/      # Command pattern tests
│   ├── Communication/ # Service layer tests
│   ├── Parsing/       # Input parsing tests
│   ├── Resilience/    # Resilience pipeline tests
│   ├── Services/      # Service implementation tests
│   ├── Strategies/    # Strategy pattern tests
│   └── Validation/    # Validation logic tests
├── Integration/       # End-to-end tests
├── Mocks/             # Test doubles
└── Models/            # Domain model tests
```

### Test Performance

**Before Optimization:** 9.3 seconds (resilience retry delays)
**After Optimization:** 1.4 seconds (85% faster)

**Key Optimization:**
```csharp
// Auto-detect test vs production based on BaseDelay
var retryDelay = opts.BaseDelay < TimeSpan.FromMilliseconds(100) 
    ? TimeSpan.FromMilliseconds(1)  // Fast for tests
    : TimeSpan.FromSeconds(1);       // Production delays
```

This eliminated 4-6 seconds of retry delays in tests while maintaining full resilience testing.

### Coverage Exclusions

**Explicitly excluded with `[ExcludeFromCodeCoverage]`:**
- `Program.cs` Main method (entry point)
- Exception constructors (framework code)
- Auto-generated code

**Documented reasoning:** Entry points provide minimal test value and are validated through integration tests and fail-fast behavior.

---

## 📁 Project Structure

```
robots/
├── MartianRobots.Abstractions/
│   ├── Commands/
│   │   └── IRobotCommand.cs           # Command interface
│   ├── Models/
│   │   ├── MarsGrid.cs                # Grid with scent tracking
│   │   ├── Orientation.cs             # N, S, E, W enum
│   │   ├── Position.cs                # X, Y coordinates (record)
│   │   ├── Robot.cs                   # Robot state & logic
│   │   └── RobotCommunicationModels.cs # DTOs for communication
│   ├── Services/
│   │   ├── IDelayService.cs           # Async delay abstraction
│   │   └── IFailureSimulator.cs       # Failure injection interface
│   └── Strategies/
│       └── IMovementStrategy.cs       # Movement strategy interface
│
├── MartianRobots.Core/
│   ├── Builders/
│   │   └── RobotBuilder.cs            # Fluent robot construction
│   ├── Commands/
│   │   ├── CommandFactory.cs          # Command instantiation
│   │   └── RobotCommands.cs           # L, R, F implementations
│   ├── Communication/
│   │   ├── IResilientRobotController.cs
│   │   ├── IRobotCommunicationService.cs
│   │   ├── ResilientRobotController.cs    # Retry logic wrapper
│   │   └── RobotCommunicationService.cs   # Mars comm simulation
│   ├── Parsing/
│   │   └── InputParser.cs             # Parse input format
│   ├── Resilience/
│   │   └── ResiliencePipelineProvider.cs  # Resilience configuration
│   ├── Services/
│   │   ├── DelayService.cs            # Production delays
│   │   └── FailureSimulators.cs       # Random/No/Always fail
│   ├── Strategies/
│   │   └── StandardMovementStrategy.cs # Respects scents
│   └── Validation/
│       └── InputValidator.cs          # Input validation
│
├── MartianRobots.Console/
│   ├── Program.cs                     # Entry point [ExcludeFromCodeCoverage]
│   ├── RobotDemo.cs                   # Orchestration logic
│   └── sample-simulation.txt          # Example input
│
├── MartianRobots.Tests/
│   ├── Console/                       # Console structural tests
│   ├── Core/                          # Core logic unit tests
│   ├── Integration/                   # End-to-end tests
│   ├── Mocks/                         # Test doubles
│   └── Models/                        # Model tests
│
├── coverlet.runsettings              # Coverage configuration
├── robots.sln                        # Solution file
└── README.md                         # This file
```

---

## ⚙️ Configuration

### Robot Communication Options

```csharp
public sealed class RobotCommunicationOptions
{
    // Network simulation
    public TimeSpan BaseDelay { get; set; } = TimeSpan.FromMilliseconds(500);
    public TimeSpan MaxRandomDelay { get; set; } = TimeSpan.FromSeconds(1);
    public double FailureProbability { get; set; } = 0.1; // 10%
    
    // Resilience
    public int MaxRetryAttempts { get; set; } = 3;
    public TimeSpan CommandTimeout { get; set; } = TimeSpan.FromSeconds(10);
    
    // Circuit breaker
    public int CircuitBreakerThreshold { get; set; } = 5;
    public TimeSpan CircuitBreakerSamplingDuration { get; set; } = TimeSpan.FromSeconds(30);
    public int CircuitBreakerMinimumThroughput { get; set; } = 10;
}
```

### Test vs Production Configuration

**Production** (Program.cs):
```csharp
services.AddSingleton(new RobotCommunicationOptions
{
    BaseDelay = TimeSpan.FromMilliseconds(500),    // Realistic Mars delays
    MaxRandomDelay = TimeSpan.FromSeconds(1),
    FailureProbability = 0.1,                      // 10% failure rate
    MaxRetryAttempts = 3
});

services.AddSingleton<IDelayService, DelayService>();  // Real delays
services.AddSingleton<IFailureSimulator>(sp => 
    new RandomFailureSimulator(0.1));                  // Random failures
```

**Tests**:
```csharp
services.AddSingleton(new RobotCommunicationOptions
{
    BaseDelay = TimeSpan.FromMilliseconds(1),      // Fast for tests
    MaxRandomDelay = TimeSpan.FromMilliseconds(1),
    FailureProbability = 0.0,                      // No randomness
    MaxRetryAttempts = 3
});

services.AddSingleton<IDelayService, MockDelayService>();  // Instant
services.AddSingleton<IFailureSimulator, NoFailureSimulator>(); // Deterministic
```

### Logging Configuration

**Serilog** with console and file sinks:
```csharp
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File($"logs/martian-robots-{DateTime.Now:yyyy-MM-dd}.log")
    .CreateLogger();
```

Logs are written to `logs/` directory (gitignored).

---

## 👨‍💻 Development

### Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download)
- IDE: [Visual Studio Code](https://code.visualstudio.com/), [Visual Studio 2022](https://visualstudio.microsoft.com/), or [Rider](https://www.jetbrains.com/rider/)

### Clone & Build

```bash
git clone https://github.com/nevseev/robots.git
cd robots
dotnet restore
dotnet build
```

### Run Tests

```bash
# All tests
dotnet test

# With coverage
dotnet test --settings coverlet.runsettings --collect:"XPlat Code Coverage"

# Watch mode (re-run on file changes)
dotnet watch test

# Specific filter
dotnet test --filter "FullyQualifiedName~RobotCommunication"
```

### Code Style

- **C# 12** with latest features (primary constructors, file-scoped namespaces)
- **Nullable reference types** enabled
- **EditorConfig** for consistent formatting
- **XML documentation** for public APIs

### Adding New Features

1. **Define interface** in `MartianRobots.Abstractions`
2. **Implement** in `MartianRobots.Core`
3. **Write tests** in `MartianRobots.Tests` (unit + integration)
4. **Register in DI** in `Program.cs`
5. **Ensure deterministic tests** (no randomness, use mocks)

### Debugging

```bash
# Run with debugger attached (VS Code)
F5

# Run with logging enabled
dotnet run --project MartianRobots.Console sample-simulation.txt
```

Check `logs/` directory for detailed logs.

---

## ⚡ Performance

### Test Execution

| Metric | Value | Target | Status |
|--------|-------|--------|--------|
| Total tests | 302 | - | ✅ |
| Execution time | 1.4s | <15s | ✅ **Exceptional** |
| Average per test | 5ms | <50ms | ✅ **Outstanding** |
| Slowest test | 53ms | <100ms | ✅ Excellent |

### Coverage

| Project | Coverage | Target | Status |
|---------|----------|--------|--------|
| Core | 100% | >90% | ✅ **Outstanding** |
| Console | 71.97% | >70% | ✅ Excellent |
| Abstractions | 97.33% | >90% | ✅ Excellent |
| Overall | 93.8% | >90% | ✅ **Outstanding** |

*Note: Entry point (Program.Main) intentionally has minimal testing - integration tests cover DI setup.*

### Optimization History

**Problem:** Resilience tests were taking 4-6 seconds due to retry delays.

**Solution:** Auto-detect test environment based on `BaseDelay` configuration.

```csharp
var retryDelay = opts.BaseDelay < TimeSpan.FromMilliseconds(100) 
    ? TimeSpan.FromMilliseconds(1)  // Fast for tests
    : TimeSpan.FromSeconds(1);       // Production delays
```

**Result:** 85% faster (9.3s → 1.4s)

---

## 📚 Key Learnings & Best Practices

### 1. **Testability First**
- Every dependency is an interface
- Randomness is isolated and mockable
- No static dependencies (except framework)

### 2. **Zero Test Conditionals**
- All tests are deterministic
- Mocks control behavior explicitly
- No `if` statements in test code

### 3. **Coverage ≠ Quality**
- 93.8% coverage is excellent, but...
- Entry points provide minimal test value
- Focus on business logic, not boilerplate

### 4. **Performance Matters**
- Fast tests encourage TDD
- Sub-second feedback loop
- Optimize bottlenecks (retry delays)

### 5. **Smart Abstractions**
- `IDelayService` makes tests instant
- `IFailureSimulator` controls randomness
- Interfaces enable testing without pain

---

## 📄 License

This project is for educational and demonstration purposes.

---

## 🤝 Contributing

This is a demonstration project, but suggestions and improvements are welcome:

1. Fork the repository
2. Create a feature branch
3. Add tests (maintain >99% Core coverage)
4. Submit a pull request

---

## 📞 Contact

**Author:** Nikolay Evseev  
**Repository:** [github.com/nevseev/robots](https://github.com/nevseev/robots)

---

## 🎓 Educational Value

This project demonstrates:

✅ **Clean Architecture** - Clear separation of concerns  
✅ **SOLID Principles** - Especially Dependency Inversion  
✅ **Design Patterns** - Command, Strategy, Builder, Factory  
✅ **Resilience Engineering** - Retry, circuit breaker, timeout  
✅ **Test-Driven Development** - 99% coverage, deterministic tests  
✅ **Modern C#** - Primary constructors, records, pattern matching  
✅ **DevOps** - Fast CI/CD with 1.4s test execution  

Perfect for:
- Learning advanced .NET patterns
- Understanding resilience in distributed systems
- Studying comprehensive test strategies
- Exploring clean architecture principles

---

**Happy Coding! 🚀**
