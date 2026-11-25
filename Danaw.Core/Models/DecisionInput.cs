using Danaw.Core.Entities;

namespace Danaw.Core.Models;

// The validator's verdict on one source: verified or not, plus notes.
public record SourceCheckInput(int SourceId, bool IsVerified, string? CrossCheckNotes);

// Everything the validator submits when pressing Approve / Request Revision / Reject.
public record DecisionInput(
    int ContentItemId,
    ValidationDecision Decision,
    string? Comments,
    IReadOnlyList<SourceCheckInput> SourceChecks);