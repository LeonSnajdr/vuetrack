using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Vuetrack.Api.Features.Config.Contracts;
using Vuetrack.Api.Infrastructure.Config;

namespace Vuetrack.Api.Features.Config;

[ApiController]
[ApiVersion("1")]
[Route("api/v{version:apiVersion}/[controller]")]
[AllowAnonymous]
public class ConfigController(IOptions<AppAuthOptions> authOptions) : ControllerBase
{
    private IOptions<AppAuthOptions> AuthOptions { get; } = authOptions;

    [HttpGet]
    public ConfigContract Get()
    {
        var options = AuthOptions.Value;

        var authContract = new AppAuthContract(options.AuthUrl, options.Realm, options.AppClientId, options.ApiClientId);

        return new ConfigContract(authContract);
    }
}
