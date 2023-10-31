# Xieyi.Logging
基于 .NET 7 的一款简单有效的日志处理框架，并遵循 Microsoft.Extension.Logging 的规约。

## Xieyi.Logging.File
- 它的用法和标准的 ConsoleLogger 非常相似，可将日志信息写入本地文件
- 在写入时可以选择“追加” or “覆盖”两种写入行为
- 支持 “RollingFile” 模式来控制日志文件的个数和日志文件大小
- 可自定义的日志文件名
- 内部使用线程安全的队列实现日志记录，避免线程阻塞，具有一定的并发性能

### 使用示例
在StartUp.cs中简单初始化该服务
```
services.AddLogging(loggingBuilder => {
	loggingBuilder.AddFile("app.log", FileWriteOption.Append);
});
```
使用appsettings.json完成配置
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
- 如果只有 `FileSizeLimitBytes` 被特别指定为某个数量，那么 logger 在当前指定日志文件大小接近 `FileSizeLimitBytes` 设置的值时，将会创建 "test.log", "test1.log", "test2.log" 等等日志文件进行写入。
- 在 `FileSizeLimitBytes` 的基础上，通过设置 `MaxRollingFiles` 来限制日志文件产生的总量; 若该值为“3”那么 logger 将创建 "test.log", "test1.log", "test2.log"，在 “test2.log” 大小到达限制后，将会使用 "test.log" 继续记录日志信息，而 "test.log" 中的旧有信息将会被覆盖。

### 即时更改的日志文件名称
可以使用 `FileLoggerOptions` 属性中的 `FormatLogFileName` 来自定义日志文件名。日志文件名可以随着时间的推移而改变。
例如，每天创建一个新的日志文件：
```
services.AddLogging(loggingBuilder => {
	loggingBuilder.AddFile("app_{0:yyyy}-{0:MM}-{0:dd}.log", fileLoggerOpts => {
		fileLoggerOpts.FormatLogFileName = fName => {
			return String.Format(fName, DateTime.UtcNow);
		};
	});
});
```
需要注意的是：文件重命名逻辑，在 Logger 的每次 Write 方法前都会执行。因此我们最好将名称的计算结果缓存到本地，避免频繁写入造成的计算开销。

### 自定义日志记录格式
你可以特殊指定 `FileLoggerOptions.FormatLogEntry` 委托来自定义日志的输出内容。 例如，可以将日志信息记录为 JSON 数组格式：
```
loggingBuilder.AddFile("logs/app.js", fileLoggerOpts => {
	fileLoggerOpts.FormatLogEntry = (msg) => {
		var sb = new System.Text.StringBuilder();
		StringWriter sw = new StringWriter(sb);
		var jsonWriter = new Newtonsoft.Json.JsonTextWriter(sw);
		jsonWriter.WriteStartArray();
		jsonWriter.WriteValue(DateTime.Now.ToString("o"));
		jsonWriter.WriteValue(msg.LogLevel.ToString());
		jsonWriter.WriteValue(msg.LogName);
		jsonWriter.WriteValue(msg.EventId.Id);
		jsonWriter.WriteValue(msg.Message);
		jsonWriter.WriteValue(msg.Exception?.ToString());
		jsonWriter.WriteEndArray();
		return sb.ToString();
	}
});
```

### 自定义日志记录过滤器
我们可以定义日志类型 Filter 的 Predicate ，在日志写入时进行逻辑判断，决定该日志信息是否可以写入 logger 指定的文件。我们可以使用此逻辑将业务中不同类型的日志信息分发到不同的文件中。当多个logger 对某个信息进行写入时，该信息只会被写入满足 Filter 条件 logger 指定的日志文件。
```
loggingBuilder.AddFile("logs/errors_only.log", fileLoggerOpts => {
	fileLoggerOpts.FilterLogEntry = (msg) => {
		return msg.LogLevel == LogLevel.Error;
	}
});
```

### 日志记录错误处理
`FileLoggerProvider` 实例被创建时（也即是`AddFile`被调用时）会同时打开日志文件。无论是文件流 Open 环节，还是 Stream 的写入环境，都可能出现IO异常。若我们想要简单处理该类型异常时，我们可以使用 `try .. catch` 包裹代码处理。但如果遇到我们无法处理的异常，这个时候日志文件可能会处于不可用的状态，此时我们可以指定一个新的文件地址进行写入，来保证一定的可用性。
你可以使用以下委托来建立备选文件地址，防止原始文件不可用的情况：
```
loggingBuilder.AddFile(loggingSection, fileLoggerOpts => {
	fileLoggerOpts.HandleFileError = (err) => {
		err.UseNewLogFileName( Path.GetFileNameWithoutExtension(err.LogFileName)+ "_alt" + Path.GetExtension(err.LogFileName) );
	};
});
```
然而，在使用时我们可能遇到更加复杂的场景，导致我们的备选日志文件也无法被使用，那么这个时候并不会递归的去创建新的备选文件，而是不进行任何日志记录。
