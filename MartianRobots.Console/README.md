# MartianRobots Console Application

This is the console application for the Mars Robot Communication System Demo.

## Usage

```bash
dotnet run
```

The application will automatically start the Mars Robot Communication System Demo, which demonstrates:

- Real-time robot communication with connection simulation
- Resilient robot command execution with retry patterns  
- Health monitoring and state management
- Graceful error handling and recovery
- Concurrent robot operations

Press Ctrl+C to stop the demo gracefully.

## Features

- **Robot Communication Service**: Simulates real robot connections with network delays
- **Resilient Controller**: Implements retry patterns with exponential backoff
- **Health Monitoring**: Continuous health checks for connected robots
- **Logging**: Comprehensive logging with Serilog to console and file
- **Graceful Shutdown**: Handles cancellation requests properly