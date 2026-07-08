using Samhammer.DependencyInjection.Attributes;

namespace Vuetrack.Api.Features.Test;

[Inject]
public class TestService : ITestService
{
    public string GetText()
    {
        return "Hello World";
    }
}

public interface ITestService
{
    string GetText();
}
