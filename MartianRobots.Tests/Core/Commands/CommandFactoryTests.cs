using MartianRobots.Core.Commands;

namespace MartianRobots.Tests.Core.Commands;

public class CommandFactoryTests
{
    [Theory]
    [InlineData('L', typeof(TurnLeftCommand))]
    [InlineData('R', typeof(TurnRightCommand))]
    [InlineData('F', typeof(MoveForwardCommand))]
    public void GetCommand_WithValidInstruction_ShouldReturnCorrectCommand(char instruction, Type expectedType)
    {
        var command = CommandFactory.GetCommand(instruction);

        command.Should().BeOfType(expectedType);
    }

    [Theory]
    [InlineData('X')]
    [InlineData('1')]
    [InlineData(' ')]
    [InlineData('l')] // lowercase
    public void GetCommand_WithInvalidInstruction_ShouldThrowArgumentException(char invalidInstruction)
    {
        var action = () => CommandFactory.GetCommand(invalidInstruction);
        action.Should().Throw<ArgumentException>()
            .WithMessage($"Invalid instruction: {invalidInstruction}");
    }

    [Fact]
    public void GetCommand_ShouldReturnSameInstanceForSameInstruction()
    {
        var command1 = CommandFactory.GetCommand('L');
        var command2 = CommandFactory.GetCommand('L');

        command1.Should().BeSameAs(command2); // Flyweight pattern - same instance
    }

    [Fact]
    public void CreateCommands_WithValidInstructions_ShouldReturnCorrectSequence()
    {
        // Arrange
        var instructions = "LRF";

        var commands = CommandFactory.CreateCommands(instructions);

        commands.Should().HaveCount(3);
        commands[0].Should().BeOfType<TurnLeftCommand>();
        commands[1].Should().BeOfType<TurnRightCommand>();
        commands[2].Should().BeOfType<MoveForwardCommand>();
    }

    [Fact]
    public void CreateCommands_WithEmptyString_ShouldReturnEmptyList()
    {
        var commands = CommandFactory.CreateCommands("");

        commands.Should().BeEmpty();
    }

    [Fact]
    public void CreateCommands_WithLongSequence_ShouldReturnAllCommands()
    {
        // Arrange
        var instructions = "LFRFRFRFRF";

        var commands = CommandFactory.CreateCommands(instructions);

        commands.Should().HaveCount(10);
        commands.All(c => c is TurnLeftCommand or TurnRightCommand or MoveForwardCommand).Should().BeTrue();
    }

    [Fact]
    public void CreateCommands_WithInvalidInstruction_ShouldThrowArgumentException()
    {
        // Arrange
        var instructions = "LRX";

        var action = () => CommandFactory.CreateCommands(instructions);
        action.Should().Throw<ArgumentException>()
            .WithMessage("Invalid instruction: X");
    }

    [Fact]
    public void GetCommand_ShouldSupportAllValidInstructions()
    {
        // Verify that all valid instructions (L, R, F) are supported
        var validInstructions = new[] { 'L', 'R', 'F' };

        foreach (var instruction in validInstructions)
        {
            var command = CommandFactory.GetCommand(instruction);
            command.Should().NotBeNull();
        }
    }

    [Fact]
    public void CreateCommands_ShouldReuseSameCommandInstances()
    {
        // Arrange
        var instructions = "LLF";

        var commands = CommandFactory.CreateCommands(instructions);

        commands[0].Should().BeSameAs(commands[1]); // Both are TurnLeftCommand instances
        commands[0].Should().BeOfType<TurnLeftCommand>();
        commands[1].Should().BeOfType<TurnLeftCommand>();
        commands[2].Should().BeOfType<MoveForwardCommand>();
    }
}