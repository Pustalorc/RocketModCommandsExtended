using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CommandLine;
using JetBrains.Annotations;
using Rocket.API;

namespace Pustalorc.Libraries.RocketModCommandsExtended.Abstractions.WithParsing;

/// <summary>
/// A basic (and mostly copied from the command parsing lib) implementation of a full custom help message.
/// </summary>
/// <remarks>
/// This class should be reworked to be translatable, but also not have to re-generate the full help message every time.
/// </remarks>
[UsedImplicitly]
public class CommandHelpText
{
#pragma warning disable CS1591
    protected List<string> HelpText { get; }

    public CommandHelpText(IRocketCommand command)
    {
        HelpText = new List<string>
        {
            $"Command: {command.Name}",
            $"{command.Help}",
            "",
            $"Aliases: {string.Join(", ", command.Aliases)}",
            $"Useable by: {command.AllowedCaller}",
            $"Permissions: {string.Join(", ", command.Permissions)}",
            $"Command usage: {command.Name} {command.Syntax}"
        };
    }

    public virtual void AddParsingInformation<T>(ParserResult<T> parserResult) where T : CommandParsing
    {
        HelpText.Add("Parameters:");
        HelpText.AddRange(GetSpecifications(typeof(T), GetSpecification));

        if (parserResult is NotParsed<T> notParsed)
            AddParsingError(notParsed);
    }

    protected virtual IEnumerable<T> GetSpecifications<T>(Type type, Func<PropertyInfo, IEnumerable<T>> selector)
    {
        var options = new List<PropertyInfo>();
        var values = new List<PropertyInfo>();

        foreach (var property in type.GetTypeInfo().GetProperties())
        {
            if (property.GetCustomAttribute<OptionAttribute>(true) != null)
            {
                options.Add(property);
                continue;
            }

            if (property.GetCustomAttribute<ValueAttribute>() != null)
                values.Add(property);
        }

        return options.SelectMany(selector).Concat(values.SelectMany(selector));
    }


    [UsedImplicitly]
    public virtual void AddParsingError<T>(NotParsed<T> notParsed) where T : CommandParsing
    {
        HelpText.Add("Errors during parsing:");
        HelpText.AddRange(notParsed.Errors
            .Where(error => error.Tag is not (ErrorType.HelpRequestedError or ErrorType.HelpVerbRequestedError
                or ErrorType.VersionRequestedError)).Select(k => k.ToString()));
    }

    protected virtual IEnumerable<string> GetSpecification(PropertyInfo property)
    {
        var specification = new List<string>();
        var optionAttribute = property.GetCustomAttribute<OptionAttribute>(true);
        var valueAttribute = property.GetCustomAttribute<ValueAttribute>(true);
        var propUnderlying = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

        if (optionAttribute is { Hidden: false })
        {
            var cmdSpec = "    ";
            if (!string.IsNullOrWhiteSpace(optionAttribute.ShortName))
                cmdSpec += $"-{optionAttribute.ShortName}";

            if (!string.IsNullOrWhiteSpace(optionAttribute.LongName))
            {
                var longSpec = $"--{optionAttribute.LongName}";

                if (string.IsNullOrWhiteSpace(cmdSpec))
                    cmdSpec += longSpec;
                else
                    cmdSpec += $", {longSpec}";
            }

            if (string.IsNullOrWhiteSpace(cmdSpec))
                cmdSpec += $"--{property.Name}";

            specification.Add(cmdSpec);
            specification.Add($"        {optionAttribute.HelpText}");

            if (propUnderlying.IsEnum)
                specification.Add(
                    $"        Possible values: {string.Join(", ", Enum.GetNames(propUnderlying).Select((k, i) => $"{k} ({i})"))}");

            specification.Add("");
        }
        else if (valueAttribute is { Hidden: false })
        {
            var cmdSpec = $"    {property.Name} pos. {valueAttribute.Index + 1}";
            if (valueAttribute.Max > 1)
                cmdSpec += $" - {valueAttribute.Max + valueAttribute.Index}";

            specification.Add(cmdSpec);
            specification.Add($"        {valueAttribute.HelpText}");

            if (propUnderlying.IsEnum)
                specification.Add(
                    $"        Possible values: {string.Join(", ", Enum.GetNames(propUnderlying).Select((k, i) => $"{k} ({i})"))}");

            specification.Add("");
        }

        return specification;
    }

    public override string ToString()
    {
        return string.Join("\n", HelpText);
    }
}