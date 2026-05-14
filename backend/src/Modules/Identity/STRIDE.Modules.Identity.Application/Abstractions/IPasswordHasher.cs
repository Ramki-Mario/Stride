using STRIDE.Modules.Identity.Domain.ValueObjects;

namespace STRIDE.Modules.Identity.Application.Abstractions;

public interface IPasswordHasher
{
    Password Hash(string plainTextPassword);
    bool Verify(string plainTextPassword, Password passwordHash);
}
