using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Domain.CinemaHalls;
using Microsoft.EntityFrameworkCore;

namespace CinemaTicketBooking.Infrastructure.Repositories
{
    public class AuditoriumsRepository : IAuditoriumsRepository
    {
        private readonly CinemaContext _context;
        private readonly ICacheService _cacheService;

        public AuditoriumsRepository(CinemaContext context, ICacheService cacheService)
        {
            _context = context;
            _cacheService = cacheService;
        }

        public async Task<CinemaHall> GetAsync(Guid auditoriumId, CancellationToken cancel)
        {
            return await _context.Auditoriums
                .Include(x => x.Seats)
                .FirstOrDefaultAsync(x => x.Id == auditoriumId, cancel);
        }

        public async Task<ICollection<SeatEntity>> GetSeatsAsync(Guid auditoriumId, CancellationToken cancel)
        {
            
            var auditoriumSeatsKey = $"auditoriumSeats{auditoriumId}";
            ICollection<SeatEntity> seatsInAuditorium =
                await _cacheService.TryGet<ICollection<SeatEntity>>(auditoriumSeatsKey);

            if (seatsInAuditorium is null || !seatsInAuditorium.Any())
            {
                var auditorium = await _context.Auditoriums
                    .Include(x => x.Seats)
                    .FirstOrDefaultAsync(x => x.Id == auditoriumId, cancel);

                if (auditorium is null || !auditorium.Seats.Any())
                    return default;

                seatsInAuditorium = auditorium.Seats.ToList();

                await _cacheService.Set(auditoriumSeatsKey, seatsInAuditorium, new TimeSpan(1, 0, 0));
            }

            return seatsInAuditorium;
        }

        public async Task<ICollection<CinemaHall>> GetAllAsync(CancellationToken cancel)
        {
            return await _context.Auditoriums
                //.Include(x => x.Seats)
                .ToListAsync();
        }
    }
}
