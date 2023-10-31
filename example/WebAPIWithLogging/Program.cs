using Xieyi.Logging.File;
using Xieyi.Logging.File.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//通过配置文件设置FileLogger
builder.Services.AddLogging(loggingBuilder =>
{
    var loggingSection = builder.Configuration.GetSection("Logging");
    loggingBuilder.AddConfiguration(loggingSection);
    loggingBuilder.AddConsole();
    
    loggingBuilder.AddFileLogger(loggingSection.GetSection("FileOne"), fileOpts =>
    {
        fileOpts.FormatLogFileName = fName => Path.IsPathRooted(fName) ? fName : Path.Combine(builder.Environment.ContentRootPath, fName);
        fileOpts.FilterLogEntry = message => message.LogLevel < LogLevel.Error;
    });
    
    loggingBuilder.AddFileLogger(loggingSection.GetSection("FileTwo"), fileOpts =>
    {
        fileOpts.FormatLogFileName = fName => Path.IsPathRooted(fName) ? fName : Path.Combine(builder.Environment.ContentRootPath, fName);
        fileOpts.FilterLogEntry = message => message.LogLevel >= LogLevel.Error;
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();