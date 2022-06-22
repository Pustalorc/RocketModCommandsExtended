using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Rocket.API;

namespace Pustalorc.Libraries.RocketModCommandsExtended.Abstractions;

public abstract class RocketCommandWithTranslations : MultiThreadedRocketCommand
{
    protected abstract Dictionary<string, string> DefaultTranslations { get; }

    protected Dictionary<string, string> Translations { get; set; }


    protected RocketCommandWithTranslations(bool multiThreaded, Dictionary<string, string> translations) : base(
        multiThreaded)
    {
        Translations = translations.Where(k => DefaultTranslations.ContainsKey(k.Key))
            .ToDictionary(k => k.Key, k => k.Value, StringComparer.OrdinalIgnoreCase);
    }

    [UsedImplicitly]
    public virtual void ReloadTranslations(Dictionary<string, string> translations)
    {
        Translations = translations.Where(k => DefaultTranslations.ContainsKey(k.Key))
            .ToDictionary(k => k.Key, k => k.Value, StringComparer.OrdinalIgnoreCase);
    }

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

    [UsedImplicitly]
    protected virtual void SendTranslatedMessage(IRocketPlayer player, string translationKey,
        params object?[] placeholder)
    {
        SendMessage(player, Translate(translationKey, placeholder));
    }

    [UsedImplicitly]
    protected virtual void SendTranslatedMessage(string translationKey, params object?[] placeholder)
    {
        SendMessage(Translate(translationKey, placeholder));
    }

    [UsedImplicitly]
    protected override void LogException(IRocketPlayer caller, Exception exception)
    {
        SendTranslatedMessage(caller, "command_exception", Name, exception.Message, exception);
    }
}