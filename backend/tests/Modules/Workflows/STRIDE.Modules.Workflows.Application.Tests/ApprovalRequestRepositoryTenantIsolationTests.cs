using MediatR;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.Modules.Workflows.Domain.Entities;
using STRIDE.Modules.Workflows.Domain.Enums;
using STRIDE.Modules.Workflows.Infrastructure.Persistence;
using STRIDE.Modules.Workflows.Infrastructure.Persistence.Repositories;
using Xunit;
using FluentAssertions;

namespace STRIDE.Modules.Workflows.Application.Tests;

/// <summary>
/// Tenant-isolation regression tests for <see cref="ApprovalRequestRepository"/>.
/// Guards against cross-tenant data leakage: a request belonging to Tenant A must
/// never be returned when the ambient <see cref="ITenantContext"/> resolves to Tenant B,
/// even if the caller supplies a valid step-instance id from another tenant.
/// </summary>
public sealed class ApprovalRequestRepositoryTenantIsolationTests : IDisposable
{
    private readonly WorkflowsDbContext _db;

    public ApprovalRequestRepositoryTenantIsolationTests()
    {
        var options = new DbContextOptionsBuilder<WorkflowsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new WorkflowsDbContext(options, Substitute.For<IPublisher>());
    }

    public void Dispose() => _db.Dispose();

    private ApprovalRequestRepository RepoFor(Guid tenantId)
    {
        var tenant = Substitute.For<ITenantContext>();
        tenant.TenantId.Returns(tenantId);
        return new ApprovalRequestRepository(_db, tenant);
    }

    [Fact]
    public async Task GetPendingByStepInstanceId_OtherTenant_ReturnsNull()
    {
        var tenantA        = Guid.NewGuid();
        var tenantB        = Guid.NewGuid();
        var stepInstanceId = Guid.NewGuid();

        var request = ApprovalRequest.Create(
            workflowInstanceId: Guid.NewGuid(),
            stepInstanceId:     stepInstanceId,
            tenantId:           tenantA,
            requestedFromRoleId: Guid.NewGuid(),
            rejectionHandling:   RejectionHandling.HaltWorkflow,
            revertToStepOrder:   null);

        _db.ApprovalRequests.Add(request);
        await _db.SaveChangesAsync();

        // Tenant B queries with Tenant A's valid step-instance id — must see nothing.
        var found = await RepoFor(tenantB).GetPendingByStepInstanceIdAsync(stepInstanceId);

        found.Should().BeNull();
    }

    [Fact]
    public async Task GetPendingByStepInstanceId_SameTenant_ReturnsRequest()
    {
        var tenantA        = Guid.NewGuid();
        var stepInstanceId = Guid.NewGuid();

        var request = ApprovalRequest.Create(
            workflowInstanceId: Guid.NewGuid(),
            stepInstanceId:     stepInstanceId,
            tenantId:           tenantA,
            requestedFromRoleId: Guid.NewGuid(),
            rejectionHandling:   RejectionHandling.HaltWorkflow,
            revertToStepOrder:   null);

        _db.ApprovalRequests.Add(request);
        await _db.SaveChangesAsync();

        var found = await RepoFor(tenantA).GetPendingByStepInstanceIdAsync(stepInstanceId);

        found.Should().NotBeNull();
        found!.TenantId.Should().Be(tenantA);
        found.StepInstanceId.Should().Be(stepInstanceId);
    }
}
