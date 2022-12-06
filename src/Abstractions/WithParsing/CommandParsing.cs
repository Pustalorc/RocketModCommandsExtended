using CommandLine;
using JetBrains.Annotations;

namespace Pustalorc.Libraries.RocketModCommandsExtended.Abstractions.WithParsing;

/// <summary>
///     An abstract class to restrict all WithParsing generic types to require -h --help support.
///     Inherit it in order to use it as a generic type for WithParsing, and add the custom types you want parsed.
/// </summary>
/// <remarks>
///     Documentation for how these should be made is here:
///     https://github.com/commandlineparser/commandline/wiki
/// </remarks>
public abstract class CommandParsing
{
    /// <summary>
    ///     Determines if the help message should be shown.
    /// </summary>
    [UsedImplicitly]
    [Option('h', "help", HelpText = "Displays the current help information about the command.")]
    public bool Help { get; set; }
}