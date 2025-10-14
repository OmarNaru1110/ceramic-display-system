using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using DATA.Models.Contract;

namespace DATA.DataAccess.Context.Interceptors
{
    public class SoftDeleteInterceptor : SaveChangesInterceptor
    {
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (eventData.Context is null)
                return result;

            ProcessSoftDeletes(eventData.Context);
            return result;
        }

        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData,
            InterceptionResult<int> result)
        {
            if (eventData.Context is null)
                return result;

            ProcessSoftDeletes(eventData.Context);
            return result;
        }

        private static void ProcessSoftDeletes(DbContext context)
        {
            foreach (var entry in context.ChangeTracker.Entries())
            {
                if (entry.State != EntityState.Deleted || entry.Entity is not ISoftDeleteable soft)
                    continue;

                // 1) Do not delete row; we only want to update two columns
                entry.State = EntityState.Unchanged;

                // 2) Set soft-delete flags
                soft.Delete();

                // 3) Mark only soft-delete props as modified
                entry.Property("IsDeleted").IsModified = true;
                entry.Property("DateDeleted").IsModified = true;

                // 4) Ensure owned navigations are not considered deleted/modified
                SetOwnedEntriesUnchanged(entry);
            }
        }

        private static void SetOwnedEntriesUnchanged(EntityEntry ownerEntry)
        {
            // Handle single owned references (e.g., Product.Size)
            foreach (var reference in ownerEntry.References)
            {
                var target = reference.TargetEntry;
                if (target is not null && target.Metadata.IsOwned())
                {
                    target.State = EntityState.Unchanged;
                }
            }

            // Handle owned collections if any (e.g., owned collection types)
            foreach (var collection in ownerEntry.Collections)
            {
                if (!collection.Metadata.TargetEntityType.IsOwned())
                    continue;

                // Do NOT force-load; only adjust already tracked dependents
                if (collection.IsLoaded && collection.CurrentValue is IEnumerable<object> dependents)
                {
                    foreach (var dependent in dependents)
                    {
                        var depEntry = ownerEntry.Context.Entry(dependent);
                        depEntry.State = EntityState.Unchanged;
                    }
                }
            }
        }
    }
}
