namespace Danaw.Core.Entities;

public enum ContentStatus
{
    Draft,
    Pending,
    NeedsRevision,
    Validated,
    Rejected
}

public enum ReviewerRole
{
    Researcher,
    Validator
}

public enum ValidationDecision
{
    Approved,
    RevisionRequested,
    Rejected
}
