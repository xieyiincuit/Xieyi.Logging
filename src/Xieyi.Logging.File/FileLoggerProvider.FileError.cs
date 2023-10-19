namespace Xieyi.Logging.File;

public partial class FileLoggerProvider
{
    public class LoggerFileError
    {
        public Exception ErrorException { get; private set; }

        /// <summary>
        /// 当前正在记录日志的文件名
        /// </summary>
        public string LogFileName { get; private set; }

        internal LoggerFileError(string logFileName, Exception ex)
        {
            LogFileName = logFileName;
            ErrorException = ex;
        }

        internal string NewLogFileName { get; private set; }

        /// <summary>
        /// 使用新的日志文件来记录错误日志 
        /// </summary>
        /// <remarks>
        /// 需要注意的是当我们使用的新的文件进行错误日志写入时若仍然出现错误，那么该错误是无法被记录的。因为这样会陷入递归的场景。
        /// </remarks>
        public void UseNewLogFileName(string newLogFileName)
        {
            NewLogFileName = newLogFileName;
        }
    }
}