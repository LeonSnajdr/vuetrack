using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace Vuetrack.Api.Test;

[ApiController]
[ApiVersion("1")]
[Route("api/v{version:apiVersion}/[controller]")]
public class TestController(ITestService testService) : ControllerBase
{
    private ITestService TestService = testService;

    [HttpGet]
    public IActionResult Get()
    {
        var text = TestService.GetText();
        return Ok(text);
    }
}
