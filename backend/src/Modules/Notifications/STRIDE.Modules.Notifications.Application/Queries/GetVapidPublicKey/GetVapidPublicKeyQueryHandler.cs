using MediatR;
using Microsoft.Extensions.Configuration;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.Notifications.Application.Queries.GetVapidPublicKey;

internal sealed class GetVapidPublicKeyQueryHandler
    : IRequestHandler<GetVapidPublicKeyQuery, Result<string>>
{
    private readonly IConfiguration _configuration;

    public GetVapidPublicKeyQueryHandler(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Task<Result<string>> Handle(GetVapidPublicKeyQuery request, CancellationToken cancellationToken)
    {
        var publicKey = _configuration["Vapid:PublicKey"];

        if (string.IsNullOrWhiteSpace(publicKey))
            return Task.FromResult(Result.Failure<string>("VAPID public key is not configured."));

        return Task.FromResult(Result.Success(publicKey));
    }
}
