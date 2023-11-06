using CinemaTicketBooking.Domain.Movies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CinemaTicketBooking.Infrastructure.Data;

public class MovieEntityConfiguration : IEntityTypeConfiguration<Movie>
{
    public void Configure(EntityTypeBuilder<Movie> builder)
    {
        builder.ToTable("movie");
        builder.HasKey(entry => entry.Id)
            .HasName("pk_movie_id");
        builder.Property(entry => entry.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(r => r.Title)
            .HasColumnName("title");

        builder.Property(r => r.ImdbId)
            .HasColumnName("imdb_id");

        builder.Property(r => r.ReleaseDate)
            .HasColumnName("release_date");

        builder.Property(r => r.Stars)
            .HasColumnName("stars");
    }
}