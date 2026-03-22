using System;

namespace TheLastMageStanding.Game.Core.Diagnostics;

internal interface IRuntimeLogger
{
    bool IsEnabled(string category, RuntimeLogLevel level);

    void Log(RuntimeLogLevel level, string category, string message, Exception? exception = null);
}
