using CodexMeter.Desktop.Services;
using Microsoft.AspNetCore.Mvc;

namespace CodexMeter.Desktop.Controllers;

[ApiController]
[Route("api/app")]
public sealed class AppController(IWindowController windowController) : ControllerBase
{
    [HttpGet("status")]
    public IActionResult Status() => Ok(new { running = true });

    [HttpPost("open")]
    public IActionResult Open()
    {
        windowController.Open();
        return Ok(new { status = "opened" });
    }

    [HttpPost("minimize")]
    [HttpPost("minimizer")]
    public IActionResult Minimize()
    {
        windowController.Minimize();
        return Ok(new { status = "minimized" });
    }

    [HttpPost("config")]
    public IActionResult Config()
    {
        windowController.Config();
        return Ok(new { status = "config_opened" });
    }

    [HttpPost("off")]
    public IActionResult Close()
    {
        windowController.Close();
        return Ok(new { status = "closing" });
    }
}
