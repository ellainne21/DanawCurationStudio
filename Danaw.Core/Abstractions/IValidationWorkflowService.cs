using Danaw.Core.Common;
using Danaw.Core.Models;

namespace Danaw.Core.Abstractions;

public interface IValidationWorkflowService
{
    // Applies the validator's source checks, records the decision and updates the
    // item's status as ONE all-or-nothing change (the Option A transaction).
    Task<OperationResult> RecordDecisionAsync(int reviewerId, DecisionInput input);
}