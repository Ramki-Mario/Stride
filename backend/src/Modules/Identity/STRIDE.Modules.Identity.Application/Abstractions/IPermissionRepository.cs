using STRIDE.Modules.Identity.Domain.Entities;

namespace STRIDE.Modules.Identity.Application.Abstractions;

public interface IPermissionRepository
{
    /// <summary>Returns all system-wide permissions from the catalog.</summary>
    Task<IReadOnlyList<Permission>> GetAllAsync(CancellationToken cancellationToken = default);
}
