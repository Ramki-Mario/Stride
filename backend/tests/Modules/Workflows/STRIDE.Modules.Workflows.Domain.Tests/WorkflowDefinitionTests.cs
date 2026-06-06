using FluentAssertions;
using STRIDE.Modules.Workflows.Domain.Entities;
using STRIDE.Modules.Workflows.Domain.Enums;
using STRIDE.Modules.Workflows.Domain.Events;
using STRIDE.Modules.Workflows.Domain.Exceptions;
using STRIDE.Modules.Workflows.Domain.Tests.Builders;

namespace STRIDE.Modules.Workflows.Domain.Tests;

public sealed class WorkflowDefinitionTests
{
    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public void Create_WithValidName_ReturnsDraftDefinition()
    {
        // Arrange
        var tenantId   = Guid.NewGuid();
        var createdBy  = Guid.NewGuid();

        // Act
        var definition = WorkflowDefinition.Create(tenantId, "Onboarding Workflow", null, createdBy);

        // Assert
        definition.Id.Should().NotBeEmpty();
        definition.Name.Should().Be("Onboarding Workflow");
        definition.Status.Should().Be(WorkflowStatus.Draft);
        definition.TenantId.Should().Be(tenantId);
        definition.IsDeleted.Should().BeFalse();
        definition.Steps.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyOrWhitespaceName_ThrowsWorkflowDomainException(string name)
    {
        // Act
        var act = () => WorkflowDefinition.Create(Guid.NewGuid(), name, null, Guid.NewGuid());

        // Assert
        act.Should().Throw<WorkflowDomainException>()
            .WithMessage("*name*");
    }

    [Fact]
    public void Create_RaisesWorkflowCreatedEvent()
    {
        // Arrange
        var tenantId  = Guid.NewGuid();
        var createdBy = Guid.NewGuid();

        // Act
        var definition = WorkflowDefinition.Create(tenantId, "My Workflow", null, createdBy);

        // Assert
        definition.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<WorkflowCreatedEvent>();

        var evt = (WorkflowCreatedEvent)definition.DomainEvents[0];
        evt.WorkflowDefinitionId.Should().Be(definition.Id);
        evt.TenantId.Should().Be(tenantId);
        evt.CreatedBy.Should().Be(createdBy);
    }

    [Fact]
    public void Create_TrimsNameAndDescription()
    {
        // Act
        var definition = WorkflowDefinition.Create(
            Guid.NewGuid(), "  My Workflow  ", "  Some desc  ", Guid.NewGuid());

        // Assert
        definition.Name.Should().Be("My Workflow");
        definition.Description.Should().Be("Some desc");
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [Fact]
    public void Update_WhenDraft_UpdatesNameAndDescription()
    {
        // Arrange
        var definition = WorkflowDefinitionBuilder.DraftWithOneStep();

        // Act
        definition.Update("New Name", "New Description");

        // Assert
        definition.Name.Should().Be("New Name");
        definition.Description.Should().Be("New Description");
    }

    [Fact]
    public void Update_WhenNotDraft_ThrowsWorkflowDomainException()
    {
        // Arrange
        var definition = WorkflowDefinitionBuilder.Active();

        // Act
        var act = () => definition.Update("New Name", null);

        // Assert
        act.Should().Throw<WorkflowDomainException>()
            .WithMessage("*Draft*");
    }

    // ── AddStep ───────────────────────────────────────────────────────────────

    [Fact]
    public void AddStep_WhenDraft_IncreasesStepCount()
    {
        // Arrange
        var definition = new WorkflowDefinitionBuilder().Build();

        // Act
        definition.AddStep("Review", "Review the document");
        definition.AddStep("Approve", "Final approval");

        // Assert
        definition.Steps.Should().HaveCount(2);
        definition.Steps[0].Name.Should().Be("Review");
        definition.Steps[1].Name.Should().Be("Approve");
        definition.Steps[0].Order.Should().Be(0);
        definition.Steps[1].Order.Should().Be(1);
    }

    [Fact]
    public void AddStep_WhenNotDraft_ThrowsWorkflowDomainException()
    {
        // Arrange
        var definition = WorkflowDefinitionBuilder.Active();

        // Act
        var act = () => definition.AddStep("New Step", null);

        // Assert
        act.Should().Throw<WorkflowDomainException>()
            .WithMessage("*Draft*");
    }

    // ── Activate ──────────────────────────────────────────────────────────────

    [Fact]
    public void Activate_WhenDraftWithSteps_SetsStatusToActive()
    {
        // Arrange
        var definition = WorkflowDefinitionBuilder.DraftWithOneStep();

        // Act
        definition.Activate(Guid.NewGuid());

        // Assert
        definition.Status.Should().Be(WorkflowStatus.Active);
    }

    [Fact]
    public void Activate_WhenDraftWithNoSteps_ThrowsWorkflowDomainException()
    {
        // Arrange
        var definition = new WorkflowDefinitionBuilder().Build(); // no steps

        // Act
        var act = () => definition.Activate(Guid.NewGuid());

        // Assert
        act.Should().Throw<WorkflowDomainException>()
            .WithMessage("*at least one step*");
    }

    [Fact]
    public void Activate_WhenAlreadyActive_ThrowsWorkflowDomainException()
    {
        // Arrange
        var definition = WorkflowDefinitionBuilder.Active();

        // Act
        var act = () => definition.Activate(Guid.NewGuid());

        // Assert
        act.Should().Throw<WorkflowDomainException>()
            .WithMessage("*Draft*");
    }

    [Fact]
    public void Activate_RaisesWorkflowActivatedEvent()
    {
        // Arrange
        var definition  = WorkflowDefinitionBuilder.DraftWithOneStep();
        var activatedBy = Guid.NewGuid();

        // Act
        definition.Activate(activatedBy);

        // Assert
        definition.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<WorkflowActivatedEvent>();

        var evt = (WorkflowActivatedEvent)definition.DomainEvents[0];
        evt.ActivatedBy.Should().Be(activatedBy);
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    [Fact]
    public void Delete_WhenDraft_SetsIsDeletedTrue()
    {
        // Arrange
        var definition = WorkflowDefinitionBuilder.DraftWithOneStep();

        // Act
        definition.Delete(Guid.NewGuid());

        // Assert
        definition.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public void Delete_WhenNotDraft_ThrowsWorkflowDomainException()
    {
        // Arrange
        var definition = WorkflowDefinitionBuilder.Active();

        // Act
        var act = () => definition.Delete(Guid.NewGuid());

        // Assert
        act.Should().Throw<WorkflowDomainException>()
            .WithMessage("*Draft*");
    }

    // ── Archive ───────────────────────────────────────────────────────────────

    [Fact]
    public void Archive_WhenActive_SetsStatusToArchived()
    {
        // Arrange
        var definition = WorkflowDefinitionBuilder.Active();

        // Act
        definition.Archive();

        // Assert
        definition.Status.Should().Be(WorkflowStatus.Archived);
    }
}
