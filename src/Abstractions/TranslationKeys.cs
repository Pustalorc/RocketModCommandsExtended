using JetBrains.Annotations;

namespace Pustalorc.Libraries.RocketModCommandsExtended.Abstractions;

/// <summary>
///     Translation keys that the library utilizes for general help.
/// </summary>
[PublicAPI]
public static class TranslationKeys
{
    /// <summary>
    ///     The translation key to be used whenever a command throws an exception.
    /// </summary>
    public const string CommandExceptionKey = "command_exception";

    /// <summary>
    ///     The translation key to be used whenever a command is used incorrectly.
    /// </summary>
    public const string CommandUsageKey = "command_usage";
}