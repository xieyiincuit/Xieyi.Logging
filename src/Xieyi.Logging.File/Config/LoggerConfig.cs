namespace Xieyi.Logging.File;

/// <summary>
/// 生成日志时的相关配置
/// </summary>
public class LoggerConfig
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
    /// File logger 的最低日志等级
    /// </summary>
    public LogLevel MinLevel { get; set; } = LogLevel.Trace;
}