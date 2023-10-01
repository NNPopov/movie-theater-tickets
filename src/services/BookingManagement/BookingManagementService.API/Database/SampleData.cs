using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Domain.CinemaHalls;
using CinemaTicketBooking.Domain.Movies;
using CinemaTicketBooking.Domain.MovieSessions;
using CinemaTicketBooking.Domain.Seats;
using CinemaTicketBooking.Infrastructure;

namespace CinemaTicketBooking.Api.Database;

public class SampleData
{
    public static void Initialize(IApplicationBuilder app)
    {
        using var serviceScope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope();
        var context = serviceScope.ServiceProvider.GetService<CinemaContext>();
        var seatStateRepository = serviceScope.ServiceProvider.GetService<ISeatStateRepository>();
        

        context.Database.EnsureCreated();

        var movieId = Guid.Parse("E1FDE23C-E26D-44D2-88F8-20295125563E");
        var redAuditoriumId = Guid.Parse("97207AE2-E5DD-4084-903A-5655966CA31B");
        var showtimeItemId = Guid.Parse("9FF3C08F-64DF-4198-8004-44B93A031753");

        var movie = Movie.Create(
            "Inception",
            new DateTime(2010, 01, 14),
            "tt1375666",
            "Leonardo DiCaprio, Joseph Gordon-Levitt, Ellen Page, Ken Watanabe"
        );

        // movie = movie.SetKey( movieId);

        var redAuditorium = CinemaHall.Create(
            name:"Red",
            description: "Red",
            seats: GenerateSeats(28, 22)
        );

        redAuditorium = redAuditorium.SetKey( redAuditoriumId);

        var blackAuditorium = CinemaHall.Create(
            name:"Black",
            description: "Black",
            seats: GenerateSeats(21, 18)
        );

        var whiteAuditorium = CinemaHall.Create(
            name:"White",
            description: "White",
            seats: GenerateSeats(15, 21)
        );

        var showtimeItem = MovieSession.Create(movieId: movie.Id,
            auditoriumId: redAuditorium.Id,
            new DateTime(2023, 08, 20),
            //redAuditorium.Seats.Select(t => new SeatMovieSession(t.Row, t.SeatNumber)).ToList(),
            redAuditorium.Seats.Count);
            
        showtimeItem = showtimeItem.SetKey( showtimeItemId);
            
        context.Movies.Add(movie);
        context.Auditoriums.Add(redAuditorium);
        context.Auditoriums.Add(blackAuditorium);
        context.Auditoriums.Add(whiteAuditorium);

        context.MovieSessions.Add(showtimeItem);

        context.SaveChanges();

        //redAuditorium.Seats.Select(t => new Seat(t.Row, t.SeatNumber)).ToList();
        foreach (var seat in redAuditorium.Seats)
        {
            //var showtimeSeatKey = MovieSessionSeat.SeatKey(showtimeItem.ShoppingCartId,seat.SeatRow, seat.SeatNumber);
            
            var showtimeSeat = new MovieSessionSeat(showtimeItem.Id,seat.Row, seat.SeatNumber, 15);

            context.ShowtimeSeats.Add(showtimeSeat);

           //var result = seatStateRepository.SetAsync(showtimeSeatKey, showtimeSeat).Result;
        }
        
        context.SaveChanges();
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