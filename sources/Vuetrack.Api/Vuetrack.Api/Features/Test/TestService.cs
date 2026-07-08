using Samhammer.DependencyInjection.Attributes;

namespace Vuetrack.Api.Features.Test;

[Inject]
public class TestService(ITestRepository testRepository) : ITestService
{
    private ITestRepository TestRepository { get; } = testRepository;

    public async Task<string> GetText()
    {
        await TestRepository.Save(new TestModel { Test = "test" });

        return "test";
    }
}

public interface ITestService
{
    Task<string> GetText();
}
