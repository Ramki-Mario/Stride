namespace STRIDE.Modules.Identity.Application.Abstractions;

public interface ISuperAdminRepository
{
    Task<bool> IsSuperAdminAsync(string email, CancellationToken cancellationToken = default);
}
