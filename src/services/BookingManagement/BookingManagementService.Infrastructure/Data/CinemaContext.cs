using System.Reflection;
using CinemaTicketBooking.Domain.CinemaHalls;
using CinemaTicketBooking.Domain.Common.Events;
using CinemaTicketBooking.Domain.Movies;
using CinemaTicketBooking.Domain.MovieSessions;
using CinemaTicketBooking.Domain.Seats;
using CinemaTicketBooking.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace CinemaTicketBooking.Infrastructure.Data;
//
// public class BloggingContextFactory : IDesignTimeDbContextFactory<CinemaContext>
// {
//     public CinemaContext CreateDbContext(string[] args)
//     {
//         var optionsBuilder = new DbContextOptionsBuilder<CinemaContext>();
//         optionsBuilder.UseNpgsql(
//             "Server=localhost;Port=5452;Database=booking_db;Username=booking_user;Password=password");
//         return new CinemaContext(optionsBuilder.Options);
//     }
// }

public interface ICinemaContext
{
    DbSet<CinemaHall> CinemaHalls { get; }
    DbSet<MovieSession> MovieSessions { get; }
    DbSet<MovieSessionSeat> MovieSessionSeats { get; }
    DbSet<Movie> Movies { get; }
    DbSet<IdempotentRequest> IdempotentRequests { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}

public class CinemaContext : DbContext, ICinemaContext
{
    public CinemaContext(DbContextOptions<CinemaContext> options) : base(options)
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
    }


    public DbSet<CinemaHall> CinemaHalls => Set<CinemaHall>();
    public DbSet<MovieSession> MovieSessions => Set<MovieSession>();

    public DbSet<MovieSessionSeat> MovieSessionSeats => Set<MovieSessionSeat>();

    public DbSet<Movie> Movies => Set<Movie>();

    public DbSet<IdempotentRequest> IdempotentRequests => Set<IdempotentRequest>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Ignore<List<IDomainEvent>>();

        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        base.OnModelCreating(modelBuilder);
    }
}