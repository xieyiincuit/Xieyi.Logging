namespace Xieyi.Logging.File.Test;

public class FileLoggerProviderTest
{
    private const string constFileDir = "/Users/zhousl/Documents/codes/mycsharp/Xieyi.Logging/test/Xieyi.Logging.File.Test/tempLogFile"; 

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

    [Fact]
    public void WriteWithRollingFile_CreateNewFileToRolling()
    {
        var testDirectory = Path.Combine(constFileDir, "testRolling");
        Directory.CreateDirectory(testDirectory);
        try
        {
            var tempFile = Path.Combine(testDirectory, "test.log");

            var logFactory = new LoggerFactory();
            logFactory.AddFileLogger(tempFile, (option) =>
            {
                option.FileLimitBytes = 1024 * 4;
                option.FileWriteOption = FileWriteOption.Append;
                option.MaxRollingFiles = 3;
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
            
            Assert.Equal(3, Directory.GetFiles(testDirectory).Length);
        }
        finally
        {
            System.IO.Directory.Delete(testDirectory, true);
        }
    }
}