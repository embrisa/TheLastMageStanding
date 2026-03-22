using System;
using System.Collections.Generic;

namespace TheLastMageStanding.Game.Core.Diagnostics;

internal sealed class RuntimeLogSettings
{
    private const string LogLevelEnvironmentVariable = "TLS_RUNTIME_LOG_LEVEL";
    private const string LogCategoriesEnvironmentVariable = "TLS_RUNTIME_LOG_CATEGORIES";

    public RuntimeLogLevel MinimumLevel { get; init; } = RuntimeLogLevel.Info;

    public ISet<string>? EnabledCategories { get; init; }

    public bool IsCategoryEnabled(string category)
    {
        return EnabledCategories == null || EnabledCategories.Contains(category);
    }

    public static RuntimeLogSettings FromEnvironment()
    {
        var minimumLevel = RuntimeLogLevel.Info;
        var configuredLevel = Environment.GetEnvironmentVariable(LogLevelEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(configuredLevel) &&
            Enum.TryParse(configuredLevel, ignoreCase: true, out RuntimeLogLevel parsedLevel))
        {
            minimumLevel = parsedLevel;
        }

        var configuredCategories = Environment.GetEnvironmentVariable(LogCategoriesEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(configuredCategories))
        {
            return new RuntimeLogSettings
            {
                MinimumLevel = minimumLevel
            };
        }

        var enabledCategories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var rawCategory in configuredCategories.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            enabledCategories.Add(rawCategory);
        }

        return new RuntimeLogSettings
        {
            MinimumLevel = minimumLevel,
            EnabledCategories = enabledCategories
        };
    }
}
