using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Rocket.API;

namespace Pustalorc.Libraries.RocketModCommandsExtended.Abstractions;

/// <inheritdoc />
/// <summary>
/// Abstract class to add support for built in translations.
/// Note that this also adds multi-threaded support.
/// </summary>
public abstract class RocketCommandWithTranslations : MultiThreadedRocketCommand
{
    /// <summary>
    /// The default translations of the command.
    /// </summary>
    /// <remarks>
    /// This property should be public (it wasn't pre-release), as the intended thing to do here is have the developer
    /// manually retrieve the default translations of all their commands and serialize them into the translations file.
    ///
    /// Please note, you have to add a default translation with the following key: command_exception
    /// This is to support the RaisedException method so it logs the correct message.
    /// See MultiThreadedRocketCommand.RaisedException for what kind of message you should be aiming to.
    /// </remarks>
    [UsedImplicitly]
    public abstract Dictionary<string, string> DefaultTranslations { get; }

    /// <summary>
    /// The currently loaded translations from the translations file.
    /// </summary>
    protected Dictionary<string, string> Translations { get; }


    /// <inheritdoc />
    /// <summary>
    /// The required constructor for this class.
    /// Sets if the command will be multi-threaded or not, as well as the currently loaded translations.
    /// </summary>
    /// <param name="multiThreaded">
    /// True if you want the command to always run on a separate thread.
    /// False otherwise.
    /// </param>
    /// <param name="translations">
    /// The currently loaded translations.
    /// </param>
    /// <remarks>
    /// Please note that the constructor will not filter the currently loaded translations.
    /// This is due to a limitation with C#, where the base type constructors are called first,
    /// so the default translations might not be set yet.
    /// 
    /// Also note, if no translations exist yet (translations file didn't exist, some error occurred, etc)
    /// please instantiate the command with an empty dictionary, followed by calling ReloadTranslations on the command.
    ///
    /// Final note, if you do not wish to filter at all the translations, you can also pass an empty dictionary.
    /// This minimum is required to determine what comparer to use when getting the translation later on.
    /// </remarks>
    protected RocketCommandWithTranslations(bool multiThreaded, Dictionary<string, string> translations) : base(
        multiThreaded)
    {
        Translations = translations;
    }

    /// <summary>
    /// Clears and reloads the loaded translations for this command.
    /// </summary>
    /// <param name="translations">
    /// The currently loaded translations.
    /// </param>
    /// <remarks>
    /// Unlike the constructor, this method will filter through the input loaded translations and just grab the required ones.
    /// This method will also NOT change the string comparer of the loaded translations, as that is set in the constructor.
    /// </remarks>
    [UsedImplicitly]
    public virtual void ReloadTranslations(Dictionary<string, string> translations)
    {
        Translations.Clear();

        // Note: I couldn't decide if I wanted the default translations applying filters on the passed translations
        // or the other way around... So feel free to override this method if you want it some other way.
        // I decided to stick to default translations being compared to the loaded ones since the loaded translations
        // will generally be a larger size than the default one. (translations > default)
        // The only downside to this is that whatever calls this method decides what comparer to use.

        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        // No need to spend one more enumerator for a simple select of a key.
        foreach (var keyValuePair in DefaultTranslations)
        {
            var key = keyValuePair.Key;

            if (!translations.TryGetValue(key, out var value))
                continue;

            Translations.Add(key, value);
        }
    }

    /// <summary>
    /// Gets the translated message for the specified key.
    /// </summary>
    /// <param name="translationKey">The key that identifies the translation message in the loaded translations.</param>
    /// <param name="placeholder">A params object array that allows to input any data into the translation.</param>
    /// <returns>
    /// A translated message.
    /// </returns>
    /// <remarks>
    /// The params is a nullable object array due to backwards compatibility.
    /// Unlike RocketMod's translations methods, this one will NOT throw an exception if the message has
    /// {n} where n > placeholder count.
    ///
    /// Please note, a placeholder that is null will be replaced with the plaintext NULL.
    ///
    /// Also unlike RocketMod, in the case that a translation wasn't loaded from file, this method will still get it from
    /// the default translations object. This makes finding missing translations outright easier, but also makes users slightly
    /// less confused when there's an update (unless they are non-native to the translations default language, in which case
    /// they'll be confused why they are seeing an un-translated message)
    /// </remarks>
    [UsedImplicitly]
    public virtual string Translate(string translationKey, params object?[] placeholder)
    {
        if (!Translations.TryGetValue(translationKey, out var translation) &&
            !DefaultTranslations.TryGetValue(translationKey, out translation))
            return translationKey;

        for (var i = 0; i < placeholder.Length; i++)
        {
            var arg = placeholder[i];
            translation = translation.Replace($"{{{i}}}", arg?.ToString() ?? "NULL");
        }

        return translation;
    }

    /// <summary>
    /// Sends a translated message to the specific player.
    /// </summary>
    /// <param name="player">The player to send the translated message to.</param>
    /// <param name="translationKey">The key that identifies the translation message in the loaded translations.</param>
    /// <param name="placeholder">A params object array that allows to input any data into the translation.</param>
    [UsedImplicitly]
    protected virtual void SendTranslatedMessage(IRocketPlayer player, string translationKey,
        params object?[] placeholder)
    {
        SendMessage(player, Translate(translationKey, placeholder));
    }

    /// <summary>
    /// Sends a translated message to the global game chat.
    /// </summary>
    /// <param name="translationKey">The key that identifies the translation message in the loaded translations.</param>
    /// <param name="placeholder">A params object array that allows to input any data into the translation.</param>
    [UsedImplicitly]
    protected virtual void SendTranslatedMessage(string translationKey, params object?[] placeholder)
    {
        SendMessage(Translate(translationKey, placeholder));
    }

    /// <inheritdoc />
    protected override void RaisedException(IRocketPlayer caller, string[] commandInput, Exception exception)
    {
        SendTranslatedMessage(caller, "command_exception", Name, string.Join(" ", commandInput), exception.Message,
            exception);
    }
}