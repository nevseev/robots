# Martian Robots Solution

This solution simulates robots moving on Mars based on given instructions. The codebase has been separated into multiple assemblies following clean architecture principles.

## Architecture

The solution is organized into three assemblies:

### MartianRobots.Abstractions
Contains all interfaces, abstract classes, and models that define the contracts:
- `Models/` - Core domain models (Position, Orientation, Robot, MarsGrid)
- `Commands/` - Command pattern interfaces (IRobotCommand)
- `Strategies/` - Strategy pattern interfaces and base classes (IMovementStrategy, MovementStrategyBase)
- `Templates/` - Template method pattern base classes (RobotSimulationTemplate)

### MartianRobots.Core
Contains all concrete implementations of the abstractions:
- `Commands/` - Concrete command implementations (TurnLeftCommand, TurnRightCommand, MoveForwardCommand, CommandFactory)
- `Strategies/` - Concrete movement strategies (StandardMovementStrategy)
- `Services/` - Main simulation service (RobotSimulationService)
- `Parsing/` - Input parsing logic (InputParser)
- `Validation/` - Input validation logic (InputValidator)
- `Builders/` - Builder pattern implementations (RobotBuilder, MarsGridBuilder)

### MartianRobots.Console
The console application entry point that uses the Core library.

### MartianRobots.Tests
Comprehensive unit test suite with 210+ tests covering all assemblies:
- **Models Tests**: Position, Orientation, Robot, MarsGrid (40+ tests)
- **Commands Tests**: All robot commands and command factory (25+ tests)  
- **Strategies Tests**: Movement strategies and boundary handling (15+ tests)
- **Validation Tests**: Input validation logic (35+ tests)
- **Parsing Tests**: Input parsing functionality (20+ tests)
- **Services Tests**: Robot simulation service (25+ tests)
- **Builders Tests**: Builder pattern implementations (15+ tests)

Features:
- **xUnit Framework**: Modern testing framework with extensive assertions
- **FluentAssertions**: Readable and expressive test assertions
- **Moq**: Mocking framework for dependency isolation
- **Code Coverage**: Integrated coverage reporting with Coverlet

## Benefits of the New Architecture

1. **Separation of Concerns**: Clear separation between abstractions and implementations
2. **Dependency Inversion**: High-level modules depend on abstractions, not concretions
3. **Testability**: Easy to unit test by mocking interfaces
4. **Extensibility**: New implementations can be added without changing existing code
5. **Reusability**: Core logic can be reused in different applications (console, web, etc.)

## Running the Application

```bash
cd MartianRobots.Console
dotnet run < sample-input.txt
```

## Running Tests

```bash
# Run all tests
dotnet test

# Run tests with code coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test class
dotnet test --filter "ClassName=RobotTests"

# Run tests with detailed output
dotnet test --verbosity normal
```

## Building the Solution

```bash
dotnet build
```

This will build all assemblies in the correct dependency order.

## Design Patterns Used

- **Command Pattern**: Robot commands (L, R, F)
- **Strategy Pattern**: Movement strategies
- **Template Method Pattern**: Simulation workflow
- **Builder Pattern**: Object construction
- **Factory Pattern**: Command creation
- **Flyweight Pattern**: Command instance reuse

## Dependencies

- .NET 9.0
- No external dependencies required
