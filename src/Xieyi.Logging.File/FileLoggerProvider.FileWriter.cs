namespace Xieyi.Logging.File;

public partial class FileLoggerProvider
{
    internal class FileWriter
    {
        private readonly FileLoggerProvider _fileLoggerProvider;
        
        private string _logFileName;
        private FileStream _logFileStream;
        private StreamWriter _logFileWriter;
        
        //缓存上一次的LogFileName，用于检测LogFileName是否变化
        private string _lastBaseLogFileName;
        
        internal FileWriter(FileLoggerProvider fileLoggerProvider)
        {
            _fileLoggerProvider = fileLoggerProvider;
            //确定写入文件
            ConfirmLogFileName();
        }

        /// <summary>
        /// 获取LogFile文件名（定义在Options中的基础LogName）
        /// </summary>
        /// <returns></returns>
        private string GetBaseLogFileName()
        {
            var fileName = _fileLoggerProvider.LogFileName;
            
            if (_fileLoggerProvider.FormatLogFileName != null)
                fileName = _fileLoggerProvider.FormatLogFileName(fileName);

            return fileName;
        }

        /// <summary>
        /// 设置最终logger写入的文件名
        /// </summary>
        /// <remarks>
        /// 若没有产生日志文件或没有限制FileLimitBytes时，不会产生新文件也不会有MaxRollingFile的概念，那么fileName其实都是baseLogFileName。
        /// 若限制了FileLimitBytes，则需要确定修改时间最新的日志文件继续追加写入
        /// </remarks>
        private void ConfirmLogFileName()
        {
            var baseLogFileName = GetBaseLogFileName();
            _lastBaseLogFileName = baseLogFileName;

            if (_fileLoggerProvider.FileLimitBytes > 0)
            {
                var logFileMask = Path.GetFileNameWithoutExtension(baseLogFileName) + "*" + Path.GetExtension(baseLogFileName);
                var logDirName = Path.GetDirectoryName(baseLogFileName);
                if (string.IsNullOrEmpty(logDirName))
                    logDirName = Directory.GetCurrentDirectory();

                var logFiles = Directory.Exists(logDirName) ? Directory.GetFiles(logDirName, logFileMask, SearchOption.TopDirectoryOnly) : Array.Empty<string>();
                if (logFiles.Any())
                {
                    var lastFileInfo = logFiles
                        .Select(fName => new FileInfo(fName))
                        .OrderByDescending(info => info.Name)
                        .ThenByDescending(info => info.LastWriteTime).First();
                    _logFileName = lastFileInfo.Name;
                }
                else
                {
                    //还没有产生任何日志文件，就使用默认文件即可
                    _logFileName = baseLogFileName;
                }
            }
            else
            {
                _logFileName = baseLogFileName;
            }
        }
        
        /// <summary>
        /// 获取下一个可写入的日志文件名
        /// </summary>
        /// <returns></returns>
        private string GetNextFileLogName()
        {
            var baseLogFileName = GetBaseLogFileName();
            
            if (checkIsNeedCreateNewFile(baseLogFileName))
                return baseLogFileName;

            var currentFileIndex = 0;
            var baseFileNameOnly = Path.GetFileNameWithoutExtension(baseLogFileName);
            var currentFileNameOnly = Path.GetFileNameWithoutExtension(_logFileName);

            //获取文件后缀
            var suffix = currentFileNameOnly?.Substring(baseFileNameOnly.Length);
            if (suffix?.Length > 0 && int.TryParse(suffix, out var parseIndex))
                currentFileIndex = parseIndex;

            var nextFileIndex = currentFileIndex + 1;
            if (_fileLoggerProvider.MaxRollingFiles > 0)
            {
                nextFileIndex %= _fileLoggerProvider.MaxRollingFiles;
            }

            var nextFileName = baseFileNameOnly + (nextFileIndex > 0 ? nextFileIndex.ToString() : "") + Path.GetExtension(baseLogFileName);
            return Path.Combine(Path.GetDirectoryName(baseLogFileName) ?? "", nextFileName);
            
            bool checkIsNeedCreateNewFile(string logFileName)
            {
                return !System.IO.File.Exists(logFileName) || _fileLoggerProvider.FileLimitBytes <= 0 || new System.IO.FileInfo(logFileName).Length <= _fileLoggerProvider.FileLimitBytes;
            }
        }

