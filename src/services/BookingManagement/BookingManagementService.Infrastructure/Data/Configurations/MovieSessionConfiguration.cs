using CinemaTicketBooking.Domain.MovieSessions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CinemaTicketBooking.Infrastructure.Data;

public class MovieSessionConfiguration : IEntityTypeConfiguration<MovieSession>
{
    public void Configure(EntityTypeBuilder<MovieSession> builder)
    {
        builder.ToTable("movie_session");
        builder.HasKey(entry => entry.Id)
            .HasName("pk_movie_session_id");

        builder.Property(entry => entry.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(r => r.CinemaHallId)
            .HasColumnName("cinema_hall_id");

        builder.Property(r => r.IsEnabled)
            .HasColumnName("is_enabled");

        builder.Property(r => r.MovieId)
            .HasColumnName("movie_id");

        builder.Property(r => r.SoldTickets)
            .HasColumnName("sold_tickets");

        builder.Property(r => r.TicketsForSale)
            .HasColumnName("tickets_for_sale");

        builder.Property(r => r.SessionDate)
            .HasColumnName("session_date");
    }
}