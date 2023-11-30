// using CinemaTicketBooking.Domain.CinemaHalls;
// using CinemaTicketBooking.Domain.Entities;
// using CinemaTicketBooking.Domain.MovieSessions;
//
// namespace CinemaTicketBooking.Application.Abstractions;
//
//     public interface ITicketsRepository
//     {
//         Task<TicketEntity> ConfirmPaymentAsync(TicketEntity ticket,
//             CancellationToken cancel);
//
//         Task<TicketEntity> CreateAsync(MovieSession showtime,
//             IEnumerable<ShowTimeSeatEntity> selectedSeats,
//             CancellationToken cancel);
//
//         Task<TicketEntity> GetAsync(Guid id, CancellationToken cancel);
//
//         Task<IEnumerable<TicketEntity>> GetEnrichedAsync(Guid showtimeId,
//             CancellationToken cancel);
//     }