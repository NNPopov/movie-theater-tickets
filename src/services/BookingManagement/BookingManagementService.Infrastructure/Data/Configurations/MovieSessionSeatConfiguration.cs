using CinemaTicketBooking.Domain.Seats;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CinemaTicketBooking.Infrastructure.Data.Configurations;

public class MovieSessionSeatConfiguration : IEntityTypeConfiguration<MovieSessionSeat>
{
    public void Configure(EntityTypeBuilder<MovieSessionSeat> builder)
    {
        
        builder.ToTable("movie_session_seat");
        builder.HasKey(entry => new { Showtime = entry.MovieSessionId, entry.SeatRow, entry.SeatNumber })
            .HasName("pk_movie_session_seat");

        builder.Property(entry => entry.SeatRow)
            .HasColumnName("seat_row")
            .ValueGeneratedNever();
        builder.Property(entry => entry.SeatNumber)
            .HasColumnName("seat_number")
            .ValueGeneratedNever();
        builder.Property(entry => entry.MovieSessionId)
            .HasColumnName("showtime")
            .ValueGeneratedNever();

        builder.Property(entry => entry.Price)
            .HasColumnName("price");
        builder.Property(entry => entry.Status)
            .HasColumnName("status");

        builder.Property(entry => entry.ShoppingCartId)
            .HasColumnName("shopping_cart_id");

        builder.Property(entry => entry.HashId)
            .HasColumnName("hash_id");
    }
}