namespace CinemaTicketBooking.Application.Abstractions;

public interface IDistributedLock
{
    Task<ILockHandler> TryAcquireAsync(string key,
        TimeSpan timeout = default,
        CancellationToken cancellationToken = default);
}

public interface ILockHandler : IAsyncDisposable
{
    bool IsLocked { get; }
}