using SmartMarketingPlatform.Common.Errors;

namespace SmartMarketingPlatform.Common.Statuses;

// Allowed status transitions of one entity, e.g. Active -> Paused, but Closed -> nothing.
public sealed class StatusFlow(string entityName, IReadOnlyDictionary<int, int[]> allowed)
{
    // Moving to the same status is a no-op (PATCH is idempotent); a forbidden move is a 409.
    public void EnsureCanMove(int from, int to, StatusCatalog statuses)
    {
        if (from == to) return;
        if (allowed.TryGetValue(from, out var next) && next.Contains(to)) return;

        throw new ConflictException(
            $"A {entityName} cannot move from {statuses.CodeOf(from)} to {statuses.CodeOf(to)}.");
    }
}
