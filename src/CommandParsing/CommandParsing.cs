using CommandLine;
using JetBrains.Annotations;

namespace Pustalorc.Libraries.RocketModCommandsExtended.CommandParsing;

public abstract class CommandParsing
{
    [UsedImplicitly]
    [Option('h', "help", HelpText = "Displays the current help information about the command.")]
    public bool Help { get; set; }
}