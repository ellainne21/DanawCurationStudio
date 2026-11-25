using Danaw.Core.Entities;
using Danaw.Core.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Danaw.Tests;

public class AuthenticationServiceTests
{
    [Fact]
    public async Task Researcher_with_correct_password_signs_in_without_exposing_the_hash()
    {
        using var db = new TestDb();
        using var ctx = db.NewContext();

        var result = await db.NewAuthenticationService(ctx).SignInAsync("balindong", "Balindong-Demo-2026");

        Assert.True(result.Success, string.Join("; ", result.Errors));
        Assert.Equal(1, result.Value!.ReviewerId);
        Assert.Equal(ReviewerRole.Researcher, result.Value.Role);
        Assert.Equal(string.Empty, result.Value.PasswordHash);
    }

    [Fact]
    public async Task Validator_with_correct_password_signs_in_with_the_validator_role()
    {
        using var db = new TestDb();
        using var ctx = db.NewContext();

        var result = await db.NewAuthenticationService(ctx).SignInAsync("validator", "BCPCH-Validator-Demo-2026");

        Assert.True(result.Success, string.Join("; ", result.Errors));
        Assert.Equal(ReviewerRole.Validator, result.Value!.Role);
    }

    [Fact]
    public async Task Wrong_password_is_rejected_and_no_user_is_returned()
    {
        using var db = new TestDb();
        using var ctx = db.NewContext();

        var result = await db.NewAuthenticationService(ctx).SignInAsync("balindong", "wrong-password");

        Assert.False(result.Success);
        Assert.Null(result.Value);
        Assert.Contains("Incorrect username or password", result.Errors[0]);
    }

    [Fact]
    public async Task Unknown_username_gives_exactly_the_same_message_as_a_wrong_password()
    {
        using var db = new TestDb();
        using var ctx = db.NewContext();
        var service = db.NewAuthenticationService(ctx);

        var wrongPassword = await service.SignInAsync("balindong", "wrong-password");
        var unknownUser = await service.SignInAsync("nobody", "wrong-password");

        Assert.False(unknownUser.Success);
        Assert.Equal(wrongPassword.Errors[0], unknownUser.Errors[0]); // does not reveal which part was wrong
    }

    [Fact]
    public async Task Username_is_not_case_sensitive_and_surrounding_spaces_are_ignored()
    {
        using var db = new TestDb();
        using var ctx = db.NewContext();

        var result = await db.NewAuthenticationService(ctx).SignInAsync("  CABASAG ", "Cabasag-Demo-2026");

        Assert.True(result.Success, string.Join("; ", result.Errors));
        Assert.Equal(3, result.Value!.ReviewerId);
    }

    [Fact]
    public async Task Inactive_account_cannot_sign_in_even_with_the_correct_password()
    {
        using var db = new TestDb();

        using (var ctx = db.NewContext())
        {
            var reviewer = await ctx.Reviewers.SingleAsync(r => r.Username == "balofinos");
            reviewer.IsActive = false;
            await ctx.SaveChangesAsync();
        }

        using var check = db.NewContext();
        var result = await db.NewAuthenticationService(check).SignInAsync("balofinos", "Balofinos-Demo-2026");

        Assert.False(result.Success);
    }

    [Theory]
    [InlineData("", "Balindong-Demo-2026")]
    [InlineData("balindong", "")]
    [InlineData("   ", "   ")]
    public async Task Blank_username_or_password_is_rejected(string username, string password)
    {
        using var db = new TestDb();
        using var ctx = db.NewContext();

        var result = await db.NewAuthenticationService(ctx).SignInAsync(username, password);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task Seeded_passwords_are_stored_as_hashes_not_plain_text()
    {
        using var db = new TestDb();
        using var ctx = db.NewContext();

        var reviewers = await ctx.Reviewers.ToListAsync();

        Assert.Equal(4, reviewers.Count);
        Assert.All(reviewers, r =>
        {
            Assert.StartsWith("PBKDF2-SHA256$", r.PasswordHash);
            Assert.DoesNotContain("Demo-2026", r.PasswordHash);
        });
    }

    [Fact]
    public void Hashing_the_same_password_twice_gives_different_values_that_both_verify()
    {
        var hasher = new PasswordHasher();

        var first = hasher.Hash("Some-Password-1");
        var second = hasher.Hash("Some-Password-1");

        Assert.NotEqual(first, second); // a random salt per password
        Assert.True(hasher.Verify("Some-Password-1", first));
        Assert.True(hasher.Verify("Some-Password-1", second));
        Assert.False(hasher.Verify("Some-Password-2", first));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-hash")]
    [InlineData("PBKDF2-SHA256$600000$!!!$???")]
    public void A_damaged_stored_hash_never_matches_and_never_crashes(string stored)
    {
        Assert.False(new PasswordHasher().Verify("anything", stored));
    }
}