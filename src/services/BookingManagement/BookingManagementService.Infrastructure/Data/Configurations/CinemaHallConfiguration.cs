using CinemaTicketBooking.Domain.CinemaHalls;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CinemaTicketBooking.Infrastructure.Data;

public class CinemaHallConfiguration : IEntityTypeConfiguration<CinemaHall>
{
    public void Configure(EntityTypeBuilder<CinemaHall> builder)
    {
        builder.ToTable("cinema_hall");
        builder.HasKey(entry => entry.Id)
            .HasName("pk_cinema_hall");

        builder.Property(entry => entry.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(r => r.Name)
            .HasColumnName("name");
        builder.Property(r => r.Description)
            .HasColumnName("description");

        builder.OwnsMany(d => d.Seats, o =>
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
    }
}