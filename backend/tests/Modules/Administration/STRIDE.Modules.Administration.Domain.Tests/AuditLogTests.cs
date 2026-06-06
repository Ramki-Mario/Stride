using FluentAssertions;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.Modules.Administration.Domain.Entities;

namespace STRIDE.Modules.Administration.Domain.Tests;

public sealed class AuditLogTests
{
    [Fact]
    public void Create_WithAllFields_PopulatesPropertiesCorrectly()
    {
        // Arrange
        var tenantId     = Guid.NewGuid();
        var actorId      = Guid.NewGuid();
        var resourceId   = Guid.NewGuid();
        const string email        = "admin@company.com";
        const string action       = AuditActions.UserInvited;
        const string resourceType = "User";
        const string oldJson      = "{\"role\":\"Member\"}";
        const string newJson      = "{\"role\":\"Admin\"}";

        // Act
        var before = DateTime.UtcNow;
        var log    = AuditLog.Create(new AuditLogData(
                         TenantId:     tenantId,
                         ActorId:      actorId,
                         ActorEmail:   email,
                         Action:       action,
                         ResourceType: resourceType,
                         ResourceId:   resourceId,
                         OldValueJson: oldJson,
                         NewValueJson: newJson));
        var after  = DateTime.UtcNow;

        // Assert
        log.Id.Should().NotBeEmpty();
        log.TenantId.Should().Be(tenantId);
        log.ActorId.Should().Be(actorId);
        log.ActorEmail.Should().Be(email);
        log.Action.Should().Be(action);
        log.ResourceType.Should().Be(resourceType);
        log.ResourceId.Should().Be(resourceId);
        log.OldValueJson.Should().Be(oldJson);
        log.NewValueJson.Should().Be(newJson);
        log.Timestamp.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void Create_WithOptionalFieldsOmitted_SetsThemToNull()
    {
        // Act
        var log = AuditLog.Create(new AuditLogData(
            TenantId:     Guid.NewGuid(),
            ActorId:      Guid.NewGuid(),
            ActorEmail:   "user@test.com",
            Action:       AuditActions.UserDeactivated,
            ResourceType: "User"));

        // Assert
        log.ResourceId.Should().BeNull();
        log.OldValueJson.Should().BeNull();
        log.NewValueJson.Should().BeNull();
    }

    [Fact]
    public void Create_EachCall_GeneratesUniqueId()
    {
        // Act
        var log1 = AuditLog.Create(new AuditLogData(Guid.NewGuid(), Guid.NewGuid(), "a@b.com", "action", "Type"));
        var log2 = AuditLog.Create(new AuditLogData(Guid.NewGuid(), Guid.NewGuid(), "a@b.com", "action", "Type"));

        // Assert
        log1.Id.Should().NotBe(log2.Id);
    }

    [Fact]
    public void Create_Timestamp_IsUtc()
    {
        // Act
        var log = AuditLog.Create(new AuditLogData(Guid.NewGuid(), Guid.NewGuid(), "a@b.com", "action", "Type"));

        // Assert
        log.Timestamp.Kind.Should().Be(DateTimeKind.Utc);
    }
}
