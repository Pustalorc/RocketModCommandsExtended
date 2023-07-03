using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Pustalorc.Libraries.RocketModCommandsExtended.Abstractions;
using Rocket.API;
using Rocket.Core;

namespace Pustalorc.Libraries.RocketModCommandsExtended.Extensions;

/// <summary>
///     Extensions for <see cref="IRocketPlugin" />s so they can get the translations for
///     <see cref="RocketCommandWithTranslations" /> as well as load and reload them.
/// </summary>
[PublicAPI]
public static class RocketPluginExtensions
{
    /// <summary>
    ///     Gets the current translations of the plugin from <see cref="IRocketPlugin.Translations" /> and converts it to a
    ///     <see cref="Dictionary{TKey,TValue}" /> that <see cref="RocketCommandWithTranslations" /> can use.
    /// </summary>
    /// <param name="plugin">The plugin we are getting the translations from.</param>
    /// <param name="comparerOverride">
    ///     An override for the <see cref="StringComparer" /> that will be used.
    ///     If left null <see cref="StringComparer.OrdinalIgnoreCase" /> will be used.
    /// </param>
    /// <returns>A <see cref="Dictionary{TKey,TValue}" /> with the translations of that plugin.</returns>
    public static Dictionary<string, string> GetCurrentTranslationsForCommands(this IRocketPlugin plugin,
        StringComparer? comparerOverride = null)
    {
        return plugin.Translations.Instance.ToDictionary(k => k.Id, k => k.Value,
            comparerOverride ?? StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     Loads and registers the selected commands.
    ///     Any commands with translations get their translations registered to the specified plugin instance.
    /// </summary>
    /// <param name="commands">The commands to load and register.</param>
    /// <param name="plugin">The plugin to register new translations to.</param>
    public static void LoadAndRegisterCommands(this IEnumerable<MultiThreadedRocketCommand> commands,
        IRocketPlugin plugin)
    {
        var updatedTranslations = false;
        foreach (var command in commands)
        {
            R.Commands.Register(command);
            foreach (var alias in command.Aliases)
                R.Commands.Register(command, alias);

            if (command is not RocketCommandWithTranslations commandWithTranslations)
                continue;

            foreach (var translation in commandWithTranslations.DefaultTranslations.Where(translation =>
                         plugin.Translations.Instance[translation.Key] == null))
            {
                plugin.Translations.Instance.Add(translation.Key, translation.Value);
                updatedTranslations = true;
            }
        }

        if (!updatedTranslations)
            return;

        plugin.Translations.Save();
    }

    /// <summary>
    ///     Reloads the commands and translations.
    /// </summary>
    /// <param name="commands">The commands to reload.</param>
    /// <param name="plugin">The plugin where the translations are stored.</param>
    public static void ReloadCommands(this IEnumerable<MultiThreadedRocketCommand> commands, IRocketPlugin plugin)
    {
        var currentTranslations = plugin.GetCurrentTranslationsForCommands();

        foreach (var command in commands.OfType<RocketCommandWithTranslations>())
            command.ReloadTranslations(currentTranslations);
    }
}