        /// <summary>
        /// 检测是否需要新建日志文件
        /// </summary>
        private void CheckForNewLogFile()
        {
            var openNewFile = isMaxSizeThresholdReached() || isBaseFileNameChanged();

            if (openNewFile)
            {
                Close();
                _logFileName = GetNextFileLogName();
                OpenFile(FileWriteOption.Override);
            }
            return;

            bool isMaxSizeThresholdReached()
            {
                return _fileLoggerProvider.FileLimitBytes > 0 && _logFileStream.Length >= _fileLoggerProvider.FileLimitBytes;
            }

            bool isBaseFileNameChanged()
            {
                if (_fileLoggerProvider.FormatLogFileName == null) return false;

                var baseLogFileName = GetBaseLogFileName();
                if (baseLogFileName == _lastBaseLogFileName) return false;

                _lastBaseLogFileName = baseLogFileName;
                return true;
            }
        }
        
        /// <summary>
        /// 根据不同的文件写入方式来设定StreamWriter
        /// </summary>
        /// <param name="writeOption">Append or Override</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private void CreateLogFileStream(FileWriteOption writeOption)
        {
            var fileInfo = new FileInfo(_logFileName);
            if (fileInfo.Directory is null) return;
            
            fileInfo.Directory.Create();
            _logFileStream = new FileStream(_logFileName, FileMode.OpenOrCreate, FileAccess.Write);

            switch (writeOption)
            {
                case FileWriteOption.Append:
                    _logFileStream.Seek(0, SeekOrigin.End); //将文件原有内容暂存进文件流
                    break;
                case FileWriteOption.Override:
                    _logFileStream.SetLength(0); // 清除文件内容
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(writeOption), writeOption, null);
            }

            _logFileWriter = new StreamWriter(_logFileStream);
        }
        
        /// <summary>
        /// 使用新的LogFile进行写入
        /// </summary>
        /// <param name="newLogFileName"></param>
        internal void UseNewLogFileAndSetFileStream(string newLogFileName)
        {
            _fileLoggerProvider.LogFileName = newLogFileName;
            ConfirmLogFileName();
            CreateLogFileStream(_fileLoggerProvider.FileWriteOption);
        }
        
        /// <summary>
        /// 打开_logFileName
        /// </summary>
        /// <param name="writeOption"></param>
        internal void OpenFile(FileWriteOption writeOption)
        {
            try
            {
                CreateLogFileStream(writeOption);
            }
            catch (Exception ex)
            {
                if (_fileLoggerProvider.HandleFileError != null)
                {
                    var fileError = new LoggerFileError(_logFileName, ex);
                    _fileLoggerProvider.HandleFileError(fileError);
                    
                    if (fileError.NewLogFileName != null)
                        UseNewLogFileAndSetFileStream(fileError.NewLogFileName);
                }
                else
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// 写入日志信息
        /// </summary>
        /// <param name="message"></param>
        /// <param name="flush"></param>
        internal void WriteMessage(string message, bool flush)
        {
            if (_logFileWriter != null)
            {
                CheckForNewLogFile();
                _logFileWriter.WriteLine(message);
                
                if (flush)
                    _logFileWriter.Flush();
            }
        }
        
        /// <summary>
        /// 清空Writer并关闭Stream
        /// </summary>
        internal void Close()
        {
            _logFileWriter?.Dispose();
            _logFileStream?.Dispose();
        }
    }
}