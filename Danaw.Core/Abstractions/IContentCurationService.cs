using Danaw.Core.Common;
using Danaw.Core.Models;

namespace Danaw.Core.Abstractions;

public interface IContentCurationService
{
    // Saves the item as a Draft (light validation). Returns the item's id.
    Task<OperationResult<int>> SaveDraftAsync(int reviewerId, ContentItemInput input);

    // Full validation, then the item becomes Pending. Returns the item's id.
    Task<OperationResult<int>> SubmitAsync(int reviewerId, ContentItemInput input);
}