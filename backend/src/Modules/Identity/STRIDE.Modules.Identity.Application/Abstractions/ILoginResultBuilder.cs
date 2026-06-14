using STRIDE.Modules.Identity.Application.Commands.LoginUser;
using STRIDE.Modules.Identity.Domain.Entities;

namespace STRIDE.Modules.Identity.Application.Abstractions;

public interface ILoginResultBuilder
{
    Task<LoginResult> BuildAsync(User user, Guid tenantId, CancellationToken ct = default);
}
