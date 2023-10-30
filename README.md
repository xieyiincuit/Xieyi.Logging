# Xieyi.Logging
基于 .NET7 的一款简单有效的日志处理框架，并遵循 Microsoft.Extension.Logging 的规约。

## Xieyi.Logging.File
- 它的用法和标准的 ConsoleLogger 非常相似，可将日志信息写入本地文件
- 在写入时可以选择“追加” or “覆盖”两种写入行为
- 支持 “RollingFile” 模式来控制日志文件的个数和占用内存大小
- 运行途中可以即时更改日志文件名
- 内部使用线程安全的队列实现日志记录，避免线程阻塞，具有一定的并发性能

### 使用示例
在StartUp.cs中初始化该服务
```
services.AddLogging(loggingBuilder => {
	loggingBuilder.AddFile("app.log", FileWriteOption.Append);
});
```
或者使用appsettings.json完成配置
```
services.AddLogging(loggingBuilder => {
	var loggingSection = Configuration.GetSection("Logging");
	loggingBuilder.AddFile(loggingSection);
});
```
以下为默认配置文件的例子：
```
"Logging": {
	"LogLevel": {
	  "Default": "Debug",
	  "System": "Information",
	  "Microsoft": "Error"
	},
	"File": {
		"FilePath": "app.log",
		"FileWriteOption": 5, // Override-0 Append-5
		"MinLevel": "Debug",  
		"FileLimitBytes": 0,  
		"MaxRollingFiles": 0 
	}
}
```

### Rolling File
当 `FileLoggerOptions` 属性中: `FileSizeLimitBytes` 和 `MaxRollingFiles` 被正确设置后，该功能将会启用. 我们假设我们的默认日志文件名为"test.log":
- 如果只有 `FileSizeLimitBytes` 被特别指定为某个数量，那么 logger 在当前指定日志文件接近 `FileSizeLimitBytes` 设置的值时，将会创建 "test.log", "test1.log", "test2.log" 等等日志文件进行日志的写入。
- 在 `FileSizeLimitBytes` 的基础上，通过设置 `MaxRollingFiles` 来限制日志文件产生的总量; 若该值为“3”那么 logger 将创建 "test.log", "test1.log", "test2.log"，在 “test2.log” 到达限制后，将会使用 "test.log" 继续记录日志信息，而 "test1.log" 中的旧有信息将会被覆盖。
