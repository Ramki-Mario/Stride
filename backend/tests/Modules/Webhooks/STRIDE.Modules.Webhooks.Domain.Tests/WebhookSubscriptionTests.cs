using FluentAssertions;
using STRIDE.Modules.Webhooks.Domain;
using STRIDE.Modules.Webhooks.Domain.Entities;
using STRIDE.Modules.Webhooks.Domain.Events;
using STRIDE.Modules.Webhooks.Domain.Exceptions;

namespace STRIDE.Modules.Webhooks.Domain.Tests;

public sealed class WebhookSubscriptionTests
{
    private static readonly Guid Tenant = Guid.NewGuid();
    private static readonly Guid Actor  = Guid.NewGuid();
    private const string ValidUrl = "https://example.com/hooks/stride";

    private static IReadOnlyList<string> ValidEvents =>
        new[] { WebhookEventTypes.WorkflowInstanceCompleted, WebhookEventTypes.InvoiceCreated };

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public void Create_WithValidData_SetsFieldsAndRaisesEvent()
    {
        var sub = WebhookSubscription.Create(Tenant, ValidUrl, ValidEvents, Actor);

        sub.TenantId.Should().Be(Tenant);
        sub.Url.Should().Be(ValidUrl);
        sub.IsActive.Should().BeTrue();
        sub.EventTypes.Should().BeEquivalentTo(ValidEvents);
        sub.SigningSecret.Should().StartWith("whsec_");
        sub.SigningSecret.Length.Should().BeGreaterThan(20);
        sub.DomainEvents.Should().ContainSingle(e => e is WebhookSubscriptionCreatedEvent);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyUrl_ThrowsRequired(string url)
    {
        var act = () => WebhookSubscription.Create(Tenant, url, ValidEvents, Actor);
        act.Should().Throw<WebhookDomainException>().WithMessage("*required*");
    }

    [Theory]
    [InlineData("http://example.com/hook")]   // not https
    [InlineData("ftp://example.com")]
    [InlineData("not-a-url")]
    [InlineData("example.com/hook")]          // not absolute
    public void Create_WithNonHttpsUrl_Throws(string url)
    {
        var act = () => WebhookSubscription.Create(Tenant, url, ValidEvents, Actor);
        act.Should().Throw<WebhookDomainException>().WithMessage("*HTTPS*");
    }

    [Fact]
    public void Create_WithNoEventTypes_Throws()
    {
        var act = () => WebhookSubscription.Create(Tenant, ValidUrl, Array.Empty<string>(), Actor);
        act.Should().Throw<WebhookDomainException>().WithMessage("*at least one event*");
    }

    [Fact]
    public void Create_WithUnknownEventType_Throws()
    {
        var act = () => WebhookSubscription.Create(Tenant, ValidUrl, new[] { "workflow.bogus.event" }, Actor);
        act.Should().Throw<WebhookDomainException>().WithMessage("*Unknown event type*");
    }

    [Fact]
    public void Create_DeduplicatesEventTypes()
    {
        var dupes = new[]
        {
            WebhookEventTypes.InvoiceCreated,
            WebhookEventTypes.InvoiceCreated,
            WebhookEventTypes.InvoiceSent,
        };

        var sub = WebhookSubscription.Create(Tenant, ValidUrl, dupes, Actor);

        sub.EventTypes.Should().HaveCount(2);
        sub.EventTypes.Should().Contain(WebhookEventTypes.InvoiceCreated);
        sub.EventTypes.Should().Contain(WebhookEventTypes.InvoiceSent);
    }

    // ── Update ──────────────────────────────────────────────────────────────

    [Fact]
    public void Update_ChangesUrlEventsAndActiveFlag()
    {
        var sub = WebhookSubscription.Create(Tenant, ValidUrl, ValidEvents, Actor);
        sub.ClearDomainEvents();

        var newUrl = "https://example.com/v2/hook";
        sub.Update(newUrl, new[] { WebhookEventTypes.UserInvited }, isActive: false, Actor);

        sub.Url.Should().Be(newUrl);
        sub.EventTypes.Should().ContainSingle().Which.Should().Be(WebhookEventTypes.UserInvited);
        sub.IsActive.Should().BeFalse();
        sub.DomainEvents.Should().ContainSingle(e => e is WebhookSubscriptionUpdatedEvent);
    }

    [Fact]
    public void Update_WithInvalidUrl_Throws()
    {
        var sub = WebhookSubscription.Create(Tenant, ValidUrl, ValidEvents, Actor);
        var act = () => sub.Update("http://insecure.example.com", ValidEvents, true, Actor);
        act.Should().Throw<WebhookDomainException>();
    }

    // ── Secret rotation ─────────────────────────────────────────────────────

    [Fact]
    public void RegenerateSecret_ProducesNewSecretAndRaisesEvent()
    {
        var sub      = WebhookSubscription.Create(Tenant, ValidUrl, ValidEvents, Actor);
        var original = sub.SigningSecret;
        sub.ClearDomainEvents();

        var rotated = sub.RegenerateSecret(Actor);

        rotated.Should().NotBe(original);
        sub.SigningSecret.Should().Be(rotated);
        sub.SigningSecret.Should().StartWith("whsec_");
        sub.DomainEvents.Should().ContainSingle(e => e is WebhookSecretRegeneratedEvent);
    }

    // ── Delete ──────────────────────────────────────────────────────────────

    [Fact]
    public void Delete_MarksDeletedAndRaisesEvent()
    {
        var sub = WebhookSubscription.Create(Tenant, ValidUrl, ValidEvents, Actor);
        sub.ClearDomainEvents();

        sub.Delete(Actor);

        sub.IsDeleted.Should().BeTrue();
        sub.DomainEvents.Should().ContainSingle(e => e is WebhookSubscriptionDeletedEvent);
    }

    [Fact]
    public void Delete_WhenAlreadyDeleted_Throws()
    {
        var sub = WebhookSubscription.Create(Tenant, ValidUrl, ValidEvents, Actor);
        sub.Delete(Actor);

        var act = () => sub.Delete(Actor);
        act.Should().Throw<WebhookDomainException>().WithMessage("*already deleted*");
    }
}
