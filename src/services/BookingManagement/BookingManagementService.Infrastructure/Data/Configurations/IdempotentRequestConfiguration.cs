using CinemaTicketBooking.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CinemaTicketBooking.Infrastructure.Data;

public class IdempotentRequestConfiguration : IEntityTypeConfiguration<IdempotentRequest>
{
    public void Configure(EntityTypeBuilder<IdempotentRequest> builder)
    {
      
        builder.ToTable("idempotent_request");

        builder.HasKey(entry => entry.Id)
            .HasName("pk_idempotent_request");
        builder.Property(entry => entry.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(entry => entry.Name)
            .HasColumnName("name")
            .IsRequired();

        builder.Property(entry => entry.CreatedOnUtc)
            .HasColumnName("created_on_utc");
    }
}