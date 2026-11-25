using Danaw.Core.Common;
using Danaw.Core.Entities;

namespace Danaw.Core.Abstractions;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string storedHash);
}

public interface IAuthenticationService
{
    // On success, returns the signed-in reviewer (without the password hash).
    Task<OperationResult<Reviewer>> SignInAsync(string username, string password);
}