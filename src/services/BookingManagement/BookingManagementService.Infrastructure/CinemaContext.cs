using System.Text.Json;
using System.Text.Json.Serialization;
using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Application.Common.Events;
using CinemaTicketBooking.Domain.CinemaHalls;
using CinemaTicketBooking.Domain.Common;
using CinemaTicketBooking.Domain.Common.Events;
using CinemaTicketBooking.Domain.Entities;
using CinemaTicketBooking.Domain.Movies;
using CinemaTicketBooking.Domain.MovieSessions;
using CinemaTicketBooking.Domain.Seats;
using CinemaTicketBooking.Infrastructure.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Newtonsoft.Json;
using Serilog;

namespace CinemaTicketBooking.Infrastructure;
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

public class DomainEventTracker : IDomainEventTracker
{
    private readonly IMediator _mediator;
    private readonly ILogger _logger;

    public DomainEventTracker(IMediator mediator, ILogger logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task PublishDomainEvents( IAggregateRoot  aggregateRoot,CancellationToken cancellationToken = default)
    {
        try
        {
            var domainEvents =
                aggregateRoot.GetDomainEvents().ToList();

            aggregateRoot.ClearDomainEvents();

            IEnumerable<Task> tasks = domainEvents.Select(domainEvent =>
            {
                var baseApplicationEventBuilder = typeof(BaseApplicationEvent<>).MakeGenericType(domainEvent.GetType());

                var appEvent = Activator.CreateInstance(baseApplicationEventBuilder,
                    domainEvent
                );

                _logger.Debug("Publish event: {AppEvent}, {@DomainEvent}", domainEvent.GetType().ToString(), domainEvent);
                return _mediator.Publish(appEvent, cancellationToken);
            });

            await Task.WhenAll(tasks);
        }
        catch (Exception e)
        {
            _logger.Error("PublishDomainEvents {@e}", e);

        }
    }
}

public class CinemaContext : DbContext
{

    public CinemaContext(DbContextOptions<CinemaContext> options) : base(options)
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
    }

    // public CinemaContext(DbContextOptions<CinemaContext> options) : base(options)
    // {
    // }


    public DbSet<CinemaHall> CinemaHalls { get; set; }
    public DbSet<MovieSession> MovieSessions { get; set; }

    public DbSet<MovieSessionSeat> MovieSessionSeats { get; set; }
    public DbSet<Movie> Movies { get; set; }
    //public DbSet<TicketEntity> Tickets { get; set; }

    public DbSet<IdempotentRequest> IdempotentRequests { get; set; }


    public override int SaveChanges()
    {
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var result = await base.SaveChangesAsync(cancellationToken);
        
        return result;
    }

   

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Ignore<List<IDomainEvent>>();

        modelBuilder.Entity<MovieSessionSeat>(build =>
            {
                build.ToTable("movie_session_seat");
                build.HasKey(entry => new { Showtime = entry.MovieSessionId, entry.SeatRow, entry.SeatNumber })
                    .HasName("pk_movie_session_seat");

                build.Property(entry => entry.SeatRow)
                    .HasColumnName("seat_row")
                    .ValueGeneratedNever();
                build.Property(entry => entry.SeatNumber)
                    .HasColumnName("seat_number")
                    .ValueGeneratedNever();
                build.Property(entry => entry.MovieSessionId)
                    .HasColumnName("showtime")
                    .ValueGeneratedNever();

                build.Property(entry => entry.Price)
                    .HasColumnName("price");
                build.Property(entry => entry.Status)
                    .HasColumnName("status");

                build.Property(entry => entry.ShoppingCartId)
                    .HasColumnName("shopping_cart_id");

                build.Property(entry => entry.HashId)
                    .HasColumnName("hash_id");
            }
        );

        modelBuilder.Entity<CinemaHall>(build =>
        {
            build.ToTable("cinema_hall");
            build.HasKey(entry => entry.Id)
                .HasName("pk_cinema_hall");

            build.Property(entry => entry.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            build.Property(r => r.Name)
                .HasColumnName("name");
            build.Property(r => r.Description)
                .HasColumnName("description");

            build.OwnsMany(d => d.Seats, o =>
            {
                o.ToTable("seat");
                o.WithOwner()
                    .HasForeignKey(t => t.CinemaHallId)
                    .HasConstraintName("fk_seat_cinema_hall_cinema_hall_id");

                o.HasKey(k => new { AuditoriumId = k.CinemaHallId, k.Row, k.SeatNumber }).HasName("pk_seat");

                o.Property(r => r.CinemaHallId)
                    .HasColumnName("cinema_hall_id");

                o.Property(r => r.Row)
                    .HasColumnName("row");

                o.Property(r => r.SeatNumber)
                    .HasColumnName("seat_number");
            });
        });

        modelBuilder.Entity<ShowTimeSeatEntity>(build =>
        {
            build.ToTable("show_time_seat");
            build
                .HasKey(entry => new { AuditoriumId = entry.CinemaHallId, entry.Row, entry.SeatNumber })
                .HasName("kp_show_time_seat");

            build.Property(r => r.CinemaHallId)
                .HasColumnName("cinema_hall_id");
            build.Property(r => r.Row)
                .HasColumnName("row");
            build.Property(r => r.SeatNumber)
                .HasColumnName("seat_number");
        });

        modelBuilder.Entity<MovieSession>(build =>
        {
            build.ToTable("movie_session");
            build.HasKey(entry => entry.Id)
                .HasName("pk_movie_session_id");

            build.Property(entry => entry.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();

            build.Property(r => r.CinemaHallId)
                .HasColumnName("cinema_hall_id");

            build.Property(r => r.IsEnabled)
                .HasColumnName("is_enabled");

            build.Property(r => r.MovieId)
                .HasColumnName("movie_id");

            build.Property(r => r.SoldTickets)
                .HasColumnName("sold_tickets");

            build.Property(r => r.TicketsForSale)
                .HasColumnName("tickets_for_sale");

            build.Property(r => r.SessionDate)
                .HasColumnName("session_date");
        });

        modelBuilder.Entity<Movie>(build =>
        {
            build.ToTable("movie");
            build.HasKey(entry => entry.Id)
                .HasName("pk_movie_id");
            build.Property(entry => entry.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();

            build.Property(r => r.Title)
                .HasColumnName("title");

            build.Property(r => r.ImdbId)
                .HasColumnName("imdb_id");

            build.Property(r => r.ReleaseDate)
                .HasColumnName("release_date");

            build.Property(r => r.Stars)
                .HasColumnName("stars");
        });

        // modelBuilder.Entity<TicketEntity>(build =>
        // {
        //     build.ToTable("ticket_entity");
        //     
        //     build.HasKey(entry => entry.Id).HasName("pk_ticket_entity");
        //     
        //     build.Property(entry => entry.Id)
        //         .HasColumnName("id")
        //         .ValueGeneratedOnAdd();
        //     
        //     build.Property(r => r.MovieSessionId)
        //         .HasColumnName("movie_session_id");
        //     
        //     build.Property(r => r.MovieSession)
        //         .HasColumnName("movie_session");
        //     
        //     build.Property(r => r.Paid)
        //         .HasColumnName("paid");
        // });

        modelBuilder.Entity<IdempotentRequest>(build =>
        {
            build.ToTable("idempotent_request");

            build.HasKey(entry => entry.Id)
                .HasName("pk_idempotent_request");
            build.Property(entry => entry.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();

            build.Property(entry => entry.Name)
                .HasColumnName("name")
                .IsRequired();

            build.Property(entry => entry.CreatedOnUtc)
                .HasColumnName("created_on_utc");
        });
    }
}