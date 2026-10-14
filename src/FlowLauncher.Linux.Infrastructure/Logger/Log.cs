using System.IO;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace FlowLauncher.Linux.Infrastructure.Logger;

public static class Log
{
    private static readonly NLog.Logger _logger = LogManager.GetCurrentClassLogger();

    public static void Init(string logDirectory)
    {
        Directory.CreateDirectory(logDirectory);
        var config = new LoggingConfiguration();
        var fileTarget = new FileTarget("file")
        {
            FileName = Path.Combine(logDirectory, "flow-launcher.log"),
            Layout = "${longdate}|${level:uppercase=true}|${logger}|${message}${onexception:inner=${newline}${exception:format=tostring}}",
            ArchiveEvery = FileArchivePeriod.Day,
            MaxArchiveFiles = 7
        };
        var consoleTarget = new ConsoleTarget("console")
        {
            Layout = "${level:uppercase=true}|${logger}|${message}"
        };
        config.AddTarget(fileTarget);
        config.AddTarget(consoleTarget);
        config.AddRule(LogLevel.Debug, LogLevel.Fatal, fileTarget);
        config.AddRule(LogLevel.Info, LogLevel.Fatal, consoleTarget);
        LogManager.Configuration = config;
    }

    public static void Error(string className, string message, Exception? ex = null)
    {
        if (ex != null) _logger.Error(ex, $"[{className}] {message}");
        else _logger.Error($"[{className}] {message}");
    }

    public static void Info(string className, string message) => _logger.Info($"[{className}] {message}");
    public static void Warn(string className, string message) => _logger.Warn($"[{className}] {message}");
    public static void Debug(string className, string message) => _logger.Debug($"[{className}] {message}");
}
