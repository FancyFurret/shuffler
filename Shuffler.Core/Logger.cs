namespace Shuffler.Core;

public enum LogLevel
{
    Debug,
    Info,
    Warning,
    Error
}

public record LogEntry(
    string Message,
    LogLevel Level,
    DateTime Timestamp,
    string Source
)
{
    public LogEntry(string message, LogLevel level, string source) : this(message, level, DateTime.UtcNow, source)
    {
    }
}

public interface ILogger
{
    void Debug(string message);
    void Info(string message);
    void Warning(string message);
    void Error(string message);
}

public static class LogManager
{
    private static readonly List<LogEntry> LogEntries = [];
    private static readonly List<ILogger> Loggers = [];

    public static IReadOnlyList<LogEntry> Entries => LogEntries;
    public static event Action<LogEntry>? OnLog;

    public static ILogger Create(string name)
    {
        var logger = new Logger(name, entry =>
        {
            LogEntries.Add(entry);
            OnLog?.Invoke(entry);
        });
        Loggers.Add(logger);
        return logger;
    }
}

public class Logger : ILogger
{
    private readonly string _name;
    private readonly Action<LogEntry> _logAction;

    public Logger(string name, Action<LogEntry> logAction)
    {
        _name = name;
        _logAction = logAction;
    }

    private void Log(LogEntry entry)
    {
        _logAction(entry);
    }

    public void Debug(string message) => Log(new LogEntry(message, LogLevel.Debug, _name));
    public void Info(string message) => Log(new LogEntry(message, LogLevel.Info, _name));
    public void Warning(string message) => Log(new LogEntry(message, LogLevel.Warning, _name));
    public void Error(string message) => Log(new LogEntry(message, LogLevel.Error, _name));
}