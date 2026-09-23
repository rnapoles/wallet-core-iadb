using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using WalletSystem.Application.Contracts.Services.Security;
using WalletSystem.Domain.Contracts.Auditing;
using WalletSystem.Domain.Contracts.Persistence;

namespace WalletSystem.Infrastructure.Persistence.Interceptors;

public class SoftDeleteInterceptor : ISaveChangesInterceptor
{
    
    private readonly ICurrentUserService _currentUserService;

    public SoftDeleteInterceptor(ICurrentUserService currentUserService)
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

        foreach (var entry in context.ChangeTracker.Entries<ISoftDeletableEntity>())
        {
            if (entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                entry.Entity.IsDeleted = true;
                entry.Entity.DeletedAt = DateTime.UtcNow;

                // Try to set DeletedBy if the entity also implements IBlameableEntity
                if (entry.Entity is IBlameableEntity blameable && entry.Context != null)
                {
                    // In a real application, you would get the current user from HttpContext or similar
                    // For now, we leave it as null or set from ambient context
                    entry.Entity.DeletedBy = GetCurrentUserId();
                }
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

        foreach (var entry in context.ChangeTracker.Entries<ISoftDeletableEntity>())
        {
            if (entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                entry.Entity.IsDeleted = true;
                entry.Entity.DeletedAt = DateTime.UtcNow;

                if (entry.Entity is IBlameableEntity blameable)
                {
                    entry.Entity.DeletedBy = GetCurrentUserId();
                }
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

    private string? GetCurrentUserId()
    {
        var id = this._currentUserService.UserId;
        return id == null ? null : id.ToString();
    }
}
