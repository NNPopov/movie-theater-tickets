using CinemaTicketBooking.Domain.CinemaHalls;

namespace CinemaTicketBooking.Application.Abstractions
{
    public interface IAuditoriumsRepository
    {
        Task<CinemaHall> GetAsync(Guid auditoriumId, CancellationToken cancel);
        
        Task<ICollection<SeatEntity>> GetSeatsAsync(Guid auditoriumId, CancellationToken cancel);
        
        Task<ICollection<CinemaHall>> GetAllAsync(CancellationToken cancel);
    }
}