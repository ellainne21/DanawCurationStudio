using Danaw.Core.Abstractions;
using Danaw.Core.Common;
using Danaw.Core.Entities;

namespace Danaw.Core.Services;

public class AuthenticationService : IAuthenticationService
{
    // The same message for every kind of failure, so an attacker cannot tell
    // "this username exists" from "this username does not exist".
    private const string InvalidCredentials = "Incorrect username or password.";

    private readonly IReviewerRepository _reviewers;
    private readonly IPasswordHasher _hasher;
    private readonly Lazy<string> _dummyHash;

    public AuthenticationService(IReviewerRepository reviewers, IPasswordHasher hasher)
    {
        _reviewers = reviewers;
        _hasher = hasher;
        _dummyHash = new Lazy<string>(() => hasher.Hash("not-a-real-password"));
    }

    public async Task<OperationResult<Reviewer>> SignInAsync(string username, string password)
    {
        var name = (username ?? string.Empty).Trim().ToLowerInvariant();
        if (name.Length == 0 || string.IsNullOrEmpty(password))
            return OperationResult<Reviewer>.Fail("Enter your username and password.");

        var reviewer = await _reviewers.GetByUsernameAsync(name);

        // Unknown username: still do the same amount of work, so timing does not give it away.
        if (reviewer is null)
        {
            _hasher.Verify(password, _dummyHash.Value);
            return OperationResult<Reviewer>.Fail(InvalidCredentials);
        }

        if (!_hasher.Verify(password, reviewer.PasswordHash) || !reviewer.IsActive)
            return OperationResult<Reviewer>.Fail(InvalidCredentials);

        // Hand back a copy without the hash, so it never travels through the UI.
        return OperationResult<Reviewer>.Ok(new Reviewer
        {
            ReviewerId = reviewer.ReviewerId,
            FullName = reviewer.FullName,
            Role = reviewer.Role,
            Email = reviewer.Email,
            IsActive = reviewer.IsActive,
            Username = reviewer.Username,
            PasswordHash = string.Empty
        });
    }
}