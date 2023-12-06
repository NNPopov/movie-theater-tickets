// using CinemaTicketBooking.Application.Abstractions;
// using CinemaTicketBooking.Domain.CinemaHalls;
// using CinemaTicketBooking.Domain.Entities;
// using CinemaTicketBooking.Domain.MovieSessions;
// using Microsoft.EntityFrameworkCore;
//
// namespace CinemaTicketBooking.Infrastructure.Repositories
// {
//     public class TicketsRepository : ITicketsRepository
//     {
//         private readonly CinemaContext _context;
//
//         public TicketsRepository(CinemaContext context)
//         {
//             _context = context;
//         }
//
//         public Task<TicketEntity> IsSeatReservedAsync(Guid id, CancellationToken cancel)
//         {
//             return _context.Tickets.FirstOrDefaultAsync(x => x.Id == id, cancel);
//         }
//
//         public async Task<IEnumerable<TicketEntity>> GetEnrichedAsync(Guid showtimeId, CancellationToken cancel)
//         {
//             return await _context.Tickets
//                 .Include(x => x.MovieSession)
//                 .Include(x => x.Seats)
//                 .Where(x => x.MovieSessionId == showtimeId)
//                 .ToListAsync(cancel);
//         }
//
//         public async Task<TicketEntity> CreateAsync(MovieSession showtime, IEnumerable<ShowTimeSeatEntity> selectedSeats, CancellationToken cancel)
//         {
//             var ticket = _context.Tickets.Add(new TicketEntity
//             {
//                 MovieSession = showtime,
//                 Seats = new List<ShowTimeSeatEntity>(selectedSeats)
//             });
//
//             await _context.SaveChangesAsync(cancel);
//
//             return ticket.Entity;
//         }
//
//         public async Task<TicketEntity> ConfirmPaymentAsync(TicketEntity ticket, CancellationToken cancel)
//         {
//             ticket.Paid = true;
//             _context.Update(ticket);
//             await _context.SaveChangesAsync(cancel);
//             return ticket;
//         }
//     }
// }
