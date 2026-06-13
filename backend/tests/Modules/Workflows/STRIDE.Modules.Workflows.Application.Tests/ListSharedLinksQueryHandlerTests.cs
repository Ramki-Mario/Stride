using STRIDE.Modules.Workflows.Application.Abstractions;
using STRIDE.Modules.Workflows.Application.Queries.ListSharedLinks;
using STRIDE.Modules.Workflows.Domain.Entities;

namespace STRIDE.Modules.Workflows.Application.Tests;

public sealed class ListSharedLinksQueryHandlerTests
{
    private readonly ISharedWorkflowLinkRepository _links     = Substitute.For<ISharedWorkflowLinkRepository>();
    private readonly ISharedLinkUrlBuilder         _urlBuilder = Substitute.For<ISharedLinkUrlBuilder>();
    private readonly ListSharedLinksQueryHandler   _sut;

    public ListSharedLinksQueryHandlerTests()
    {
        _urlBuilder.BuildShareUrl(Arg.Any<string>())
                   .Returns(ci => $"https://app.test/job/{ci.Arg<string>()}");
        _sut = new ListSharedLinksQueryHandler(_links, _urlBuilder);
    }

    [Fact]
    public async Task Handle_WhenNoLinks_ReturnsEmptyList()
    {
        _links.ListActiveByInstanceAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
              .Returns(Array.Empty<SharedWorkflowLink>());

        var result = await _sut.Handle(new ListSharedLinksQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_MapsLinksToDtosWithUrls()
    {
        var instanceId = Guid.NewGuid();
        var link1 = SharedWorkflowLink.Create(instanceId, Guid.NewGuid(), Guid.NewGuid());
        var link2 = SharedWorkflowLink.Create(instanceId, Guid.NewGuid(), Guid.NewGuid());

        _links.ListActiveByInstanceAsync(instanceId, Arg.Any<CancellationToken>())
              .Returns(new[] { link1, link2 });

        var result = await _sut.Handle(new ListSharedLinksQuery(instanceId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value[0].ShareUrl.Should().StartWith("https://app.test/job/");
        result.Value[0].IsActive.Should().BeTrue();
    }
}
