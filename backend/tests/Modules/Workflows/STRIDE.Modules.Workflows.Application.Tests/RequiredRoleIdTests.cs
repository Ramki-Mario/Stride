using Microsoft.Extensions.Logging.Abstractions;
using STRIDE.Modules.Workflows.Application.Abstractions;
using STRIDE.Modules.Workflows.Application.Commands.CreateWorkflow;
using STRIDE.Modules.Workflows.Application.Queries.GetWorkflowDefinition;
using STRIDE.Modules.Workflows.Domain.Entities;

namespace STRIDE.Modules.Workflows.Application.Tests;

/// <summary>
/// Tests that RequiredRoleId is correctly threaded from the command → domain entity
/// → DTO layer for workflow step definitions and step instances.
/// </summary>
public sealed class RequiredRoleIdTests
{
    // ── CreateWorkflow: RequiredRoleId propagation ────────────────────────────

    [Fact]
    public async Task CreateWorkflow_WhenStepHasRequiredRoleId_StepDefinitionStoresIt()
    {
        var repo      = Substitute.For<IWorkflowDefinitionRepository>();
        var sut       = new CreateWorkflowCommandHandler(repo, NullLogger<CreateWorkflowCommandHandler>.Instance);
        var roleId    = Guid.NewGuid();
        var tenantId  = Guid.NewGuid();

        WorkflowDefinition? saved = null;
        await repo.AddAsync(Arg.Do<WorkflowDefinition>(d => saved = d), Arg.Any<CancellationToken>());

        var cmd = new CreateWorkflowCommand(
            TenantId:    tenantId,
            Name:        "Test WF",
            Description: null,
            CreatedBy:   Guid.NewGuid(),
            Steps:       new[]
            {
                new StepRequest("Step One", null, IsRequired: true, RequiredRoleId: roleId),
            });

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        saved.Should().NotBeNull();
        saved!.Steps.Should().HaveCount(1);
        saved.Steps[0].RequiredRoleId.Should().Be(roleId);
    }

    [Fact]
    public async Task CreateWorkflow_WhenStepHasNoRequiredRoleId_StepDefinitionIsNull()
    {
        var repo     = Substitute.For<IWorkflowDefinitionRepository>();
        var sut      = new CreateWorkflowCommandHandler(repo, NullLogger<CreateWorkflowCommandHandler>.Instance);

        WorkflowDefinition? saved = null;
        await repo.AddAsync(Arg.Do<WorkflowDefinition>(d => saved = d), Arg.Any<CancellationToken>());

        var cmd = new CreateWorkflowCommand(
            TenantId:    Guid.NewGuid(),
            Name:        "WF No Role",
            Description: null,
            CreatedBy:   Guid.NewGuid(),
            Steps:       new[] { new StepRequest("Step A", null) });

        await sut.Handle(cmd, CancellationToken.None);

        saved!.Steps[0].RequiredRoleId.Should().BeNull();
    }

    // ── GetWorkflowDefinition: RequiredRoleId surfaces in DTO ─────────────────

    [Fact]
    public async Task GetWorkflowDefinition_MapsRequiredRoleIdToDto()
    {
        var roleId     = Guid.NewGuid();
        var definition = WorkflowDefinition.Create(Guid.NewGuid(), "Test WF", null, Guid.NewGuid());
        definition.AddStep("Step A", null, isRequired: true, requiredRoleId: roleId);
        definition.AddStep("Step B", null, isRequired: false, requiredRoleId: null);

        var repo = Substitute.For<IWorkflowDefinitionRepository>();
        repo.GetByIdAsync(definition.Id, Arg.Any<CancellationToken>()).Returns(definition);

        var sut    = new GetWorkflowDefinitionQueryHandler(repo, NullLogger<GetWorkflowDefinitionQueryHandler>.Instance);
        var result = await sut.Handle(new GetWorkflowDefinitionQuery(definition.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Steps[0].RequiredRoleId.Should().Be(roleId);
        result.Value.Steps[1].RequiredRoleId.Should().BeNull();
    }

    // ── StepDefinition domain: Update also clears role when null ─────────────

    [Fact]
    public void StepDefinition_Update_ClearsRequiredRoleIdWhenSetToNull()
    {
        var roleId     = Guid.NewGuid();
        var definition = WorkflowDefinition.Create(Guid.NewGuid(), "WF", null, Guid.NewGuid());
        var step       = definition.AddStep("Step One", null, requiredRoleId: roleId);

        step.RequiredRoleId.Should().Be(roleId);

        definition.UpdateStep(step.Id, "Step One", null, isRequired: true, requiredRoleId: null);

        step.RequiredRoleId.Should().BeNull();
    }

    // ── StepInstance: RequiredRoleId propagated from definition at creation ───

    [Fact]
    public void WorkflowInstance_Start_PropagatesRequiredRoleIdToStepInstances()
    {
        var roleId     = Guid.NewGuid();
        var definition = WorkflowDefinition.Create(Guid.NewGuid(), "WF", null, Guid.NewGuid());
        definition.AddStep("Step With Role", null, isRequired: true, requiredRoleId: roleId);
        definition.AddStep("Step No Role",   null, isRequired: true, requiredRoleId: null);
        definition.Activate(Guid.NewGuid());

        var instance = WorkflowInstance.Start(definition, Guid.NewGuid());

        var stepWithRole = instance.Steps.First(s => s.StepName == "Step With Role");
        var stepNoRole   = instance.Steps.First(s => s.StepName == "Step No Role");

        stepWithRole.RequiredRoleId.Should().Be(roleId);
        stepNoRole.RequiredRoleId.Should().BeNull();
    }

    // ── StepInstance.Assign records AssignedAt ────────────────────────────────

    [Fact]
    public void StepInstance_Assign_SetsAssignedAt()
    {
        var definition = WorkflowDefinition.Create(Guid.NewGuid(), "WF", null, Guid.NewGuid());
        definition.AddStep("Step A", null);
        definition.Activate(Guid.NewGuid());

        var instance = WorkflowInstance.Start(definition, Guid.NewGuid());
        var step     = instance.Steps.First();

        step.AssignedAt.Should().BeNull();
        var before = DateTime.UtcNow;

        instance.AssignStep(step.Id, Guid.NewGuid());

        step.AssignedAt.Should().NotBeNull();
        step.AssignedAt.Should().BeOnOrAfter(before);
    }
}
