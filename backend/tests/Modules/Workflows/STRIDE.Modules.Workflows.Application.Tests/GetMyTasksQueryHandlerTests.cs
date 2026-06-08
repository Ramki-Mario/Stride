using STRIDE.Modules.Workflows.Application.Abstractions;
using STRIDE.Modules.Workflows.Application.Queries.GetMyTasks;

namespace STRIDE.Modules.Workflows.Application.Tests;

/// <summary>
/// Tests for <see cref="GetMyTasksQueryHandler"/> — verifies the handler
/// delegates to IWorkflowReadService and maps MyTaskReadModel → MyTaskDto correctly.
/// </summary>
public sealed class GetMyTasksQueryHandlerTests
{
    private readonly IWorkflowReadService _readService = Substitute.For<IWorkflowReadService>();
    private readonly GetMyTasksQueryHandler _sut;

    public GetMyTasksQueryHandlerTests()
    {
        _sut = new GetMyTasksQueryHandler(_readService);
    }

    // ── Empty list ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenNoTasksAssigned_ReturnsEmptyList()
    {
        var userId   = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        _readService.GetMyTasksAsync(userId, tenantId, Arg.Any<CancellationToken>())
                    .Returns(Array.Empty<MyTaskReadModel>());

        var result = await _sut.Handle(
            new GetMyTasksQuery(userId, tenantId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    // ── Field mapping ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_MapsAllReadModelFieldsToDto()
    {
        var userId     = Guid.NewGuid();
        var tenantId   = Guid.NewGuid();
        var assignedAt = DateTime.UtcNow.AddHours(-2);

        var readModel = new MyTaskReadModel(
            StepInstanceId:    Guid.NewGuid(),
            StepName:          "Review Contract",
            StepStatus:        "Assigned",
            AssignedAt:        assignedAt,
            WorkflowInstanceId: Guid.NewGuid(),
            WorkflowName:      "Client Onboarding",
            WorkflowStatus:    "Running",
            ClientName:        "Acme Corp");

        _readService.GetMyTasksAsync(userId, tenantId, Arg.Any<CancellationToken>())
                    .Returns(new[] { readModel });

        var result = await _sut.Handle(
            new GetMyTasksQuery(userId, tenantId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var dto = result.Value.Single();

        dto.StepInstanceId.Should().Be(readModel.StepInstanceId);
        dto.StepName.Should().Be("Review Contract");
        dto.StepStatus.Should().Be("Assigned");
        dto.AssignedAt.Should().Be(assignedAt);
        dto.WorkflowInstanceId.Should().Be(readModel.WorkflowInstanceId);
        dto.WorkflowName.Should().Be("Client Onboarding");
        dto.WorkflowStatus.Should().Be("Running");
        dto.ClientName.Should().Be("Acme Corp");
    }

    // ── Null ClientName is preserved ──────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenClientNameIsNull_DtoClientNameIsNull()
    {
        var userId   = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var readModel = new MyTaskReadModel(
            StepInstanceId:    Guid.NewGuid(),
            StepName:          "Verify Documents",
            StepStatus:        "Pending",
            AssignedAt:        null,
            WorkflowInstanceId: Guid.NewGuid(),
            WorkflowName:      "Internal Process",
            WorkflowStatus:    "Running",
            ClientName:        null);

        _readService.GetMyTasksAsync(userId, tenantId, Arg.Any<CancellationToken>())
                    .Returns(new[] { readModel });

        var result = await _sut.Handle(
            new GetMyTasksQuery(userId, tenantId),
            CancellationToken.None);

        result.Value.Single().ClientName.Should().BeNull();
    }

    // ── Multiple tasks returned in order ──────────────────────────────────────

    [Fact]
    public async Task Handle_WithMultipleTasks_ReturnsSameCount()
    {
        var userId   = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var readModels = Enumerable.Range(1, 3).Select(i => new MyTaskReadModel(
            StepInstanceId:    Guid.NewGuid(),
            StepName:          $"Step {i}",
            StepStatus:        "Pending",
            AssignedAt:        DateTime.UtcNow.AddMinutes(-i * 10),
            WorkflowInstanceId: Guid.NewGuid(),
            WorkflowName:      $"Workflow {i}",
            WorkflowStatus:    "Running",
            ClientName:        null)).ToArray();

        _readService.GetMyTasksAsync(userId, tenantId, Arg.Any<CancellationToken>())
                    .Returns(readModels);

        var result = await _sut.Handle(
            new GetMyTasksQuery(userId, tenantId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
    }

    // ── Query params are forwarded to read service ────────────────────────────

    [Fact]
    public async Task Handle_PassesCorrectUserIdAndTenantId_ToReadService()
    {
        var userId   = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        _readService.GetMyTasksAsync(userId, tenantId, Arg.Any<CancellationToken>())
                    .Returns(Array.Empty<MyTaskReadModel>());

        await _sut.Handle(new GetMyTasksQuery(userId, tenantId), CancellationToken.None);

        await _readService.Received(1).GetMyTasksAsync(
            userId,
            tenantId,
            Arg.Any<CancellationToken>());
    }
}
