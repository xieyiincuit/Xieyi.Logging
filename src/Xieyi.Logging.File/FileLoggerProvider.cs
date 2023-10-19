using System.Collections.Concurrent;

namespace Xieyi.Logging.File;

[ProviderAlias("File")]
public partial class FileLoggerProvider : ILoggerProvider
{
    private string LogFileName;
    
    /// <summary>
    /// 不同类型的Logger
    /// </summary>
    private readonly ConcurrentDictionary<string, Logger> loggers = new ConcurrentDictionary<string, Logger>();
    
    /// <summary>
    /// 日志信息处理队列
    /// </summary>
    private readonly BlockingCollection<string> entryQueue = new BlockingCollection<string>(1024);
    
    /// <summary>
    /// 后台线程写入日志
    /// </summary>
    private readonly Task processQueueTask;
    
    /// <summary>
    /// 日志文件写入
    /// </summary>
    private readonly FileWriter fWriter;
    
    #region Options Proprety

    internal LoggerOptions Options { get; private set; }

    private FileWriteOption FileWriteOption => Options.FileWriteOption;
    private long FileLimitBytes => Options.FileLimitBytes;
    private int MaxRollingFiles => Options.MaxRollingFiles;

    public LogLevel MinLevel
    {
        get => Options.MinLevel;
        set => Options.MinLevel = value;
    }

    public bool UseUtcTimestamp
    {
        get => Options.UseUtcTimestamp;
        set => Options.UseUtcTimestamp = value;
    }

    public Func<LogMessage, string> FormatLogEntry
    {
        get => Options.FormatLogEntry;
        set => Options.FormatLogEntry = value;
    }

    public Func<string, string> FormatLogFileName
    {
        get => Options.FormatLogFileName;
        set => Options.FormatLogFileName = value;
    }

    public Action<LoggerFileError> HandleFileError
    {
        get => Options.HandleFileError;
        set => Options.HandleFileError = value;
    }

    #endregion

    public FileLoggerProvider(string fileName, FileWriteOption writeOption = FileWriteOption.Append) 
        : this(fileName, new LoggerOptions() { FileWriteOption = writeOption })
    {
    }
    
    public FileLoggerProvider(string fileName, LoggerOptions options)
    {
        Options = options;
        LogFileName = Environment.ExpandEnvironmentVariables(fileName);

        fWriter = new FileWriter(this);
        processQueueTask = Task.Factory.StartNew(ProcessQueue, this, TaskCreationOptions.LongRunning);
    }

    public ILogger CreateLogger(string categoryName)
    {
        return loggers.GetOrAdd(categoryName, CreateLoggerImplementation);
        
        Logger CreateLoggerImplementation(string name)
        {
            return new Logger(name, this);
        }
    }
    
    public void Dispose()
    {
        entryQueue.CompleteAdding();
        try
        {
            //尽量保证当前任务线程完成日志的记录
            processQueueTask.Wait(2000);
        }
        catch (TaskCanceledException) { }
        catch (AggregateException ex) when (ex.InnerExceptions.Count == 1 && ex.InnerExceptions.First() is TaskCanceledException) { }

        loggers.Clear();
        fWriter.Close();
    }
    
    internal void WriteMessage(string message)
    {
        if (entryQueue.IsAddingCompleted) return;
        try
        {
            entryQueue.Add(message);
            return;
        }
        catch (InvalidOperationException)
        {
        }
    }

    private static void ProcessQueue(object state)
    {
        var fileLogger = (FileLoggerProvider)state;
        fileLogger.ProcessQueue();
    }

    private void ProcessQueue()
    {
        var writeMessageFailed = false;
        foreach (var message in entryQueue.GetConsumingEnumerable())
        {
            try
            {
                if (!writeMessageFailed)
                    fWriter.WriteMessage(message, flush: entryQueue.Count == 0);
            }
            catch (Exception ex)
            {
                var stopLogging = true;
                if (HandleFileError != null)
                {
                    var fileError = new LoggerFileError(LogFileName, ex);
                    try
                    {
                        HandleFileError(fileError);
                        if (fileError.NewLogFileName != null)
                        {
                            fWriter.UseNewLogFileAndSetFileStream(fileError.NewLogFileName);
                            fWriter.WriteMessage(message, entryQueue.Count == 0);
                            stopLogging = false;
                        }
                    }
                    catch
                    {
                        //可能在某些情况下，写入错误日志信息的错误文件不可用，这个时候则忽略此错误，并停止写入日志。
                    }
                }

                if (stopLogging)
                {
                    //因为无法写入日志文件，那么停止处理日志消息
                    entryQueue.CompleteAdding();
                    writeMessageFailed = true;
                }
            }
        }
    }
}