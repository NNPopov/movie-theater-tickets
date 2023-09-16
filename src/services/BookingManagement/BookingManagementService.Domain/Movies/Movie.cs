using CinemaTicketBooking.Domain.Common;
using CinemaTicketBooking.Domain.Common.Ensure;
using CinemaTicketBooking.Domain.Movies.Events;

namespace CinemaTicketBooking.Domain.Movies;

public class Movie : AggregateRoot
{
    
    private Movie(Guid id,
        string title,
        DateTime releaseDate,
        string imdbId,
        string stars) : base(id)
    {
        Ensure.NotEmpty(title, "The title is required.", nameof(title));
        Ensure.NotEmpty(releaseDate, "The releaseDate is required.", nameof(releaseDate));
        
        Title = title;
        ReleaseDate = releaseDate;
        ImdbId = imdbId;
        Stars = stars;
    }

    public string Title { get; private set; }
    public string ImdbId { get; private set;  }
    public string Stars { get; private set;  }
    public DateTime ReleaseDate { get; private set;  }

    public static Movie Create(string title,
        DateTime releaseDate,
        string imdbId,
        string stars)
    {
        var movie = new Movie(
            Guid.NewGuid(),
            title,
            releaseDate,
            imdbId,
            stars
        );
        
        movie.AddDomainEvent(new MovieCreatedDomainEvent(movie));
        return movie;
    }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="AggregateRoot"/> class.
    /// </summary>
    /// <remarks>
    /// Required by EF Core.
    /// </remarks>
    private Movie()
    {
        
    }
}