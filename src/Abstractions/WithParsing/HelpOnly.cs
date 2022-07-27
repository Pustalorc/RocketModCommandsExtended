using JetBrains.Annotations;

namespace Pustalorc.Libraries.RocketModCommandsExtended.Abstractions.WithParsing;

/// <inheritdoc />
/// <summary>
/// A basic class in order to pass "the default" to RocketCommandWithTranslations&lt;T&gt;
/// </summary>
/// <remarks>
/// The class restricts to CommandParsing implementations on purpose as to force the user to at least have support for -h.
/// However, in order to discourage users from just passing that class when they see the error, this separate class was developed.
/// In the end, this class exists to mitigate devs who would just put the "missing" or "required" class instead of doing it right.
/// </remarks>
[UsedImplicitly]
public class HelpOnly : CommandParsing
{
}