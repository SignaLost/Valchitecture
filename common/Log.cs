using System.Linq;
using BepInEx.Configuration;
using BepInEx.Logging;

namespace Valchitecture.Common;

/// <summary>Logging with configurable verbosity for info and warning output.</summary>
internal static class Log
{
    internal enum InfoLevel
    {
        Low,
        Medium,
        High
    }

    private static ManualLogSource _logger;

    internal static ConfigEntry<InfoLevel> Verbosity { get; set; }

    private static InfoLevel VerbosityLevel => Verbosity?.Value ?? InfoLevel.Low;

    internal static void Init(ManualLogSource logger)
    {
        _logger = logger;
    }

    /// <summary>Writes to LogOutput.log only, bypassing console listeners.</summary>
    internal static void ToFile(string data)
    {
        DiskLogListener disk = Logger.Listeners.OfType<DiskLogListener>().FirstOrDefault();

        if (disk == null)
        {
            _logger?.LogInfo(data);
            return;
        }

        disk.LogEvent(null, new LogEventArgs(data, LogLevel.Info, _logger));
    }

    /// <summary>Whether a message at this level would be written.</summary>
    internal static bool Shows(InfoLevel level)
    {
        return level <= VerbosityLevel;
    }

    internal static void LogInfo(object data, InfoLevel level = InfoLevel.Low)
    {
        if (level <= VerbosityLevel)
        {
            _logger?.LogInfo(data);
        }
    }

    internal static void LogWarning(object data, InfoLevel level = InfoLevel.Low)
    {
        if (level <= VerbosityLevel)
        {
            _logger?.LogWarning(data);
        }
    }

    internal static void LogError(object data)
    {
        _logger?.LogError(data);
    }
}
