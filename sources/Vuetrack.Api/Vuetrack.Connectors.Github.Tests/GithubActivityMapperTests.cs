using AwesomeAssertions;
using Vuetrack.Connectors.Abstractions;
using Vuetrack.Connectors.Github.Activity;
using Vuetrack.Connectors.Github.Activity.Api;
using Xunit;

namespace Vuetrack.Connectors.Github.Tests;

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

        var signal = GithubActivityMapper.ToCommitSignal(item);

        signal.ConnectorKey.Should().Be(ConnectorKey.Github);
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
