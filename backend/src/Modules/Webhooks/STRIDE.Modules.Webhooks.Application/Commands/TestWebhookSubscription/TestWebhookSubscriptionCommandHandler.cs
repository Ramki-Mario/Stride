using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Webhooks.Application.Abstractions;
using STRIDE.Modules.Webhooks.Domain.Repositories;

namespace STRIDE.Modules.Webhooks.Application.Commands.TestWebhookSubscription;

internal sealed class TestWebhookSubscriptionCommandHandler
    : IRequestHandler<TestWebhookSubscriptionCommand, Result<WebhookTestResult>>
{
    private readonly IWebhookSubscriptionRepository _repo;
    private readonly IWebhookTester                 _tester;

    public TestWebhookSubscriptionCommandHandler(
        IWebhookSubscriptionRepository repo, IWebhookTester tester)
    {
        _repo   = repo;
        _tester = tester;
    }

    public async Task<Result<WebhookTestResult>> Handle(
        TestWebhookSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var sub = await _repo.GetByIdAsync(request.TenantId, request.SubscriptionId, cancellationToken);
        if (sub is null)
            return Result<WebhookTestResult>.Failure("Webhook subscription not found.");

        var result = await _tester.SendPingAsync(sub.Url, sub.SigningSecret, cancellationToken);
        return Result<WebhookTestResult>.Success(result);
    }
}
