namespace CinemaTicketBooking.Application.Abstractions.Services;

public interface IIdempotencyService
{

    Task<bool> RequestExistsAsync(Guid requestId);
    
    Task CreateRequestAsync(Guid requestId, string name);
}