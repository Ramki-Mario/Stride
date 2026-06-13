using FluentAssertions;
using NSubstitute;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.Modules.Webhooks.Application.Commands.CreateWebhookSubscription;
using STRIDE.Modules.Webhooks.Domain;
using STRIDE.Modules.Webhooks.Domain.Entities;
using STRIDE.Modules.Webhooks.Domain.Repositories;

namespace STRIDE.Modules.Webhooks.Application.Tests;

public sealed class CreateWebhookSubscriptionCommandHandlerTests
{
    private readonly IWebhookSubscriptionRepository _repo  = Substitute.For<IWebhookSubscriptionRepository>();
    private readonly IAuditLogger                   _audit = Substitute.For<IAuditLogger>();
    private readonly ICurrentUser                   _user  = Substitute.For<ICurrentUser>();
    private readonly CreateWebhookSubscriptionCommandHandler _sut;

    private static readonly Guid Tenant = Guid.NewGuid();
    private static readonly Guid Actor  = Guid.NewGuid();

    public CreateWebhookSubscriptionCommandHandlerTests()
    {
        _user.Email.Returns("admin@example.com");
        _sut = new CreateWebhookSubscriptionCommandHandler(_repo, _audit, _user);
    }

    private CreateWebhookSubscriptionCommand ValidCommand =>
        new(Tenant, "https://example.com/hook",
            new[] { WebhookEventTypes.WorkflowInstanceCompleted }, Actor);

    [Fact]
    public async Task Handle_WhenValid_PersistsAndReturnsSecretOnce()
    {
        _repo.CountActiveAsync(Tenant, Arg.Any<CancellationToken>()).Returns(0);

        var result = await _sut.Handle(ValidCommand, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.SigningSecret.Should().StartWith("whsec_");
        result.Value.Id.Should().NotBeEmpty();
        await _repo.Received(1).AddAsync(Arg.Any<WebhookSubscription>(), Arg.Any<CancellationToken>());
        await _repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _audit.Received(1).LogAsync(Arg.Is<AuditLogEntry>(e => e.Action == AuditActions.WebhookCreated));
    }

    [Fact]
    public async Task Handle_WhenLimitReached_ReturnsFailureAndDoesNotPersist()
    {
        _repo.CountActiveAsync(Tenant, Arg.Any<CancellationToken>()).Returns(25);

        var result = await _sut.Handle(ValidCommand, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Maximum");
        await _repo.DidNotReceive().AddAsync(Arg.Any<WebhookSubscription>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenUrlNotHttps_ReturnsFailureFromDomain()
    {
        _repo.CountActiveAsync(Tenant, Arg.Any<CancellationToken>()).Returns(0);
        var cmd = ValidCommand with { Url = "http://insecure.example.com" };

        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _repo.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
