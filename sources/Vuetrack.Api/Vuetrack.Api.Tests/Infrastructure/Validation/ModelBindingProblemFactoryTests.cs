using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Vuetrack.Api.Features.Integrations.Contracts;
using Vuetrack.Api.Infrastructure.Validation;
using Xunit;

namespace Vuetrack.Api.Tests.Infrastructure.Validation;

public class ModelBindingProblemFactoryTests
{
    [Fact]
    public void Create_MapsBindingErrorsToValidationErrors_AndNormalizesKeys()
    {
        var provider = BuildProvider();
        var modelState = new ModelStateDictionary();

        modelState.AddModelError("ProjectId", "The ProjectId field is required.");

        var conversionException = new FormatException("could not convert");
        modelState.TryAddModelException("$.dateStarted", conversionException);

        var context = CreateContext(provider, modelState);

        var result = ModelBindingProblemFactory.Create(context);

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        objectResult.ContentTypes.Should().Contain("application/problem+json");

        var problem = objectResult.Value.Should().BeOfType<ValidationProblemDetails>().Subject;

        problem.Errors.Should().ContainKey("ProjectId");
        problem.Errors["ProjectId"].Should().Contain(nameof(ValidationError.Required));

        problem.Errors.Should().ContainKey("dateStarted");
        problem.Errors["dateStarted"].Should().Contain(nameof(ValidationError.Invalid));
        problem.Errors.Should().NotContainKey("$.dateStarted");
    }

    private static ServiceProvider BuildProvider() =>
        new ServiceCollection()
            .AddLogging()
            .AddMvcCore()
            .Services
            .AddProblemDetails()
            .BuildServiceProvider();

    private static ActionContext CreateContext(IServiceProvider provider, ModelStateDictionary modelState)
    {
        var httpContext = new DefaultHttpContext { RequestServices = provider };
        return new ActionContext(httpContext, new RouteData(), new ActionDescriptor(), modelState);
    }
}
