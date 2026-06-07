using STRIDE.Modules.Clients.Domain.Entities;
using STRIDE.Modules.Clients.Domain.Enums;
using STRIDE.Modules.Clients.Domain.Events;
using STRIDE.Modules.Clients.Domain.Exceptions;
using STRIDE.Modules.Clients.Domain.Tests.Builders;

namespace STRIDE.Modules.Clients.Domain.Tests;

public sealed class ClientTests
{
    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public void Create_WithValidData_ReturnsActiveClient()
    {
        // Arrange
        var tenantId  = Guid.NewGuid();
        var createdBy = Guid.NewGuid();

        // Act
        var client = Client.Create(new NewClient(
            TenantId:      tenantId,
            Name:          "Acme Ltd",
            ContactPerson: "Alice",
            Email:         "alice@acme.com",
            Phone:         "07000000000",
            Address:       "1 High Street",
            Notes:         "VIP client.",
            CreatedBy:     createdBy));

        // Assert
        client.Id.Should().NotBeEmpty();
        client.TenantId.Should().Be(tenantId);
        client.Name.Should().Be("Acme Ltd");
        client.ContactPerson.Should().Be("Alice");
        client.Email.Should().Be("alice@acme.com");
        client.Phone.Should().Be("07000000000");
        client.Address.Should().Be("1 High Street");
        client.Notes.Should().Be("VIP client.");
        client.Status.Should().Be(ClientStatus.Active);
        client.IsDeleted.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyName_ThrowsClientDomainException(string name)
    {
        var act = () => Client.Create(new NewClient(
            Guid.NewGuid(), name, null, null, null, null, null, Guid.NewGuid()));

        act.Should().Throw<ClientDomainException>()
            .WithMessage("*name*required*");
    }

    [Fact]
    public void Create_WithInvalidEmail_ThrowsClientDomainException()
    {
        var act = () => Client.Create(new NewClient(
            Guid.NewGuid(), "Acme", null, "not-an-email", null, null, null, Guid.NewGuid()));

        act.Should().Throw<ClientDomainException>()
            .WithMessage("*email*not valid*");
    }

    [Fact]
    public void Create_NormalisesEmailToLowercase()
    {
        var client = Client.Create(new NewClient(
            Guid.NewGuid(), "Acme", null, "BILLING@ACME.COM", null, null, null, Guid.NewGuid()));

        client.Email.Should().Be("billing@acme.com");
    }

    [Fact]
    public void Create_WithNullEmail_DoesNotThrow()
    {
        var act = () => new ClientBuilder().WithEmail(null).Build();

        act.Should().NotThrow();
    }

    [Fact]
    public void Create_RaisesClientCreatedEvent()
    {
        var client = new ClientBuilder().Build();

        client.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ClientCreatedEvent>();
    }

    [Fact]
    public void Create_WithOnlyName_CreatesClientWithNullOptionalFields()
    {
        var client = new ClientBuilder().WithNoOptionalFields().Build();

        client.ContactPerson.Should().BeNull();
        client.Email.Should().BeNull();
        client.Phone.Should().BeNull();
        client.Address.Should().BeNull();
        client.Notes.Should().BeNull();
        client.Status.Should().Be(ClientStatus.Active);
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [Fact]
    public void Update_WithValidData_UpdatesProperties()
    {
        var client    = new ClientBuilder().Build();
        var updatedBy = Guid.NewGuid();

        client.Update("New Name", "Bob", "bob@example.com", null, "New Address", "Updated notes.", updatedBy);

        client.Name.Should().Be("New Name");
        client.ContactPerson.Should().Be("Bob");
        client.Email.Should().Be("bob@example.com");
        client.Phone.Should().BeNull();
        client.Address.Should().Be("New Address");
        client.Notes.Should().Be("Updated notes.");
    }

    [Fact]
    public void Update_WithEmptyName_ThrowsClientDomainException()
    {
        var client = new ClientBuilder().Build();

        var act = () => client.Update("", null, null, null, null, null, Guid.NewGuid());

        act.Should().Throw<ClientDomainException>()
            .WithMessage("*name*required*");
    }

    [Fact]
    public void Update_RaisesClientUpdatedEvent()
    {
        var client = new ClientBuilder().Build();
        client.ClearDomainEvents();

        client.Update("Updated Name", null, null, null, null, null, Guid.NewGuid());

        client.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ClientUpdatedEvent>();
    }

    // ── Deactivate ────────────────────────────────────────────────────────────

    [Fact]
    public void Deactivate_WhenActive_SetsStatusToInactive()
    {
        var client = new ClientBuilder().Build();
        client.ClearDomainEvents();

        client.Deactivate(Guid.NewGuid());

        client.Status.Should().Be(ClientStatus.Inactive);
    }

    [Fact]
    public void Deactivate_WhenAlreadyInactive_ThrowsClientDomainException()
    {
        var client = new ClientBuilder().Build();
        client.Deactivate(Guid.NewGuid());
        client.ClearDomainEvents();

        var act = () => client.Deactivate(Guid.NewGuid());

        act.Should().Throw<ClientDomainException>()
            .WithMessage("*already inactive*");
    }

    [Fact]
    public void Deactivate_RaisesClientDeactivatedEvent()
    {
        var client = new ClientBuilder().Build();
        client.ClearDomainEvents();

        client.Deactivate(Guid.NewGuid());

        client.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ClientDeactivatedEvent>();
    }

    // ── Reactivate ────────────────────────────────────────────────────────────

    [Fact]
    public void Reactivate_WhenInactive_SetsStatusToActive()
    {
        var client = new ClientBuilder().Build();
        client.Deactivate(Guid.NewGuid());
        client.ClearDomainEvents();

        client.Reactivate(Guid.NewGuid());

        client.Status.Should().Be(ClientStatus.Active);
    }

    [Fact]
    public void Reactivate_WhenAlreadyActive_ThrowsClientDomainException()
    {
        var client = new ClientBuilder().Build();
        client.ClearDomainEvents();

        var act = () => client.Reactivate(Guid.NewGuid());

        act.Should().Throw<ClientDomainException>()
            .WithMessage("*already active*");
    }
}
