using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.Workflows.Application.Commands.SignOffPublicJob;

public sealed record SignOffPublicJobCommand(string Token, string ClientName)
    : IRequest<Result>;
