#!/bin/bash

# Start Jaeger and observability stack
echo "🚀 Starting Jaeger and observability stack..."
docker-compose up -d

echo "⏳ Waiting for services to be ready..."
sleep 5

# Check if services are running
if ! docker ps | grep -q "martian-robots-jaeger"; then
    echo "❌ Jaeger failed to start"
    exit 1
fi

echo "✅ Services are ready!"
echo ""
echo "📊 Access points:"
echo "  - Jaeger UI:  http://localhost:16686"
echo "  - Prometheus: http://localhost:9090"
echo "  - Grafana:    http://localhost:3000 (admin/admin)"
echo ""
echo "🤖 Running Martian Robots with OpenTelemetry..."
echo ""

# Run the application with OTLP exporter
export OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317
dotnet run --project MartianRobots.Console sample-simulation.txt

echo ""
echo "✨ Simulation complete!"
echo ""
echo "🔍 View traces at: http://localhost:16686"
echo "   1. Select 'MartianRobots' service"
echo "   2. Click 'Find Traces'"
echo "   3. Explore the distributed traces!"
echo ""
echo "🛑 To stop the observability stack:"
echo "   docker-compose down"
