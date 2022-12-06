using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using CommandLine;
using JetBrains.Annotations;
using Pustalorc.Libraries.RocketModCommandsExtended.Abstractions.WithParsing;
using Rocket.API;

namespace Pustalorc.Libraries.RocketModCommandsExtended.Abstractions;

/// <inheritdoc />
/// <summary>
///     Abstract class to add support for automatic command parsing.
///     Note that this also adds multi-threaded AND translation support.
/// </summary>
/// <typeparam name="T">The class used for parsing. Must inherit from CommandParsing</typeparam>
/// <remarks>
///     All parsing is handled by the library CommandLineParser, please read their wiki as to how to format the classes:
///     https://github.com/commandlineparser/commandline/wiki
/// </remarks>
public abstract class RocketCommandWithParsing<T> : RocketCommandWithTranslations
    where T : CommandParsing
{
    /// <summary>
    ///     The parser instance for this command.
    /// </summary>
    /// <remarks>
    ///     Early on in development, I had a global parser per project.
    ///     I've since decided against it as to allow people to enable/disable features depending on the specific command.
    /// </remarks>
    [UsedImplicitly]
    protected Parser CommandParser { get; }

    /// <inheritdoc />
    /// <summary>
    ///     The required constructor for this class.
    ///     Sets if the command will be multi-threaded or not, as well as the currently loaded translations and their comparer,
    ///     and offers an optional setting for a custom parser.
    /// </summary>
    /// <param name="multiThreaded">
    ///     True if you want the command to always run on a separate thread.
    ///     False otherwise.
    /// </param>
    /// <param name="translations">
    ///     The currently loaded translations.
    /// </param>
    /// <param name="comparer">
    ///     The comparer to use when getting translations.
    /// </param>
    /// <param name="parser">
    ///     An instance of Parser to change the default parsing settings for this command.
    /// </param>
    protected RocketCommandWithParsing(bool multiThreaded, Dictionary<string, string> translations,
        StringComparer comparer, Parser? parser = null) : base(multiThreaded, translations, comparer)
    {
        CommandParser = parser ?? new Parser(s =>
        {
            s.AutoHelp = false;
            s.AutoVersion = false;
            s.CaseSensitive = false;
            s.HelpWriter = null;
            s.ParsingCulture = CultureInfo.InvariantCulture;
            s.IgnoreUnknownArguments = true;
            s.CaseInsensitiveEnumValues = true;
        });
    }

    /// <inheritdoc />
    /// <summary>
    ///     The required constructor for this class.
    ///     Sets if the command will be multi-threaded or not, as well as the currently loaded translations, and offers
    ///     an optional setting for a custom parser.
    /// </summary>
    /// <param name="multiThreaded">
    ///     True if you want the command to always run on a separate thread.
    ///     False otherwise.
    /// </param>
    /// <param name="translations">
    ///     The currently loaded translations.
    /// </param>
    /// <param name="parser">
    ///     An instance of Parser to change the default parsing settings for this command.
    /// </param>
    protected RocketCommandWithParsing(bool multiThreaded, Dictionary<string, string> translations,
        Parser? parser = null) : base(multiThreaded, translations)
    {
        CommandParser = parser ?? new Parser(s =>
        {
            s.AutoHelp = false;
            s.AutoVersion = false;
            s.CaseSensitive = false;
            s.HelpWriter = null;
            s.ParsingCulture = CultureInfo.InvariantCulture;
            s.IgnoreUnknownArguments = true;
            s.CaseInsensitiveEnumValues = true;
        });
    }

    /// <inheritdoc />
    /// <summary>
    ///     The required constructor for this class.
    ///     Sets if the command will be multi-threaded or not, as well as a comparer for the translations dictionary,
    ///     and offers an optional setting for a custom parser.
    /// </summary>
    /// <param name="multiThreaded">
    ///     True if you want the command to always run on a separate thread.
    ///     False otherwise.
    /// </param>
    /// <param name="comparer">
    ///     The comparer to use when getting translations.
    /// </param>
    /// <param name="parser">
    ///     An instance of Parser to change the default parsing settings for this command.
    /// </param>
    protected RocketCommandWithParsing(bool multiThreaded, StringComparer comparer, Parser? parser = null) : base(
        multiThreaded, comparer)
    {
        CommandParser = parser ?? new Parser(s =>
        {
            s.AutoHelp = false;
            s.AutoVersion = false;
            s.CaseSensitive = false;
            s.HelpWriter = null;
            s.ParsingCulture = CultureInfo.InvariantCulture;
            s.IgnoreUnknownArguments = true;
            s.CaseInsensitiveEnumValues = true;
        });
    }

    /// <inheritdoc />
    /// <summary>
    ///     Overriden method to add full parsing support, including default help messages if parsing fails.
    /// </summary>
    public override async Task ExecuteAsync(IRocketPlayer caller, string[] command)
    {
        var result = CommandParser.ParseArguments<T>(command);
        switch (result)
        {
            case Parsed<T> parsed:
                if (parsed.Value.Help)
                {
                    await DisplayHelp(caller, parsed);
                    break;
                }

                await ExecuteAsync(caller, parsed.Value);
                break;
            default:
                await DisplayHelp(caller, result);
                break;
        }
    }

    /// <summary>
    ///     A method to display a help message in case of parsing fail, or if the user inputted -h
    /// </summary>
    /// <param name="caller">The user that executed the command.</param>
    /// <param name="parserResult">
    ///     The parsing result. With this you can check if it failed or if it got parsed successfully for different messages.
    /// </param>
    /// <returns>
    ///     A Task for async support.
    /// </returns>
    /// <remarks>
    ///     This method will provide 2 different types of help, a custom constructed one for console, and another one for
    ///     players.
    ///     The player one requires a translation with key "command_usage", which has 2 parameters, the name and the syntax of
    ///     the command.
    ///     If you wish to change the functionality or the message that the console receives, simply override this method.
    /// </remarks>
    [UsedImplicitly]
    protected virtual Task DisplayHelp(IRocketPlayer caller, ParserResult<T> parserResult)
    {
        if (caller is not ConsolePlayer)
        {
            SendTranslatedMessage(caller, "command_usage", Name, string.Join(" ", Syntax));
            return Task.CompletedTask;
        }

        var helpText = new CommandHelpText(this);
        helpText.AddParsingInformation(parserResult);

        SendMessage(caller, helpText.ToString());
        return Task.CompletedTask;
    }

    /// <summary>
    ///     An abstract method to override with the final command's implementation.
    /// </summary>
    /// <param name="caller">The user that executed the command.</param>
    /// <param name="parsed">The parsed input from the user, if parsing succeeded.</param>
    /// <returns>A Task that describes the current method's execution.</returns>
    /// <remarks>
    ///     In order to allow commands to use the async and await keywords, the method returns Task by default.
    ///     If you do not wish to use the async keyword, please return Task.CompletedTask.
    ///     If parsing fails, the method will not execute.
    /// </remarks>
    [UsedImplicitly]
    public abstract Task ExecuteAsync(IRocketPlayer caller, T parsed);
}