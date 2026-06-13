using MediatR;
using STRIDE.Modules.Webhooks.Application.DTOs;
using STRIDE.Modules.Webhooks.Domain;

namespace STRIDE.Modules.Webhooks.Application.Queries.GetWebhookEventTypes;

internal sealed class GetWebhookEventTypesQueryHandler
    : IRequestHandler<GetWebhookEventTypesQuery, IReadOnlyList<WebhookEventTypeDto>>
{
    /// <summary>Human-readable labels for each event key, surfaced as checkbox labels in the UI.</summary>
    private static readonly IReadOnlyDictionary<string, string> Labels = new Dictionary<string, string>
    {
        [WebhookEventTypes.WorkflowInstanceStarted]   = "Workflow started",
        [WebhookEventTypes.WorkflowInstanceCompleted] = "Workflow completed",
        [WebhookEventTypes.WorkflowStepCompleted]     = "Step completed",
        [WebhookEventTypes.WorkflowStepOverdue]       = "Step overdue",
        [WebhookEventTypes.InvoiceCreated]            = "Invoice created",
        [WebhookEventTypes.InvoiceSent]               = "Invoice sent",
        [WebhookEventTypes.UserInvited]               = "User invited",
    };

    public Task<IReadOnlyList<WebhookEventTypeDto>> Handle(
        GetWebhookEventTypesQuery request, CancellationToken cancellationToken)
    {
        IReadOnlyList<WebhookEventTypeDto> result = WebhookEventTypes.All
            .Select(key => new WebhookEventTypeDto(key, Labels.GetValueOrDefault(key, key)))
            .ToList();

        return Task.FromResult(result);
    }
}
