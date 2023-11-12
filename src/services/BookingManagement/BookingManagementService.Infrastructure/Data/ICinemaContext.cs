using CinemaTicketBooking.Domain.CinemaHalls;
using CinemaTicketBooking.Domain.Movies;
using CinemaTicketBooking.Domain.MovieSessions;
using CinemaTicketBooking.Domain.Seats;
using CinemaTicketBooking.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace CinemaTicketBooking.Infrastructure.Data;

public interface ICinemaContext
{
    DbSet<CinemaHall> CinemaHalls { get; }
    DbSet<MovieSession> MovieSessions { get; }
    DbSet<MovieSessionSeat> MovieSessionSeats { get; }
    DbSet<Movie> Movies { get; }
    DbSet<IdempotentRequest> IdempotentRequests { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}