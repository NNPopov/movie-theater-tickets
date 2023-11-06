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
    }
}

public class SampleDataInitializer
{
    private readonly CinemaContext _context;
    private readonly ILogger _logger;

    public SampleDataInitializer(CinemaContext context, ILogger logger)
    {
        _context = context;
        _logger = logger;
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

        // movie = movie.SetKey( movieId);

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


        var showtimeItem = MovieSession.Create(movieId: movie.Id,
            auditoriumId: redAuditorium.Id,
            new DateTime(2023, 11, 20),
            //redAuditorium.Seats.Select(t => new SeatMovieSession(t.Row, t.SeatNumber)).ToList(),
            redAuditorium.Seats.Count);

        showtimeItem = showtimeItem.SetKey(movieSessionId);

        var showtimeItem2 = MovieSession.Create(movieId: movie.Id,
            auditoriumId: redAuditorium.Id,
            new DateTime(2023, 11, 21),
            //redAuditorium.Seats.Select(t => new SeatMovieSession(t.Row, t.SeatNumber)).ToList(),
            redAuditorium.Seats.Count);

        showtimeItem2 = showtimeItem2.SetKey(movieSessionId2);

        var showtimeItem3 = MovieSession.Create(movieId: movie2.Id,
            auditoriumId: whiteAuditorium.Id,
            new DateTime(2023, 11, 22),
            whiteAuditorium.Seats.Count);

        showtimeItem3 = showtimeItem3.SetKey(movieSessionId3);

        var showtimeItem4 = MovieSession.Create(movieId: movie.Id,
            auditoriumId: blackAuditorium.Id,
            new DateTime(2023, 11, 22),
            blackAuditorium.Seats.Count);
        showtimeItem4 = showtimeItem4.SetKey(movieSessionId4);

        var showtimeItem5 = MovieSession.Create(movieId: movie2.Id,
            auditoriumId: whiteAuditorium.Id,
            new DateTime(2023, 11, 23),
            whiteAuditorium.Seats.Count);
        showtimeItem5 = showtimeItem5.SetKey(movieSessionId5);

        var showtimeItem6 = MovieSession.Create(movieId: movie3.Id,
            auditoriumId: whiteAuditorium.Id,
            new DateTime(2023, 11, 27),
            whiteAuditorium.Seats.Count);

        showtimeItem6 = showtimeItem6.SetKey(movieSessionId6);

        _context.Movies.Add(movie);
        _context.Movies.Add(movie2);
        _context.Movies.Add(movie3);
        await _context.SaveChangesAsync();

        _context.CinemaHalls.Add(redAuditorium);
        _context.CinemaHalls.Add(blackAuditorium);
        _context.CinemaHalls.Add(whiteAuditorium);
        await _context.SaveChangesAsync();

        _context.MovieSessions.Add(showtimeItem);
        _context.MovieSessions.Add(showtimeItem2);
        _context.MovieSessions.Add(showtimeItem3);
        _context.MovieSessions.Add(showtimeItem4);
        _context.MovieSessions.Add(showtimeItem5);
        _context.MovieSessions.Add(showtimeItem6);

        await _context.SaveChangesAsync();

        CreateMovieSessionSeats(redAuditorium, showtimeItem, _context);
        CreateMovieSessionSeats(redAuditorium, showtimeItem2, _context);
        CreateMovieSessionSeats(whiteAuditorium, showtimeItem3, _context);
        CreateMovieSessionSeats(blackAuditorium, showtimeItem4, _context);
        CreateMovieSessionSeats(whiteAuditorium, showtimeItem5, _context);
        CreateMovieSessionSeats(whiteAuditorium, showtimeItem6, _context);


        await _context.SaveChangesAsync();
    }


    private static void CreateMovieSessionSeats(CinemaHall redAuditorium, MovieSession showtimeItem,
        CinemaContext context)
    {
        foreach (var seat in redAuditorium.Seats)
        {
            var showtimeSeat = MovieSessionSeat.Create(showtimeItem.Id, seat.Row, seat.SeatNumber, 15);

            context.MovieSessionSeats.Add(showtimeSeat);
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