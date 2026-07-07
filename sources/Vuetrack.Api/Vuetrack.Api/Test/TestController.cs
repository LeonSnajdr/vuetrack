using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace Vuetrack.Api.Test;

[ApiController]
[ApiVersion("1")]
[Route("api/v{version:apiVersion}/[controller]")]
public class TestController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok("Hello World!");
    }
}
