namespace Xieyi.Logging.File;

public class Logger : ILogger
{
    private readonly string _logName;
    private readonly FileLoggerProvider _fileLoggerProvider;

    public Logger(string logName, FileLoggerProvider fileLoggerProvider)
    {
        _logName = logName;
        _fileLoggerProvider = fileLoggerProvider;
    }
    
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;

        if (formatter == null)
            throw new ArgumentNullException(nameof(formatter));

        var message = formatter(state, exception);

        //自定义LogFilter
        if (_fileLoggerProvider.Options.FilterLogEntry != null && !_fileLoggerProvider.Options.FilterLogEntry(new LogMessage(_logName, logLevel, eventId, message, exception)))
            return;

        if (_fileLoggerProvider.Options.FormatLogEntry != null)
        {
            _fileLoggerProvider.WriteMessage(_fileLoggerProvider.FormatLogEntry((new LogMessage(_logName, logLevel, eventId, message, exception))));
        }
        else
        {
            var logBuilder = new StringBuilder();
            if (!string.IsNullOrEmpty(message))
            {
                var timeStamp = _fileLoggerProvider.UseUtcTimestamp ? DateTime.UtcNow : DateTime.Now;

                logBuilder.Append(timeStamp.ToString("O"));
                logBuilder.Append('\t');
                logBuilder.Append(GetShortLogLevel(logLevel));
                logBuilder.Append("\t[");
                logBuilder.Append(_logName);
                logBuilder.Append(']');
                logBuilder.Append("\t[");
                logBuilder.Append(eventId);
                logBuilder.Append("]\t");
                logBuilder.Append(message);
            }

            if (exception != null)
            {
                logBuilder.AppendLine(exception.ToString());
            }

            _fileLoggerProvider.WriteMessage(logBuilder.ToString());
        }
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel >= _fileLoggerProvider.MinLevel;
    }

    public IDisposable BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }
    
    private string GetShortLogLevel(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Information => "info",
            LogLevel.Warning => "warn",
            LogLevel.Critical => "crit",
            _ => logLevel.ToString().ToUpper()
        };
    }
}