using System.Runtime.InteropServices;
using System.Text;

namespace Xieyi.Logging.File.Test;

public class FileLoggerProviderTest
{
    [Fact]
    public void WriteToFileWithAppend_ShouldCreateFileAndSuccess()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            var logFactory = new LoggerFactory();
            logFactory.AddFileLogger(tempFile, FileWriteOption.Append);
            var logger = logFactory.CreateLogger("Append");

            logger.LogTrace("trace world");
            logger.LogInformation("info world");
            logger.LogDebug("debug world");
            logger.LogWarning("warn world");
            logger.LogError("error world");
            logger.LogCritical("critical world");
            logFactory.Dispose();

            var logEntries = System.IO.File.ReadAllLines(tempFile);
            Assert.Equal(6, logEntries.Length);

            var entry1Parts = logEntries[0].Split('\t');
            Assert.True(DateTime.Parse(entry1Parts[0]).Ticks <= DateTime.Now.Ticks);
            Assert.Equal("[TRACE]", entry1Parts[1]);
            Assert.Equal("[Append]", entry1Parts[2]);
            Assert.Equal("trace world", entry1Parts[4]);

            var entry2Parts = logEntries[1].Split('\t');
            Assert.True(DateTime.Parse(entry2Parts[0]).Ticks <= DateTime.Now.Ticks);
            Assert.Equal("[INFO]", entry2Parts[1]);
            Assert.Equal("[Append]", entry2Parts[2]);
            Assert.Equal("info world", entry2Parts[4]);

            logFactory = new LoggerFactory();
            logger = logFactory.CreateLogger("Append2");
            logFactory.AddProvider(new FileLoggerProvider(tempFile, FileWriteOption.Append));
            logger.LogInformation("Just message");
            logFactory.Dispose();

            var logEntries1 = System.IO.File.ReadAllLines(tempFile);
            Assert.Equal(7, System.IO.File.ReadAllLines(tempFile).Length);

