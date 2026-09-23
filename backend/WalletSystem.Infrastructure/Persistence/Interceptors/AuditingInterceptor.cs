using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using WalletSystem.Application.Contracts.Services.Security;
using WalletSystem.Domain.Contracts.Auditing;
using WalletSystem.Domain.Contracts.Persistence;

namespace WalletSystem.Infrastructure.Persistence.Interceptors;

public class AuditingInterceptor : ISaveChangesInterceptor
{

    private readonly ICurrentUserService _currentUserService;

    public AuditingInterceptor(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }
    
    public ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        if (context == null)
        {
            return new ValueTask<InterceptionResult<int>>(result);
        }

        var currentUserId = GetCurrentUserId();
        var utcNow = DateTime.UtcNow;

        foreach (var entry in context.ChangeTracker.Entries())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    SetAuditFieldsForNewEntity(entry, currentUserId, utcNow);
                    break;

                case EntityState.Modified:
                    SetAuditFieldsForModifiedEntity(entry, currentUserId, utcNow);
                    break;
            }
        }

        return new ValueTask<InterceptionResult<int>>(result);
    }

    public InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        var context = eventData.Context;
        if (context == null)
        {
            return result;
        }

        var currentUserId = GetCurrentUserId();
        var utcNow = DateTime.UtcNow;

        foreach (var entry in context.ChangeTracker.Entries())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    SetAuditFieldsForNewEntity(entry, currentUserId, utcNow);
                    break;

                case EntityState.Modified:
                    SetAuditFieldsForModifiedEntity(entry, currentUserId, utcNow);
                    break;
            }
        }

        return result;
    }

    public ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        return new ValueTask<int>(result);
    }

    public int SavedChanges(
        SaveChangesCompletedEventData eventData,
        int result)
    {
        return result;
    }

    public InterceptionResult ThrowingConcurrencyException(
        ConcurrencyExceptionEventData eventData,
        InterceptionResult result)
    {
        return result;
    }

    public ValueTask<InterceptionResult> ThrowingConcurrencyExceptionAsync(
        ConcurrencyExceptionEventData eventData,
        InterceptionResult result,
        CancellationToken cancellationToken = default)
    {
        return new ValueTask<InterceptionResult>(result);
    }

    private static void SetAuditFieldsForNewEntity(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry, string? currentUserId, DateTime utcNow)
    {
        // Handle ITimestampableEntity
        if (entry.Entity is ITimestampableEntity timestampable)
        {
            timestampable.CreatedAt = utcNow;
            timestampable.UpdatedAt = null;
        }

        // Handle IBlameableEntity
        if (entry.Entity is IBlameableEntity blameable)
        {
            blameable.CreatedBy = currentUserId;
            blameable.UpdatedBy = null;
        }

        // Handle ISoftDeletableEntity (ensure IsDeleted is false for new entities)
        if (entry.Entity is ISoftDeletableEntity softDeletable)
        {
            softDeletable.IsDeleted = false;
            softDeletable.DeletedAt = null;
            softDeletable.DeletedBy = null;
        }
    }

    private static void SetAuditFieldsForModifiedEntity(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry, string? currentUserId, DateTime utcNow)
    {
        // Skip if the entity is being soft deleted (SoftDeleteInterceptor handles that)
        if (entry.Entity is ISoftDeletableEntity softDel && softDel.IsDeleted)
        {
            return;
        }

        // Handle ITimestampableEntity
        if (entry.Entity is ITimestampableEntity timestampable)
        {
            timestampable.UpdatedAt = utcNow;
        }

        // Handle IBlameableEntity
        if (entry.Entity is IBlameableEntity blameable)
        {
            blameable.UpdatedBy = currentUserId;
        }
    }

    private string? GetCurrentUserId()
    {
        var id = this._currentUserService.UserId;
        return id == null ? null : id.ToString();
    }
}
