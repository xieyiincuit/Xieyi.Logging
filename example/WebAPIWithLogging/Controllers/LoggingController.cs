using Microsoft.AspNetCore.Mvc;

namespace WebAPIWithLogging.Controllers;

[ApiController]
[Route("[controller]")]
public class LoggingController : ControllerBase
{
    private readonly ILogger<LoggingController> _logger;

    public LoggingController(ILogger<LoggingController> logger)
    {
        _logger = logger;
    }

    [HttpPost]
    [Route("Info")]
    public IActionResult Info([FromBody] string message)
    {
        _logger.LogInformation("info world " + message);
        return Ok();
    }

    [HttpPost]
    [Route("Debug")]
    public IActionResult Debug([FromBody] string message)
    {
        _logger.LogDebug("debug world " + message);
        return Ok();
    }

    [HttpPost]
    [Route("Error")]
    public IActionResult Error([FromBody] string message)
    {
        _logger.LogError("error world " + message);
        return Ok();
    }

    [HttpGet]
    public IActionResult GetLogs()
    {
        _logger.LogInformation("hello world info");
        return Ok();
    }
}