namespace Xieyi.Logging.File.Test;

public class FileLoggerProviderTest
{
    private const string constFileDir = "/Users/zhousl/Documents/codes/mycsharp/Xieyi.Logging/test/Xieyi.Logging.File.Test/tempLogFile/log.txt"; 

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
            
            Assert.Equal(6, System.IO.File.ReadAllLinesAsync(tempFile).Result.Length);
        }
        finally
        {
            System.IO.File.Delete(tempFile);
        }
    }
}