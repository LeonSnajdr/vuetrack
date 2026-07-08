using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Vuetrack.Api.Features.Test;

[ApiController]
[ApiVersion("1")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
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
