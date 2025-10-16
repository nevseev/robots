using System.Diagnostics.CodeAnalysis;

namespace MartianRobots.Console;

internal static class Program
{
    [ExcludeFromCodeCoverage]
    private static int Main(string[] args)
    {
        var application = new Application();
        return application.Run();
    }
}