namespace CinemaTicketBooking.Application.Abstractions;

public interface IIdempotencyService
{

    Task<bool> RequestExistsAsync(Guid requestId);
    
    Task CreateRequestAsync(Guid requestId, string name);
}