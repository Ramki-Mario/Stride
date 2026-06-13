using FluentAssertions;
using NSubstitute;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.Modules.Webhooks.Application.Abstractions;
using STRIDE.Modules.Webhooks.Application.Commands.DeleteWebhookSubscription;
using STRIDE.Modules.Webhooks.Application.Commands.RegenerateWebhookSecret;
using STRIDE.Modules.Webhooks.Application.Commands.TestWebhookSubscription;
using STRIDE.Modules.Webhooks.Application.Commands.UpdateWebhookSubscription;
using STRIDE.Modules.Webhooks.Domain;
using STRIDE.Modules.Webhooks.Domain.Entities;
using STRIDE.Modules.Webhooks.Domain.Repositories;

namespace STRIDE.Modules.Webhooks.Application.Tests;

public sealed class WebhookSubscriptionCommandHandlersTests
{
    private readonly IWebhookSubscriptionRepository _repo  = Substitute.For<IWebhookSubscriptionRepository>();
    private readonly IAuditLogger                   _audit = Substitute.For<IAuditLogger>();
    private readonly ICurrentUser                   _user  = Substitute.For<ICurrentUser>();
    private readonly IWebhookTester                 _tester = Substitute.For<IWebhookTester>();

    private static readonly Guid Tenant = Guid.NewGuid();
    private static readonly Guid Actor  = Guid.NewGuid();

    public WebhookSubscriptionCommandHandlersTests() => _user.Email.Returns("admin@example.com");

    private static WebhookSubscription NewSub() =>
        WebhookSubscription.Create(Tenant, "https://example.com/hook",
            new[] { WebhookEventTypes.WorkflowInstanceCompleted }, Actor);

    // ── Update ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_WhenNotFound_ReturnsFailure()
    {
        _repo.GetByIdAsync(Tenant, Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((WebhookSubscription?)null);
        var sut = new UpdateWebhookSubscriptionCommandHandler(_repo, _audit, _user);

        var result = await sut.Handle(new UpdateWebhookSubscriptionCommand(
            Tenant, Guid.NewGuid(), "https://example.com/x",
            new[] { WebhookEventTypes.InvoiceSent }, true, Actor), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _repo.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_WhenFound_PersistsAndAudits()
    {
        var sub = NewSub();
        _repo.GetByIdAsync(Tenant, Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(sub);
        var sut = new UpdateWebhookSubscriptionCommandHandler(_repo, _audit, _user);

        var result = await sut.Handle(new UpdateWebhookSubscriptionCommand(
            Tenant, sub.Id, "https://example.com/changed",
            new[] { WebhookEventTypes.UserInvited }, false, Actor), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        sub.Url.Should().Be("https://example.com/changed");
        sub.IsActive.Should().BeFalse();
        _repo.Received(1).Update(sub);
        await _repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _audit.Received(1).LogAsync(Arg.Is<AuditLogEntry>(e => e.Action == AuditActions.WebhookUpdated));
    }

    // ── Delete ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_WhenNotFound_ReturnsFailure()
    {
        _repo.GetByIdAsync(Tenant, Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((WebhookSubscription?)null);
        var sut = new DeleteWebhookSubscriptionCommandHandler(_repo, _audit, _user);

        var result = await sut.Handle(new DeleteWebhookSubscriptionCommand(Tenant, Guid.NewGuid(), Actor), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Delete_WhenFound_SoftDeletesAndAudits()
    {
        var sub = NewSub();
        _repo.GetByIdAsync(Tenant, Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(sub);
        var sut = new DeleteWebhookSubscriptionCommandHandler(_repo, _audit, _user);

        var result = await sut.Handle(new DeleteWebhookSubscriptionCommand(Tenant, sub.Id, Actor), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        sub.IsDeleted.Should().BeTrue();
        await _audit.Received(1).LogAsync(Arg.Is<AuditLogEntry>(e => e.Action == AuditActions.WebhookDeleted));
    }

    // ── Regenerate secret ─────────────────────────────────────────────────────

    [Fact]
    public async Task Regenerate_WhenNotFound_ReturnsFailure()
    {
        _repo.GetByIdAsync(Tenant, Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((WebhookSubscription?)null);
        var sut = new RegenerateWebhookSecretCommandHandler(_repo, _audit, _user);

        var result = await sut.Handle(new RegenerateWebhookSecretCommand(Tenant, Guid.NewGuid(), Actor), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Regenerate_WhenFound_ReturnsNewSecret()
    {
        var sub      = NewSub();
        var original = sub.SigningSecret;
        _repo.GetByIdAsync(Tenant, Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(sub);
        var sut = new RegenerateWebhookSecretCommandHandler(_repo, _audit, _user);

        var result = await sut.Handle(new RegenerateWebhookSecretCommand(Tenant, sub.Id, Actor), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.SigningSecret.Should().NotBe(original);
        await _audit.Received(1).LogAsync(Arg.Is<AuditLogEntry>(e => e.Action == AuditActions.WebhookSecretRegenerated));
    }

    // ── Test ping ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Test_WhenNotFound_ReturnsFailure()
    {
        _repo.GetByIdAsync(Tenant, Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((WebhookSubscription?)null);
        var sut = new TestWebhookSubscriptionCommandHandler(_repo, _tester);

        var result = await sut.Handle(new TestWebhookSubscriptionCommand(Tenant, Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _tester.DidNotReceive().SendPingAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Test_WhenFound_InvokesTesterWithUrlAndSecret()
    {
        var sub = NewSub();
        _repo.GetByIdAsync(Tenant, Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(sub);
        _tester.SendPingAsync(sub.Url, sub.SigningSecret, Arg.Any<CancellationToken>())
               .Returns(new WebhookTestResult(true, 200, null, 42));
        var sut = new TestWebhookSubscriptionCommandHandler(_repo, _tester);

        var result = await sut.Handle(new TestWebhookSubscriptionCommand(Tenant, sub.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Success.Should().BeTrue();
        result.Value.StatusCode.Should().Be(200);
        await _tester.Received(1).SendPingAsync(sub.Url, sub.SigningSecret, Arg.Any<CancellationToken>());
    }
}