            var entry7Parts = logEntries1[6].Split('\t');
            Assert.Equal("[INFO]", entry7Parts[1]);
            Assert.Equal("[Append2]", entry7Parts[2]);
            Assert.Equal("Just message", entry7Parts[4]);
        }
        finally
        {
            System.IO.File.Delete(tempFile);
        }
    }

    [Fact]
    public void WriteToFileWithOverride_OverrideContentInFile()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            var logFactory = new LoggerFactory();
            logFactory.AddFileLogger(tempFile, FileWriteOption.Override);
            var logger = logFactory.CreateLogger("Override1");
            logger.LogInformation("Override Line");
            logFactory.Dispose();

            var logEntries = System.IO.File.ReadAllLines(tempFile);
            Assert.Equal(1, logEntries.Length);

            var entry1Parts = logEntries[0].Split('\t');
            Assert.Equal("[INFO]", entry1Parts[1]);
            Assert.Equal("[Override1]", entry1Parts[2]);
            Assert.Equal("Override Line", entry1Parts[4]);

            logFactory = new LoggerFactory();
            logFactory.AddFileLogger(tempFile, FileWriteOption.Override);
            logger = logFactory.CreateLogger("Override2");
            logger.LogInformation("Override Line2");
            logFactory.Dispose();

            logEntries = System.IO.File.ReadAllLines(tempFile);
            Assert.Equal(1, logEntries.Length);

            var entry2Parts = logEntries[0].Split('\t');
            Assert.Equal("[INFO]", entry2Parts[1]);
            Assert.Equal("[Override2]", entry2Parts[2]);
            Assert.Equal("Override Line2", entry2Parts[4]);
            logFactory.Dispose();
        }
        finally
        {
            System.IO.File.Delete(tempFile);
        }
    }

    [Theory]
    [InlineData(3)]
    [InlineData(1)]
    public void WriteWithRollingFile_CreateNewFileToRolling(int fileNum)
    {
        var testDirectory = Path.Combine(Path.GetTempPath(), "testRolling");
        Directory.CreateDirectory(testDirectory);
        try
        {
            var tempFile = Path.Combine(testDirectory, "test.log");

            var logFactory = new LoggerFactory();
            logFactory.AddFileLogger(tempFile, (option) =>
            {
                option.FileLimitBytes = 1024 * 4;
                option.FileWriteOption = FileWriteOption.Append;
                option.MaxRollingFiles = fileNum;
            });

            var logger = logFactory.CreateLogger("MaxRollingLogger");

            for (int i = 0; i < 300; i++)
            {
                logger.LogInformation("hello world, this is logger test");
                //文件写入是异步的，这里我们每在Queue中写入50条时，就等待一会儿后台线程的Buffer写入。
                if (i % 50 == 0)
                    Thread.Sleep(50);
            }

            logFactory.Dispose();

            Assert.Equal(fileNum, Directory.GetFiles(testDirectory).Length);
        }
        finally
        {
            System.IO.Directory.Delete(testDirectory, true);
        }
    }

    [Fact]
    public void AutoCreateDirectory_WhenWritingFile()
    {
        var testDirectory = Path.Combine(Path.GetTempPath(), "testAutoCreateDir");

        try
        {
            var randomDir = Guid.NewGuid().ToString();
            var tempFile = Path.Combine(testDirectory, randomDir, "test.log");

            var logFactory = new LoggerFactory();
            logFactory.AddFileLogger(tempFile, FileWriteOption.Append);
            var logger = logFactory.CreateLogger("createDir");

            logger.LogInformation("is a directory created? ");
            logFactory.Dispose();

            Assert.Equal(1, System.IO.File.ReadAllLines(tempFile).Length);
            Assert.Contains(randomDir, Directory.GetDirectories(testDirectory).First().Split('/', '\\'));
        }
        finally
        {
            System.IO.Directory.Delete(testDirectory, true);
        }
    }

    [Fact]
    public void SetFileNameFormatter_ChangeNameWhenWrite()
    {
        var testDirectory = Path.Combine(Path.GetTempPath(), "testFormatter");
        Directory.CreateDirectory(testDirectory);

        try
        {
            var tempFile = Path.Combine(testDirectory, "test_{0}.log");

            var logFactory = new LoggerFactory();
            logFactory.AddFileLogger(tempFile, options => { options.FormatLogFileName = (fileName) => string.Format(fileName, "zhousl"); });
            var logger = logFactory.CreateLogger("formatter");

            logger.LogInformation("format the file");
            logFactory.Dispose();

            var logFiles = Directory.GetFiles(Path.GetDirectoryName(tempFile) ?? string.Empty, "*.*", SearchOption.TopDirectoryOnly);
            Assert.NotNull(logFiles.FirstOrDefault(x => x.Contains("zhousl")));
        }
        finally
        {
            System.IO.Directory.Delete(testDirectory, true);
        }
    }

    [Fact]
    public void SetLogFilter_RemoveUselessLog()
    {
        var testDirectory = Path.Combine(Path.GetTempPath(), "testFilterEntry");
        Directory.CreateDirectory(testDirectory);

        try
        {
            var tempFile = Path.Combine(testDirectory, "test.log");

            var logFactory = new LoggerFactory();
            logFactory.AddFileLogger(tempFile, options => { options.FilterLogEntry = messageEntity => messageEntity.LogLevel > LogLevel.Information && messageEntity.EventId.Id != 0; });
            var logger = logFactory.CreateLogger("filter");

            //不满足Filter条件，被过滤
            logger.LogInformation("this info will not be log");

            //set a default EventId to test
            logger.LogError(1000, "this info will be log");

            logFactory.Dispose();

            Assert.Equal(1, System.IO.File.ReadAllLines(tempFile).Length);
        }
        finally
        {
            System.IO.Directory.Delete(testDirectory, true);
        }
    }
    
    [Fact]
    public void UseErrorHandling_WhenOpenFile()
    {
        //Mac系统中多个线程可以重入文件写入句柄
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return;
        
        var testDirectory = Path.Combine(Path.GetTempPath(), "testErrorHandling");
        Directory.CreateDirectory(testDirectory);

        try
        {
            var tempFile = Path.Combine(testDirectory, "test.log");

            var task = Task.Run(() =>
            {
                var factory = new LoggerFactory();
                factory.AddProvider(new FileLoggerProvider(tempFile));
                var logger1 = factory.CreateLogger("TEST");
                LogMessageInFileWithDelay(logger1, 20);
                factory.Dispose();
            });
          
            //让task线程先获取到文件的读写句柄
            Thread.Sleep(500);

            Assert.Throws<IOException>(() =>
            {
                var factory2 = new LoggerFactory();
                factory2.AddProvider(new FileLoggerProvider(tempFile));
                var logger2 = factory2.CreateLogger("TEST");
                LogMessageInFileWithDelay(logger2, 20);
                factory2.Dispose();
            });

            var errorFallbackFile = Path.Combine(testDirectory, "fallbackError.log");
            var useErrorHandler = false;
            var factory3 = new LoggerFactory();
            factory3.AddProvider(new FileLoggerProvider(tempFile, new LoggerOptions()
            {
                HandleFileError = (err) =>
                {
                    useErrorHandler = true;
                    err.UseNewLogFileName(errorFallbackFile);
                }
            }));
            
            var logger3 = factory3.CreateLogger("TEST");
            LogMessageInFileWithDelay(logger3, 20);
            
            Assert.True(useErrorHandler);
            factory3.Dispose();
            
            var altLogFileInfo = new FileInfo(errorFallbackFile);
            Assert.True(altLogFileInfo.Exists);
            Assert.True(altLogFileInfo.Length > 0);
        }
        finally
        {
            System.IO.Directory.Delete(testDirectory, true);
        }
    }
    
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UserErrorHandling_WhenWritingFile(bool useNewLogFile)
    {
        var tmpDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var logFileName = Path.Combine(tmpDir, "testFile.log");
        var fallbackLogFileName = Path.Combine(tmpDir, "fallbackFile.log");

        var factory = new LoggerFactory();
        var fileLoggerOpts = new LoggerOptions();
        if (useNewLogFile)
        {
            fileLoggerOpts.HandleFileError = (fileErr) => { fileErr.UseNewLogFileName(fallbackLogFileName); };
        }

        var fileLogProvider = new FileLoggerProvider(logFileName, fileLoggerOpts);
        factory.AddProvider(fileLogProvider);
        var logger = factory.CreateLogger("TEST");
        LogMessageInFile(logger, 1);
        
        var logFileWriter = fileLogProvider.GetType()
            .GetField("fWriter", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?
            .GetValue(fileLogProvider);
        // close file handler, this will cause an exception inside FileLoggerProvider.ProcessQueue 
        var logFileStream = (Stream)logFileWriter.GetType()
            .GetField("_logFileStream", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            .GetValue(logFileWriter);
        logFileStream?.Close();

        var t = Task.Run(() => { LogMessageInFile(logger, 2000); });
        Assert.True(t.Wait(1000), "Logger queue blocks app's log calls.");

        factory.Dispose();

        if (useNewLogFile)
        {
            Assert.True(new System.IO.FileInfo(fallbackLogFileName).Length > 0, "Alternative log file name was not used.");
        }
    }
    
    [Fact]
    public void WriteLog_Concurrently()
    {
        var testDirectory = Path.Combine(Path.GetTempPath(), "testErrorHandling");
        Directory.CreateDirectory(testDirectory);

        try
        {
            var tempFile = Path.Combine(testDirectory, "test.log");
            var logFactory = new LoggerFactory();
            logFactory.AddFileLogger(tempFile, FileWriteOption.Append);

            var writeTasks = new List<Task>();
            for (int i = 0; i < 5; i++)
            {
                //keep i is correct for any thread
                var i1 = i;
                writeTasks.Add(Task.Factory.StartNew(() =>
                {
                    // ReSharper disable once AccessToDisposedClosure
                    var logger = logFactory.CreateLogger("TEST" + i1);
                    for (int j = 0; j < 100000; j++)
                    {
                        logger.LogInformation("MSG" + j);
                    }
                }));
            }

            Task.WaitAll(writeTasks.ToArray());
            logFactory.Dispose();
            Assert.Equal(100000 * 5, System.IO.File.ReadAllLines(tempFile).Length);
        }
        finally
        {
            System.IO.Directory.Delete(testDirectory, true);
        }
    }
    
    [Fact]
    public void ExpandEnvironmentVariables()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return;

        var tmpFileWithEnvironmentVariable = "%TEMP%\\" + Path.GetFileName(Path.GetTempFileName());
        var expandedTmpFileName = Path.Combine(Path.GetTempPath(), Path.GetFileName(tmpFileWithEnvironmentVariable));
        try
        {
            var factory = new LoggerFactory();
            factory.AddProvider(new FileLoggerProvider(tmpFileWithEnvironmentVariable, FileWriteOption.Append));
            var logger = factory.CreateLogger("TEST");
            logger.LogInformation("Line1");
            factory.Dispose();

            Assert.Single(System.IO.File.ReadAllLines(expandedTmpFileName));
        }
        finally
        {
            System.IO.File.Delete(expandedTmpFileName);
        }

    }

    private void LogMessageInFile(ILogger logger, int messageCount)
    {
        for (int i = 0; i < messageCount; i++)
        {
            logger.LogInformation($"hello i am {i}");
        }
    }

    private void LogMessageInFileWithDelay(ILogger logger, int messageCount)
    {
        for (int i = 0; i < messageCount; i++)
        {
            logger.LogInformation($"hello i am {i}");
            Thread.Sleep(200);
        }
    }
}