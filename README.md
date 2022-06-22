# RocketMod Commands Extended [![NuGet](https://img.shields.io/nuget/v/Pustalorc.RocketModExtended.Commands.svg)](https://www.nuget.org/packages/Pustalorc.RocketModExtended.Commands/)

Library to add abstracted command classes to make setups for commands under [RocketMod (or LDM, as maintained by Nelson)](https://github.com/SmartlyDressedGames/Legally-Distinct-Missile) easier.

# Quick notes

Please be aware that all 3 of the abstracted classes inherit from eachother in the following order:  
`MultiThreadedRocketCommand` -> `RocketCommandWithTranslations` -> `RocketCommandWithParsing`  
So inheriting from `RocketCommandWithParsing` will also inherit the multithread support and translations support.

If you wish to contribute to this repository, feel free by creating a PR to add/remove or change anything as need be.

# References

This project includes the following NuGet packages in order to function as intended:  
[CommandLineParser v2.9.1](https://www.nuget.org/packages/CommandLineParser/2.9.1)  
[OpenMod.Unturned.Redist](https://www.nuget.org/packages/OpenMod.Unturned.Redist)  
[OpenMod.UnityEngine.Redist](https://www.nuget.org/packages/OpenMod.UnityEngine.Redist)  

# Usage

All you need for usage is to create a class and inherit from one of the 3 abstracted classes.  
For example:
```cs
public sealed class PPlayersCommand : RocketCommandWithParsing<HelpOnly>
{
    protected override Dictionary<string, string> DefaultTranslations => new()
    {
        { "command_exception", "Error during command execution. Command: {0}. Error: {1}. Full exception: {2}" },
        { "error_external_perm_provider", "This command cannot be used with an external permissions provider. Please run `/ap -p`, or make sure no other plugin overrides advanced permissions. External provider: {0}" },
        { "list_players", "{0} Players: {1}" },
        { "list_players_format", "{0} [{1}]" }
    };

    public override AllowedCaller AllowedCaller => AllowedCaller.Both;
    public override string Name => "p.players";
    public override string Help => "Lists all permission players on the server.";
    public override string Syntax => "[-h|--help]";

    public PPlayersCommand(bool multiThreaded, Dictionary<string, string> translations) : base(multiThreaded, translations)
    {
    }

    public override async Task ExecuteAsync(IRocketPlayer caller, HelpOnly parsed)
    {
        switch (R.Permissions)
        {
            case IPermissionProviderController provider:
                var players = (await provider.GetPlayers())
                    .Select(k => Translate("list_players_format", k.LastSeenDisplayName, k.Key)).ToArray();

                SendTranslatedMessage(caller, "list_players", players.Length, string.Join(", ", players));
                break;
            default:
                SendTranslatedMessage(caller, "error_external_perm_provider", R.Permissions.GetType().FullName);
                break;
        }
    }
}
```

To explain a bit of what goes on here:

`HelpOnly` is a class used to only provide access to `--help` or `-h` when performing the command parsing within `RocketCommandWithParsing<T>`.  
This `HelpOnly` class can be inherited from other classes, but you can also inherit from `CommandParsing`, which already enables `--help` or `-h`.  
Note that you cannot use `CommandParsing` for `RocketCommandWithParsing<T>`.

`DefaultTranslations` is a dictionary for quick lookups on translations, and will be used by `RocketCommandWithTranslations` if the translation is not found from the loaded translations.

`AllowedCaller`, `Name`, `Help`, `Syntax` are all required properties by default from rocketmod, and will not be assumed from class names or anything.

The constructor requires a `bool` to determine if the command should be multithreaded. If you do not wish to provide an option in configuration to users for this, simply remove it from the consrtuctor and just pass `true` to the base constructor.  
A `Dictionary<string, string>` is also needed to get the currently loaded translations, either from rocket or somewhere else.

Finally, the `ExecuteAsync` method passes the caller as usual, but also the parsed input, in this case `HelpOnly` which will always be `false` since `RocketCommandWithParsing<T>` automatically handles printing the help information.


# Further usage

## `RocketCommandWithParsing<T>`
If you wish, you can override the following method to replace or change the help message for the command:  
`protected virtual Task DisplayHelp(IRocketPlayer caller, ParserResult<T> parserResult)`

You can also override the following method to change how parsing is performed or when the help message is raised:  
`public override async Task ExecuteAsync(IRocketPlayer caller, string[] command)`

The constructor for `RocketCommandWithParsing<T>` has an additional and optional parameter `Parser? parser = null` which allows you to change the parser's settings instead of using a default.

---

## `RocketCommandWithTranslations`
From the input loaded translations, only the required ones from `DefaultTranslations` are used, so if you find yourself seeing a translation key but no message, despite having it on the default translations **of the plugin**, you should consider adding it to the command instead.

You can reload translations if you ever need to with the following method:  
`public virtual void ReloadTranslations(Dictionary<string, string> translations)`

You can override the following method in order to change how translations are retrieved and how the variables within the translation are parsed:  
`public virtual string Translate(string translationKey, params object?[] placeholder)`  
By default, this method will replace `{i}`, where i is a number, from within translations, but only as many i's as count in the placeholder input, so having a translation with `{0} said {1} {2}` will not throw an error if you only pass to the placeholder 2 elements

You can also override the following method to change how exceptions are logged when the command executes, as they are captured due to the default multithreaded support:  
`protected override void LogException(IRocketPlayer caller, Exception exception)`  
By default this method will use the translation key `command_exception`, so you should include one always in your command's default translations.

---

## `MultiThreadedRocketCommand`

You can change the multithreaded setting with the following method:  
`public void ReloadMultiThreaded(bool multiThreaded)`  
This is useful if you are using a configuration setting for this option and the configuration gets changed and reloaded.

Like with `RocketCommandWithTranslations`, you can override the following method in order to change how exceptions get logged.  
`protected virtual void LogException(IRocketPlayer caller, Exception exception)`  
Since the multithreaded class does not include translations by default, this class will log a built in default message in english that will mention the command name that failed, the exception message, and the full exception.

You can also override the following method to change how multithreading is done, or if it even should be done at all:
`public virtual void Execute(IRocketPlayer caller, string[] command)`

- Multithreaded commands by default with async support. No more having the server freeze because your command is taking 542873 years to query a database on the main thread.
- Thread-safe message sending. Sending a message from a different thread and you now have to check if you are on main thread or not? You can stop caring, the code will redirect it to the main thread where necessary. (Note, you should still be careful if you do something like Barricade.Destroy from this).
- Translations built into the command and exported/exposed as part of the plugin's main translations (or if you want, each command has its own translation file, this will be up to the dev to chose and re-implement).
- Less error prone translations. User added {1234} onto the translation? Not to worry! you won't get a pesky error because the user did this, simply it will be left as is.
- Translation is missing? Default it to internal default translations if the user is missing the translation from the translation file. No more my_translation_key in chat and leaving players confused. Instead you can leave them confused in English.
- Command parsing, forget about reading string[] command and just build a class based on this API https://github.com/commandlineparser/commandline and pass it to the command with parsing when you inherit it. This will simplify all command execution and parameter validation to an external library so you no longer need to write 5000 if statements checking the size of the command array, or if the value is precisely xyz, the library will handle it.
