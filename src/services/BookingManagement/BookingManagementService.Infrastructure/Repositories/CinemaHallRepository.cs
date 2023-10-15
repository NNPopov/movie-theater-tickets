using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Domain.CinemaHalls;
using Microsoft.EntityFrameworkCore;

namespace CinemaTicketBooking.Infrastructure.Repositories
{
    public class CinemaHallRepository : ICinemaHallRepository
    {
        private readonly CinemaContext _context;
        private readonly ICacheService _cacheService;

        public CinemaHallRepository(CinemaContext context, ICacheService cacheService)
        {
            _context = context;
            _cacheService = cacheService;
        }

        public async Task<CinemaHall> GetAsync(Guid auditoriumId, CancellationToken cancel)
        {
            return await _context.CinemaHalls
                .Include(x => x.Seats)
                .FirstOrDefaultAsync(x => x.Id == auditoriumId, cancel);
        }

        public async Task<ICollection<SeatEntity>> GetSeatsAsync(Guid auditoriumId, CancellationToken cancel)
        {
            
            var auditoriumSeatsKey = $"auditoriumSeats{auditoriumId}";
            ICollection<SeatEntity> seatsInCinemaHall =
                await _cacheService.TryGet<ICollection<SeatEntity>>(auditoriumSeatsKey);

            if (seatsInCinemaHall is null || !seatsInCinemaHall.Any())
            {
                var auditorium = await _context.CinemaHalls
                    .Include(x => x.Seats)
                    .FirstOrDefaultAsync(x => x.Id == auditoriumId, cancel);

                if (auditorium is null || !auditorium.Seats.Any())
                    return default;

                seatsInCinemaHall = auditorium.Seats.ToList();

                await _cacheService.Set(auditoriumSeatsKey, seatsInCinemaHall, new TimeSpan(1, 0, 0));
            }

            return seatsInCinemaHall;
        }

        public async Task<ICollection<CinemaHall>> GetAllAsync(CancellationToken cancel)
        {
            return await _context.CinemaHalls
                //.Include(x => x.Seats)
                .ToListAsync();
        }
    }
}
