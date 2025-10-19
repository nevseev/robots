# Martian Robots - Mars Surface Navigation System

A sophisticated .NET application for simulating Mars rover navigation with realistic communication patterns, resilience strategies, and comprehensive test coverage.

[![.NET Version](https://img.shields.io/badge/.NET-9.0-purple)](https://dotnet.microsoft.com/)
[![Test Coverage](https://img.shields.io/badge/Coverage-99.13%25-brightgreen)](https://github.com/nevseev/robots)
[![Tests](https://img.shields.io/badge/Tests-302%20passing-success)](https://github.com/nevseev/robots)

## 📋 Table of Contents

- [Overview](#overview)
- [Quick Start](#quick-start)
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

- ✅ **99.13% code coverage** on core business logic
- ⚡ **Sub-second test execution** (302 tests in ~1.4s)
- 🔄 **Resilience patterns** with Polly (retry, circuit breaker)
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
- **Resilience patterns** (Polly integration)
- **Input parsing and validation**
- **Command pattern implementation**

#### **MartianRobots.Console**
- **Application entry point** only
- **Dependency injection setup**
- **Configuration** (logging, services)
- **Minimal logic** (marked `[ExcludeFromCodeCoverage]`)

#### **MartianRobots.Tests**
- **302 comprehensive tests**
- **99.13% coverage** on Core
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

### 4. Resilience Patterns (Polly)

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

- **Total Tests:** 302
- **Execution Time:** ~1.4 seconds
- **Coverage:** 99.13% (Core), 78.62% (Overall)
- **Philosophy:** Deterministic, fast, maintainable

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

#### ❌ **Intentionally NOT Tested**

1. **Program.cs Entry Point**
   - Marked `[ExcludeFromCodeCoverage]`
   - Tested via structural tests only
   - **Why:** Low value, high complexity, fail-fast behavior

2. **Framework Code**
   - Logging calls (Serilog)
   - DI container internals
   - **Why:** Already tested by framework authors

3. **Simple DTOs/Models**
   - Auto-properties
   - Record types
   - **Why:** Compiler-generated, no logic

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
│   ├── Resilience/    # Polly pipeline tests
│   ├── Services/      # Service implementation tests
│   ├── Strategies/    # Strategy pattern tests
│   └── Validation/    # Validation logic tests
├── Integration/       # End-to-end tests
├── Mocks/             # Test doubles
└── Models/            # Domain model tests
```

### Test Performance

**Before Optimization:** 9.3 seconds (Polly retry delays)
**After Optimization:** 1.4 seconds (85% faster)

**Key Optimization:**
```csharp
// Auto-detect test vs production based on BaseDelay
var retryDelay = opts.BaseDelay < TimeSpan.FromMilliseconds(100) 
    ? TimeSpan.FromMilliseconds(1)  // Fast for tests
    : TimeSpan.FromSeconds(1);       // Production delays
```

This eliminated 4-6 seconds of Polly retry delays in tests while maintaining full resilience testing.

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
│   │   └── ResiliencePipelineProvider.cs  # Polly configuration
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
| Core | 99.13% | >90% | ✅ **Outstanding** |
| Abstractions | 97.33% | >90% | ✅ Excellent |
| Overall | 78.62% | >75% | ✅ Good |

*Note: Console project intentionally excluded (entry point).*

### Optimization History

**Problem:** Resilience tests were taking 4-6 seconds due to Polly retry delays.

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
- 99% coverage is excellent, but...
- Entry points don't need coverage (fail-fast)
- Focus on business logic, not boilerplate

### 4. **Performance Matters**
- Fast tests encourage TDD
- Sub-second feedback loop
- Optimize bottlenecks (Polly delays)

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
