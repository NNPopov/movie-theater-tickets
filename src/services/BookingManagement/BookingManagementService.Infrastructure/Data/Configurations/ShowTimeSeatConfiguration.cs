using CinemaTicketBooking.Domain.CinemaHalls;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CinemaTicketBooking.Infrastructure.Data;

public class ShowTimeSeatConfiguration : IEntityTypeConfiguration<ShowTimeSeatEntity>
{
    public void Configure(EntityTypeBuilder<ShowTimeSeatEntity> builder)
    {
        builder.ToTable("show_time_seat");
        builder
            .HasKey(entry => new { AuditoriumId = entry.CinemaHallId, entry.Row, entry.SeatNumber })
            .HasName("kp_show_time_seat");

        builder.Property(r => r.CinemaHallId)
            .HasColumnName("cinema_hall_id");
        builder.Property(r => r.Row)
            .HasColumnName("row");
        builder.Property(r => r.SeatNumber)
            .HasColumnName("seat_number");
    }
}