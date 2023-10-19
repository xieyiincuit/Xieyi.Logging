namespace Xieyi.Logging.File.Extensions;

public static class FileLoggerExtensions
{
    public static ILoggingBuilder AddFileLogger(this ILoggingBuilder builder, string fileName, FileWriteOption writeOption)
    {
        builder.Services.Add(ServiceDescriptor.Singleton<ILoggerProvider, FileLoggerProvider>(_ => new FileLoggerProvider(fileName, writeOption)));
        return builder;
    }
    
    public static ILoggingBuilder AddFileLogger(this ILoggingBuilder builder, string fileName, Action<LoggerOptions> configureAction)
    {
        builder.Services.Add(ServiceDescriptor.Singleton<ILoggerProvider, FileLoggerProvider>(_ =>
        {
            var options = new LoggerOptions();
            configureAction(options);
            return new FileLoggerProvider(fileName, options);
        }));
        
        return builder;
    }
    
    public static ILoggingBuilder AddFileLogger(this ILoggingBuilder builder, IConfiguration configuration, Action<LoggerOptions> configureAction = null)
    {
        var loggerProvider = CreateFromConfiguration(configuration, configureAction);
        if (loggerProvider != null)
        {
            builder.Services.AddSingleton<ILoggerProvider, FileLoggerProvider>(_ => loggerProvider);
        }

        return builder;
    }
    
    public static ILoggerFactory AddFileLogger(this ILoggerFactory factory, string fileName, FileWriteOption writeOption)
    {
        factory.AddProvider(new FileLoggerProvider(fileName, writeOption));
        return factory;
    }
    
    public static ILoggerFactory AddFileLogger(this ILoggerFactory factory, string fileName, Action<LoggerOptions> configureAction)
    {
        var options = new LoggerOptions();
        configureAction(options);
        factory.AddProvider(new FileLoggerProvider(fileName, options));
        
        return factory;
    }
    
    public static ILoggerFactory AddFileLogger(this ILoggerFactory factory, IConfiguration configuration, Action<LoggerOptions> configureAction = null)
    {
        var loggerProvider = CreateFromConfiguration(configuration, configureAction);
        factory.AddProvider(loggerProvider);
        
        return factory;
    }

    private static FileLoggerProvider CreateFromConfiguration(IConfiguration configuration, Action<LoggerOptions> configure)
    {
        var config = new LoggerConfig();
        var fileSection = configuration.GetSection("File");
        if (!fileSection.Exists())
        {
            //查询Config文件中设定的文件路径
            var pathValue = configuration["FilePath"];
            if (string.IsNullOrEmpty(pathValue))
                return null;
            
            configuration.Bind(config);
        }
        
        if (string.IsNullOrWhiteSpace(config.FilePath))
            return null;
            
        var fileLoggerOptions = new LoggerOptions
        {
            FileWriteOption = config.FileWriteOption,
            MinLevel = config.MinLevel,
            FileLimitBytes = config.FileLimitBytes,
            MaxRollingFiles = config.MaxRollingFiles
        };

        configure?.Invoke(fileLoggerOptions);

        return new FileLoggerProvider(config.FilePath, fileLoggerOptions);
    }
}