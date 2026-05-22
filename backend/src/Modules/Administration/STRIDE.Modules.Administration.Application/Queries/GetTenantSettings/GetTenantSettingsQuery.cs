using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Administration.Application.DTOs;

namespace STRIDE.Modules.Administration.Application.Queries.GetTenantSettings;

public sealed record GetTenantSettingsQuery(Guid TenantId) : IRequest<Result<TenantSettingsDto>>;
