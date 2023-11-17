using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Application.Abstractions.Services;
using CinemaTicketBooking.Domain.CinemaHalls;
using CinemaTicketBooking.Infrastructure.Data;
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

        public async Task<ICollection<CinemaHall>> GetAllAsync(CancellationToken cancel)
        {
            return await _context.CinemaHalls
                //.Include(x => x.Seats)
                .ToListAsync(cancel);
        }
    }
}
