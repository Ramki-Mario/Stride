using STRIDE.Modules.Identity.Domain.Entities;

namespace STRIDE.Modules.Identity.Application.Abstractions;

public interface IInviteTokenRepository
{
    Task AddAsync(InviteToken token, CancellationToken ct = default);
    Task<InviteToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
