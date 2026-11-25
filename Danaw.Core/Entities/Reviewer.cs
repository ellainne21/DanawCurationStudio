namespace Danaw.Core.Entities;

public class Reviewer
{
    public int ReviewerId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public ReviewerRole Role { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; } = true;

    // Sign-in credentials. The password itself is never stored, only a salted hash.
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
}