using CodexMeter.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CodexMeter.Desktop.Controllers;

[ApiController]
[Route("api/usage")]
public sealed class UsageController(IUsageProvider usageProvider) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<UsageSnapshot>> Get(CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await usageProvider.GetUsageAsync(cancellationToken));
        }
        catch (Exception exception)
        {
            return Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Nao foi possivel consultar o Codex agora.",
                detail: exception.Message);
        }
    }
}
