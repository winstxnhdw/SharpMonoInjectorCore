using System.CommandLine;

#nullable enable

namespace SharpMonoInjector;
public static class OptionHelper {
    public static Option<T> CreateOption<T>(string name, string alias, string? description = null, T? defaultValue = default) {
        Option<T> option = new Option<T>(
            name: name,
            description: description
        );

        option.AddAlias(alias);
        option.SetDefaultValue(defaultValue);

        return option;
    }
}