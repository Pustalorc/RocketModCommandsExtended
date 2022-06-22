using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using CommandLine;
using JetBrains.Annotations;
using Pustalorc.Libraries.RocketModCommandsExtended.CommandParsing;
using Rocket.API;

namespace Pustalorc.Libraries.RocketModCommandsExtended.Abstractions;

public abstract class RocketCommandWithParsing<T> : RocketCommandWithTranslations
    where T : CommandParsing.CommandParsing
{
    [UsedImplicitly] protected Parser CommandParser { get; }

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

    [UsedImplicitly]
    protected virtual Task DisplayHelp(IRocketPlayer caller, ParserResult<T> parserResult)
    {
        var helpText = new CommandHelpText(this);
        helpText.AddParsingInformation(parserResult);

        SendMessage(caller, helpText.ToString());
        return Task.CompletedTask;
    }

    [UsedImplicitly]
    public abstract Task ExecuteAsync(IRocketPlayer caller, T parsed);
}