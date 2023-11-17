using CinemaTicketBooking.Domain.CinemaHalls;

namespace CinemaTicketBooking.Application.Abstractions
{
    public interface ICinemaHallRepository
    {
        Task<CinemaHall> GetAsync(Guid auditoriumId, CancellationToken cancel);
        
        
        Task<ICollection<CinemaHall>> GetAllAsync(CancellationToken cancel);
    }
}