namespace CinemaTicketBooking.Application.Abstractions.Services;

public interface ICacheService
{
    Task<T?> TryGet<T>(string cacheKey);
    Task<T> Set<T>(string cacheKey, T value, TimeSpan? expiry);
    Task Remove(string cacheKey);
}