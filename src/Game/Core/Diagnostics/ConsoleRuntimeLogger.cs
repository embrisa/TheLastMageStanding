using System;
using System.IO;

namespace TheLastMageStanding.Game.Core.Diagnostics;

internal sealed class ConsoleRuntimeLogger : IRuntimeLogger
{
    private readonly TextWriter _writer;
    private readonly Func<DateTimeOffset> _timeProvider;
    private readonly RuntimeLogSettings _settings;

    public ConsoleRuntimeLogger(
        RuntimeLogSettings? settings = null,
        TextWriter? writer = null,
        Func<DateTimeOffset>? timeProvider = null)
    {
        _settings = settings ?? new RuntimeLogSettings();
        _writer = writer ?? Console.Out;
        _timeProvider = timeProvider ?? (() => DateTimeOffset.Now);
    }

    public bool IsEnabled(string category, RuntimeLogLevel level)
    {
        return level >= _settings.MinimumLevel && _settings.IsCategoryEnabled(category);
    }

    public void Log(RuntimeLogLevel level, string category, string message, Exception? exception = null)
    {
        if (!IsEnabled(category, level))
        {
            return;
        }

        var timestamp = _timeProvider().ToString("O", System.Globalization.CultureInfo.InvariantCulture);
        _writer.WriteLine($"[{timestamp}] [{level}] [{category}] {message}");
        if (exception != null)
        {
            _writer.WriteLine(exception);
        }
    }
}
