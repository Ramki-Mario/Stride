using MediatR;
using Microsoft.Extensions.Logging;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Workflows.Application.Abstractions;
using STRIDE.Modules.Workflows.Domain.Enums;

namespace STRIDE.Modules.Workflows.Application.Commands.SignOffPublicJob;

internal sealed class SignOffPublicJobCommandHandler
    : IRequestHandler<SignOffPublicJobCommand, Result>
{
    private const string GoneError      = "This link has expired or been revoked.";
    private const string NotCompleted   = "Sign-off is only available once the job has completed.";

    private readonly ISharedWorkflowLinkRepository           _links;
    private readonly IWorkflowInstanceRepository             _instances;
    private readonly ILogger<SignOffPublicJobCommandHandler> _logger;

    public SignOffPublicJobCommandHandler(
        ISharedWorkflowLinkRepository            links,
        IWorkflowInstanceRepository              instances,
        ILogger<SignOffPublicJobCommandHandler>  logger)
    {
        _links     = links;
        _instances = instances;
        _logger    = logger;
    }

    public async Task<Result> Handle(
        SignOffPublicJobCommand request,
        CancellationToken       cancellationToken)
    {
        var link = await _links.GetByTokenAsync(request.Token, cancellationToken);

        if (link is null || !link.IsValid(DateTime.UtcNow))
        {
            _logger.LogInformation(
                "Public sign-off: token resolved to {State}",
                link is null ? "not found" : (link.IsRevoked ? "revoked" : "expired"));
            return Result.Failure(GoneError);
        }

        var instance = await _instances.GetByIdPublicAsync(
            link.WorkflowInstanceId, link.TenantId, cancellationToken);

        if (instance is null)
        {
            _logger.LogWarning(
                "Public sign-off: link {LinkId} points to missing instance {InstanceId}",
                link.Id, link.WorkflowInstanceId);
            return Result.Failure(GoneError);
        }

        if (instance.Status != WorkflowStatus.Completed)
            return Result.Failure(NotCompleted);

        // Idempotent — SignOff() is a no-op if already signed off.
        link.SignOff(request.ClientName);
        _links.Update(link);
        await _links.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
