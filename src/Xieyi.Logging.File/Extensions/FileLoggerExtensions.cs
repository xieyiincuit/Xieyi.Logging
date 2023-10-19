namespace Xieyi.Logging.File.Extensions;

public static class FileLoggerExtensions
{
    public static ILoggingBuilder AddFileLogger(this ILoggingBuilder builder, string fileName, FileWriteOption writeOption)
    {
        builder.Services.Add(ServiceDescriptor.Singleton<ILoggerProvider, FileLoggerProvider>(_ => new FileLoggerProvider(fileName, writeOption)));
        return builder;
    }
    
    public static ILoggingBuilder AddFileLogger(this ILoggingBuilder builder, string fileName, Action<LoggerOptions> configure)
    {
        builder.Services.Add(ServiceDescriptor.Singleton<ILoggerProvider, FileLoggerProvider>(_ =>
        {
            var options = new LoggerOptions();
            configure(options);
            return new FileLoggerProvider(fileName, options);
        }));
        
        return builder;
    }
}