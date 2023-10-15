using System.Text.Json;
using System.Text.Json.Serialization;
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
using Newtonsoft.Json;

namespace CinemaTicketBooking.Infrastructure;

public class CinemaContext : DbContext
{
    private readonly IMediator _mediator;

    public CinemaContext(DbContextOptions<CinemaContext> options,
        IMediator mediator) : base(options)
    {
        _mediator = mediator;
    }

    public DbSet<CinemaHall> CinemaHalls { get; set; }
    public DbSet<MovieSession> MovieSessions { get; set; }

    public DbSet<MovieSessionSeat> ShowtimeSeats { get; set; }
    public DbSet<Movie> Movies { get; set; }
    public DbSet<TicketEntity> Tickets { get; set; }
    
    public DbSet<IdempotentRequest> IdempotentRequests { get; set; }
    

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var result = await base.SaveChangesAsync(cancellationToken);
        
         await PublishDomainEvents(cancellationToken);

         return result;
    }

    private async Task PublishDomainEvents(CancellationToken cancellationToken)
    {
        var aggregateRoots = ChangeTracker
            .Entries<AggregateRoot>()
            .Where(entityEntry => entityEntry.Entity.DomainEvents.Any()).ToList();

        var domainEvents =
            aggregateRoots.SelectMany(entityEntry => entityEntry.Entity.DomainEvents);

        aggregateRoots.ForEach(entityEntry => entityEntry.Entity.ClearDomainEvents());

        IEnumerable<Task> tasks = domainEvents.Select(domainEvent =>
        {
            var baseApplicationEventBuilder = typeof(BaseApplicationEvent<>).MakeGenericType(domainEvent.GetType());

            var appEvent = Activator.CreateInstance(baseApplicationEventBuilder,
                domainEvent
            );

            return _mediator.Publish(appEvent, cancellationToken);
        });

        await Task.WhenAll(tasks);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Ignore<List<IDomainEvent>>();
        
        modelBuilder.Entity<MovieSessionSeat>(build =>
            {
                build.ToTable("showtime_seat");
                build.HasKey(entry => new { Showtime = entry.MovieSessionId, entry.SeatRow, entry.SeatNumber })
                    .HasName("pk_showtime_seat");

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
            }
        );

        modelBuilder.Entity<CinemaHall>(build =>
        {
            build.ToTable("auditorium_entity");
            build.HasKey(entry => entry.Id);
            build.Property(entry => entry.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();
            build.OwnsMany(d => d.Seats, o =>
            {
                o.ToTable("seat");
                o.WithOwner()
                    .HasForeignKey("auditorium_id");

                o.HasKey(k => new { k.AuditoriumId, k.Row, k.SeatNumber });

                o.Property(r => r.AuditoriumId)
                    .HasColumnName("auditorium_id");
                o.Property(r => r.Row)
                    .HasColumnName("row");
                o.Property(r => r.SeatNumber)
                    .HasColumnName("seat_number");
            });

            // build.HasMany(entry => entry.MovieSessions)
            //     .WithOne()
            //     .HasForeignKey(entity => entity.AuditoriumId);
        });

        modelBuilder.Entity<ShowTimeSeatEntity>(build =>
        {
            build.ToTable("show_time_seat_entity");
            build
                .HasKey(entry => new { entry.AuditoriumId, entry.Row, entry.SeatNumber });
            // build
            //     .HasOne(entry => entry.CinemaHall)
            //     .WithMany(entry => entry.Seats)
            //     .HasForeignKey(entry => entry.AuditoriumId);
        });

        modelBuilder.Entity<MovieSession>(build =>
        {
            var opts = new JsonSerializerOptions { ReferenceHandler = ReferenceHandler.IgnoreCycles };

            build.ToTable("showtime");
            build.HasKey(entry => entry.Id);

            // build.Property(t => t.Seats)
            //     .HasColumnName("seats")
            //     .HasConversion(
            //         v => JsonConvert.SerializeObject(v),
            //         v => JsonConvert.DeserializeObject<SeatMovieSession[]>(v));

            build.Property(entry => entry.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();

            // build.HasOne(entry => entry.Movie).WithMany(entry => entry.MovieSessions);
            // build
            //     .HasMany(entry => entry.Tickets)
            //     .WithOne(entry => entry.ShoppingCartId)
            //     .HasForeignKey(entry => entry.ShoppingCartId);
        });

        modelBuilder.Entity<Movie>(build =>
        {
            build.ToTable("movie");
            build.HasKey(entry => entry.Id);
            build.Property(entry => entry.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();
            // build.OwnsMany(t => t.ShowtimeIds, i =>
            // {
            //     i.ToTable("showtime");
            //     i.WithOwner().HasForeignKey("id");
            // });
        });

        modelBuilder.Entity<TicketEntity>(build =>
        {
            build.ToTable("ticket_entity");
            build.HasKey(entry => entry.Id);
            build.Property(entry => entry.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();
        });
        
        modelBuilder.Entity<IdempotentRequest>(build =>
        {
            build.ToTable("idempotent_request");
            
            build.HasKey(entry => entry.Id);
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