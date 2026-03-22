using System;

namespace TheLastMageStanding.Game.Core.Diagnostics;

internal static class RuntimeLog
{
    private static IRuntimeLogger _current = new ConsoleRuntimeLogger();

    public static IRuntimeLogger Current => _current;

    public static void Configure(IRuntimeLogger logger)
    {
        _current = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public static void ResetToDefaults()
    {
        _current = new ConsoleRuntimeLogger();
    }

    public static bool IsEnabled(string category, RuntimeLogLevel level)
    {
        return _current.IsEnabled(category, level);
    }

    public static void Debug(string category, string message)
    {
        _current.Log(RuntimeLogLevel.Debug, category, message);
    }

    public static void Info(string category, string message)
    {
        _current.Log(RuntimeLogLevel.Info, category, message);
    }

    public static void Warning(string category, string message)
    {
        _current.Log(RuntimeLogLevel.Warning, category, message);
    }

    public static void Error(string category, string message, Exception? exception = null)
    {
        _current.Log(RuntimeLogLevel.Error, category, message, exception);
    }
}
