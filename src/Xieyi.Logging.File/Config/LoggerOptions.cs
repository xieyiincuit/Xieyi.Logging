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
    /// 日志文件自定义Handler
    /// </summary>
    /// <remarks>
    /// 指定具体的文件重命名逻辑，这个方法在LogProvider的每次Write方法前都会执行。使用此方法可以避免文件的覆盖和相同文件频繁被加载的问题。
    /// For example:
    /// </remarks>
    /// <example>
    /// fileLoggerOpts.FormatLogFileName = (fname) => {
    /// return String.Format(Path.GetFileNameWithoutExtension(fname) + "_{0:yyyy}-{0:MM}-{0:dd}" + Path.GetExtension(fname), DateTime.UtcNow); 
    /// };
    /// </example>
    public Func<string, string> FormatLogFileName { get; set; }
    
    /// <summary>
    /// 自定义日志错误处理逻辑
    /// </summary>
    /// <remarks>
    /// 可以自定义日志错误逻辑，当记录日志出现错误的时候，根据当前业务Scope做出正确的记录。在记录时可以使用一个备选文件进行日志的记录，使业务日志与日志记录异常具有隔离性。
    /// </remarks>
    /// <example>
    /// fileLoggerOpts.HandleFileError = (err) => {
    ///   err.UseNewLogFileName( Path.GetFileNameWithoutExtension(err.LogFileName)+ "_alt" + Path.GetExtension(err.LogFileName) );
    /// };
    /// </example>
    public Action<FileLoggerProvider.LoggerFileError> HandleFileError { get; set; }
}