namespace Xieyi.Logging.File;

/// <summary>
/// 日志生成即时设置
/// </summary>
public class LoggerOptions
{
    /// <summary>
    /// 日志文件生成路径
    /// </summary>
    public string FilePath { get; set; } = null;
    
    /// <summary>
    /// 文件写入方式（追加 or 覆盖）
    /// </summary>
    public FileWriteOption FileWriteOption { get; set; } = FileWriteOption.Append;

    /// <summary>
    /// 日志文件的最大字节数
    /// </summary>
    /// <remarks>
    /// 设定日志文件的大小限制，如果日志信息数量接近最大限制。则会新增日志文件来承载日志信息。
    /// 比如你创建了一个日志文件“test1.log”，logger将会创建新的“test2.log”来继续写入
    /// </remarks>
    public long FileLimitBytes { get; set; } = 0;
    
    /// <summary>
    /// 决定有多少log文件可以适用于滚动覆盖写入
    /// </summary>
    /// <remarks>
    /// 如果该值被指定，那么则会覆盖之前创建并有日志信息写入的日志文件
    /// 比如你的logFile名为‘test.log’且MaxRollingFiles设置为3，logger将会使用‘test.log’,‘test1.log’,‘test2.log’，若文件都已经达到写入限制，则最新内容会覆盖‘test.log’的内容
    /// </remarks>
    public int MaxRollingFiles { get; set; } = 0;

    /// <summary>
    /// 决定日志文件中的 timestamps 是否使用 UTC Zone. 默认不使用。
    /// </summary>
    public bool UseUtcTimestamp { get; set; }
    
    /// <summary>
    /// 自定义日志行信息的格式
    /// </summary>
    public Func<LogMessage, string> FormatLogEntry { get; set; }

    /// <summary>
    /// 自定义日志类型过滤Filter
    /// </summary>
    public Func<LogMessage, bool> FilterLogEntry { get; set; }
    
    /// <summary>
    /// File logger 的最低日志等级
    /// </summary>
    public LogLevel MinLevel { get; set; } = LogLevel.Trace;
    
    /// <summary>
    /// Custom formatter for the log file name.
    /// </summary>
    /// <remarks>By specifying custom formatting handler you can define your own criteria for creation of log files. Note that this handler is called
    /// on EVERY log message 'write'; you may cache the log file name calculation in your handler to avoid any potential overhead in case of high-load logger usage.
    /// For example:
    /// </remarks>
    /// <example>
    /// fileLoggerOpts.FormatLogFileName = (fname) => {
    /// return String.Format(Path.GetFileNameWithoutExtension(fname) + "_{0:yyyy}-{0:MM}-{0:dd}" + Path.GetExtension(fname), DateTime.UtcNow); 
    /// };
    /// </example>
    public Func<string, string> FormatLogFileName { get; set; }
    
    /// <summary>
    /// Custom handler for log file errors.
    /// </summary>
    /// <remarks>If this handler is provided file open exception (on <code>FileLoggerProvider</code> creation) will be suppressed.
    /// You can handle file error exception according to your app's logic, and propose an alternative log file name (if you want to keep file logger working).
    /// </remarks>
    /// <example>
    /// fileLoggerOpts.HandleFileError = (err) => {
    ///   err.UseNewLogFileName( Path.GetFileNameWithoutExtension(err.LogFileName)+ "_alt" + Path.GetExtension(err.LogFileName) );
    /// };
    /// </example>
    public Action<FileLoggerProvider.LoggerFileError> HandleFileError { get; set; }
}