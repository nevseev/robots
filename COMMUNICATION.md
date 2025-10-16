# Mars Robot Communication System

## Overview
This project has been enhanced with a real-time robot communication system that enables remote control of Mars rovers with resilience patterns to handle the challenges of Mars communication.

## Key Features

### Real-Time Communication
- **Robot Instances**: Connect to and manage multiple Mars rovers simultaneously
- **Command Acknowledgment**: All commands are tracked with unique IDs and acknowledgment responses
- **Mars Distance Simulation**: Realistic communication delays (500ms-1.5s) to simulate Earth-Mars communication
- **Connection Management**: Maintain persistent connections with automatic reconnection capabilities

### Resilience Patterns
- **Circuit Breaker**: Automatically opens circuit when failure threshold is reached to prevent cascade failures
- **Retry Logic**: Exponential backoff with configurable retry attempts for failed operations
- **Timeout Handling**: Command timeouts with graceful failure handling
- **Failure Simulation**: 10% random failure rate to test resilience patterns

### Communication Models
- **RobotInstance**: Represents a connected Mars rover with state tracking
- **RobotCommand**: Command objects with acknowledgment tracking
- **CommandResponse**: Response objects with execution status and new robot state
- **ConnectionState**: Track robot connection status (Connected, Disconnected, Failed)

## Architecture

### Core Components

#### MartianRobots.Core.Communication
- **IRobotCommunicationService**: Main interface for robot communication
- **RobotCommunicationService**: Core implementation with Mars simulation
- **ResilientRobotController**: Resilient wrapper with circuit breakers and retry logic
- **RobotCommunicationModels**: Data models and configuration options

#### Resilience Implementation
- **Microsoft.Extensions.Resilience**: Uses .NET's built-in resilience library
- **ResiliencePipeline**: Separate pipelines for connection, command, and query operations
- **Circuit Breaker Configuration**: 5 failures in 30 seconds triggers circuit breaker
- **Retry Configuration**: 3 attempts with exponential backoff

## Usage

### Running the Communication Demo
```bash
dotnet run --project MartianRobots.Console -- --communication-demo
```

### Running the Original Simulation
```bash
echo "5 3\n1 1 E\nRFRFRFRF" | dotnet run --project MartianRobots.Console
```

## Demo Features

The communication demo demonstrates:

1. **Robot Connection Management**: Connect to multiple Mars rovers
2. **Resilient Command Execution**: Send commands with automatic retry on failure
3. **Health Monitoring**: Check robot health and retrieve current state
4. **Instruction Sequences**: Execute complex command sequences with failure handling
5. **Mars Communication Simulation**: Realistic delays and failure scenarios

## Technical Implementation

### Configuration
```csharp
services.AddSingleton(new RobotCommunicationOptions
{
    BaseDelay = TimeSpan.FromMilliseconds(500),
    MaxRandomDelay = TimeSpan.FromMilliseconds(1000),
    FailureProbability = 0.1,
    MaxRetryAttempts = 3,
    CommandTimeout = TimeSpan.FromSeconds(5)
});
```

### Resilience Pipelines
- **Connection Pipeline**: Retry with exponential backoff for robot connections
- **Command Pipeline**: Circuit breaker + retry for command execution
- **Query Pipeline**: Lightweight retry for state queries

### Example Output
```
[19:30:59 INF] === Mars Robot Communication System Demo ===
[19:30:59 INF] Connecting to robot MARS-ROVER-1...
[19:30:59 INF] ✅ Successfully connected to robot MARS-ROVER-1
[19:31:04 INF] Sending command 'R' to robot MARS-ROVER-1...
[19:31:07 INF] ✅ Command executed successfully. Robot at (2, 1) facing East
[19:31:47 WRN] Command attempt 1 failed, retrying in 500ms: Robot MARS-ROVER-2 failed to execute command R: Simulated communication failure
[19:31:50 DBG] Successfully executed resilient command 'R' on robot MARS-ROVER-2
```

## Benefits

1. **Production Ready**: Enterprise-grade resilience patterns for unreliable networks
2. **Mars Realistic**: Simulates actual Mars communication challenges
3. **Monitoring**: Comprehensive logging and health checks
4. **Scalable**: Can manage multiple robots simultaneously
5. **Testable**: Built-in failure simulation for testing resilience

This enhancement transforms the simple Mars robot simulation into a sophisticated real-time communication system suitable for actual Mars rover operations.