using CinemaTicketBooking.Application.MovieSessions.Queries;
using CinemaTicketBooking.Application.MovieSessionSeats;
using CinemaTicketBooking.Domain.CinemaHalls;
using CinemaTicketBooking.Domain.Movies;
using CinemaTicketBooking.Domain.MovieSessions;
using CinemaTicketBooking.Domain.Seats;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace CinemaTicketBooking.Infrastructure.Data;

public static class DbInitializerExtensions
{
    public static async Task InitialiseDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var initializer = scope.ServiceProvider.GetRequiredService<SampleDataInitializer>();

        await initializer.InitialiseAsync();

        await initializer.SeedAsync();

        await initializer.WormUpCache();
    }
}

public class SampleDataInitializer
{
    private readonly CinemaContext _context;
    private readonly ILogger _logger;
    private IMovieSessionSeatsDataCacheService _movieSessionSeatsDataCacheService;

    public SampleDataInitializer(CinemaContext context, ILogger logger,
        IMovieSessionSeatsDataCacheService movieSessionSeatsDataCacheService)
    {
        _context = context;
        _logger = logger;
        _movieSessionSeatsDataCacheService = movieSessionSeatsDataCacheService;
    }

    public async Task InitialiseAsync()
    {
        try
        {
            await _context.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "An error occurred while initialising the database.");
            throw;
        }
    }

    public async Task SeedAsync()
    {
        try
        {
            await TrySeedAsync();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "An error occurred while seeding the database.");
        }
    }

    public async Task WormUpCache()
    {
        try
        {
            var movieSessions = await _context.MovieSessions
                .Where(t => t.SessionDate > TimeProvider.System.GetUtcNow())
                .ToListAsync();


            foreach (var movieSession in movieSessions)
            {
                ICollection<MovieSessionSeat> movieSessionSeats = await _context.MovieSessionSeats
                    .Where(t => t.MovieSessionId == movieSession.Id)
                    .ToListAsync();

                var seats =
                    movieSessionSeats.Select(allSeats =>
                        new MovieSessionSeatDto(
                            SeatNumber: allSeats.SeatNumber,
                            Row: allSeats.SeatRow,
                            Blocked: allSeats.Status == SeatStatus.Available ? false : true,
                            SeatStatus: allSeats.Status,
                            HashId: allSeats.ShoppingCartHashId
                        )).ToList();

                await _movieSessionSeatsDataCacheService.GetMovieSessionSeatsData(movieSession.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "An error occurred while Worm Up Cache.");
        }
    }


    public async Task TrySeedAsync()
    {
        // context.Database.EnsureCreated();

        var movieId = Guid.Parse("E1FDE23C-E26D-44D2-88F8-202951255001");
        var movieId2 = Guid.Parse("E1FDE23C-E26D-44D2-88F8-202951255002");
        var movieId3 = Guid.Parse("E1FDE23C-E26D-44D2-88F8-202951255003");
        var movieId4 = Guid.Parse("E1FDE23C-E26D-44D2-88F8-202951255004");
        var movieId5 = Guid.Parse("E1FDE23C-E26D-44D2-88F8-202951255005");

        var redAuditoriumId = Guid.Parse("97207AE2-E5DD-4084-903A-5655966CA010");
        var blackAuditoriumId = Guid.Parse("97207AE2-E5DD-4084-903A-5655966CA011");
        var whiteAuditoriumId = Guid.Parse("97207AE2-E5DD-4084-903A-5655966CA012");

        var movieSessionId = Guid.Parse("97207AE2-E5DD-4084-903A-5655966CD101");
        var movieSessionId2 = Guid.Parse("97207AE2-E5DD-4084-903A-5655966CD102");
        var movieSessionId3 = Guid.Parse("97207AE2-E5DD-4084-903A-5655966CD103");
        var movieSessionId4 = Guid.Parse("97207AE2-E5DD-4084-903A-5655966CD104");
        var movieSessionId5 = Guid.Parse("97207AE2-E5DD-4084-903A-5655966CD105");
        var movieSessionId6 = Guid.Parse("97207AE2-E5DD-4084-903A-5655966CD106");

        var movie = Movie.Create(
            "Inception",
            new DateTime(2010, 01, 14),
            "tt1375666",
            "Leonardo DiCaprio, Joseph Gordon-Levitt, Ellen Page, Ken Watanabe"
        );
        movie = movie.SetKey(movieId);

        var movie2 = Movie.Create(
            "Back to the Future",
            new DateTime(1985, 07, 03),
            "tt0088763",
            "Michael J. Fox, Christopher Lloyd, Lea Thompson, Crispin Glover"
        );
        movie2 = movie2.SetKey(movieId2);

        var movie3 = Movie.Create(
            "Back to the Future Part II",
            new DateTime(1989, 10, 20),
            "tt0096874",
            "Michael J. Fox, Christopher Lloyd, Lea Thompson, Crispin Glover"
        );
        movie3 = movie3.SetKey(movieId3);

        var movie4 = Movie.Create(
            "Back to the Future Part III",
            new DateTime(1990, 05, 25),
            "tt0099088",
            "Michael J. Fox, Christopher Lloyd, Mary Steenburgen, Thomas F. Wilson"
        );

        movie4 = movie4.SetKey(movieId4);

        _context.Movies.Add(movie);
        _context.Movies.Add(movie2);
        _context.Movies.Add(movie3);
        _context.Movies.Add(movie4);
        await _context.SaveChangesAsync();

        var redAuditorium = CinemaHall.Create(
            name: "Red",
            description: "Red",
            seats: GenerateSeats(28, 22)
        );

        redAuditorium = redAuditorium.SetKey(redAuditoriumId);

        var blackAuditorium = CinemaHall.Create(
            name: "Black",
            description: "Black",
            seats: GenerateSeats(21, 18)
        );
        blackAuditorium = blackAuditorium.SetKey(blackAuditoriumId);


        var whiteAuditorium = CinemaHall.Create(
            name: "White",
            description: "White",
            seats: GenerateSeats(15, 12)
        );
        whiteAuditorium = whiteAuditorium.SetKey(whiteAuditoriumId);

        _context.CinemaHalls.Add(redAuditorium);
        _context.CinemaHalls.Add(blackAuditorium);
        _context.CinemaHalls.Add(whiteAuditorium);
        await _context.SaveChangesAsync();

        await CreateMovieSessionMovieSessionSeats(movie,
            redAuditorium,
            new DateTime(2024, 12, 20, 12, 00, 0),
            movieSessionId);

        await CreateMovieSessionMovieSessionSeats(movie,
            redAuditorium,
            new DateTime(2024, 12, 21, 15, 30, 0),
            movieSessionId2);

        await CreateMovieSessionMovieSessionSeats(movie2,
            whiteAuditorium,
            new DateTime(2024, 12, 21, 15, 30, 0),
            movieSessionId3);

        await CreateMovieSessionMovieSessionSeats(movie2,
            whiteAuditorium,
            new DateTime(2024, 12, 23, 15, 30, 0),
            movieSessionId4);

        await CreateMovieSessionMovieSessionSeats(movie3,
            blackAuditorium,
            new DateTime(2024, 12, 22, 15, 30, 0),
            movieSessionId5);


        await CreateMovieSessionMovieSessionSeats(movie3,
            whiteAuditorium,
            new DateTime(2024, 12, 27, 15, 30, 0),
            movieSessionId6);

        await CreateMovieSessionMovieSessionSeats(movie4,
            whiteAuditorium,
            new DateTime(2024, 12, 27, 19, 00, 0));

        await CreateMovieSessionMovieSessionSeats(movie4,
            blackAuditorium,
            new DateTime(2024, 12, 26, 08, 30, 0));

        await CreateMovieSessionMovieSessionSeats(movie4,
            blackAuditorium,
            new DateTime(2024, 12, 26, 11, 00, 0));

        await CreateMovieSessionMovieSessionSeats(movie4,
            blackAuditorium,
            new DateTime(2024, 12, 26, 13, 30, 0));

        await CreateMovieSessionMovieSessionSeats(movie4,
            blackAuditorium,
            new DateTime(2024, 12, 26, 16, 00, 0));

        await CreateMovieSessionMovieSessionSeats(movie4,
            blackAuditorium,
            new DateTime(2024, 12, 26, 19, 00, 0));

        await CreateMovieSessionMovieSessionSeats(movie4,
            blackAuditorium,
            new DateTime(2024, 12, 26, 22, 30, 0));

        await CreateMovieSessionMovieSessionSeats(movie4,
            whiteAuditorium,
            new DateTime(2024, 12, 27, 19, 00, 0));


        await CreateMovieSessionMovieSessionSeats(movie4,
            redAuditorium,
            new DateTime(2024, 12, 26, 09, 00, 0));

        await CreateMovieSessionMovieSessionSeats(movie4,
            redAuditorium,
            new DateTime(2024, 12, 26, 12, 30, 0));

        await CreateMovieSessionMovieSessionSeats(movie4,
            redAuditorium,
            new DateTime(2024, 12, 26, 17, 00, 0));
    }

    private async Task CreateMovieSessionMovieSessionSeats(Movie movie, CinemaHall redAuditorium, DateTime sessionDate,
        Guid? movieSessionId = null)
    {
        var movieSession = MovieSession.Create(movieId: movie.Id,
            auditoriumId: redAuditorium.Id,
            sessionDate,
            redAuditorium.Seats.Count);

        movieSession = movieSession.SetKey(movieSessionId ?? Guid.NewGuid());

        await _context.MovieSessions.AddAsync(movieSession);


        await CreateMovieSessionSeats(redAuditorium, movieSession, _context);

        await _context.SaveChangesAsync();
    }


    private async Task CreateMovieSessionSeats(CinemaHall redAuditorium,
        MovieSession showtimeItem,
        CinemaContext context)
    {
        foreach (var seat in redAuditorium.Seats)
        {
            var showtimeSeat = MovieSessionSeat.Create(showtimeItem.Id, seat.Row, seat.SeatNumber, 15);

            await context.MovieSessionSeats.AddAsync(showtimeSeat);
        }
    }


    private static List<(short Row, short SeatNumber)> GenerateSeats(short rows, short seatsPerRow)
    {
        var seats = new List<(short Row, short SeatNumber)>();
        for (short r = 1; r <= rows; r++)
        for (short s = 1; s <= seatsPerRow; s++)
            seats.Add((Row: r, SeatNumber: s));

        return seats;
    }
}

public static class ReflectionExtensions
{
    public static T SetKey<T>(this T obj, Guid id)
    {
        var prop = obj.GetType().GetProperty("Id");
        prop.SetValue(obj, id);
        return obj;
    }
}