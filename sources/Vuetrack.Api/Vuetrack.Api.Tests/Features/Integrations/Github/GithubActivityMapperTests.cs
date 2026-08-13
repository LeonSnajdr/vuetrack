using AwesomeAssertions;
using Vuetrack.Api.Features.Integrations;
using Vuetrack.Api.Features.Integrations.Github;
using Vuetrack.Api.Features.Integrations.Github.Api;
using Vuetrack.Api.Features.Integrations.Github.Internal;
using Xunit;

namespace Vuetrack.Api.Tests.Features.Integrations.Github;

public class GithubActivityMapperTests
{
    [Fact]
    public void ToCommitSignal_MapsCommitToSignal()
    {
        var item = new GithubCommitItemResponse
        {
            Sha = "abcdef1234567890",
            Commit = new GithubCommitResponse
            {
                Message = "feat: add thing\n\ndetails",
                Author = new GithubCommitAuthorResponse
                {
                    Date = new DateTime(2026, 7, 1, 7, 0, 0, DateTimeKind.Utc),
                },
            },
            Repository = new GithubRepoResponse { Name = "widgets", FullName = "acme/widgets" },
        };

        var signal = item.ToCommitSignal();

        signal.Key.Should().Be(IntegrationKey.Github);
        signal.Kind.Should().Be(ActivityKind.Commit);
        signal.ExternalId.Should().Be("acme/widgets:commit:abcdef1234567890");
        signal.DateStarted.Should().Be(new DateTime(2026, 7, 1, 7, 0, 0, DateTimeKind.Utc));
        signal.DateStarted.Kind.Should().Be(DateTimeKind.Utc);
        signal.DateEnded.Should().BeNull();

        var detail = signal.Detail.Should().BeOfType<GithubSignalDetail>().Which;
        detail.RepoName.Should().Be("widgets");
        detail.Message.Should().Be("feat: add thing\n\ndetails");
    }
}
