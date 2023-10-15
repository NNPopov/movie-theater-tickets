﻿using CinemaTicketBooking.Application.Abstractions;
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
        
        var movie2 = Movie.Create(
            "Back to the Future",
            new DateTime(1985, 07, 03),
            "tt0088763",
            "Michael J. Fox, Christopher Lloyd, Lea Thompson, Crispin Glover"
        );
        
        var movie3 = Movie.Create(
            "Back to the Future Part II",
            new DateTime(1989, 10, 20),
            "tt0096874",
            "Michael J. Fox, Christopher Lloyd, Lea Thompson, Crispin Glover"
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
            seats: GenerateSeats(15, 12)
        );

        var showtimeItem = MovieSession.Create(movieId: movie.Id,
            auditoriumId: redAuditorium.Id,
            new DateTime(2023, 10, 20),
            //redAuditorium.Seats.Select(t => new SeatMovieSession(t.Row, t.SeatNumber)).ToList(),
            redAuditorium.Seats.Count);
        
        var showtimeItem2 = MovieSession.Create(movieId: movie.Id,
            auditoriumId: redAuditorium.Id,
            new DateTime(2023, 10, 21),
            //redAuditorium.Seats.Select(t => new SeatMovieSession(t.Row, t.SeatNumber)).ToList(),
            redAuditorium.Seats.Count);
        
        var showtimeItem3 = MovieSession.Create(movieId: movie2.Id,
            auditoriumId: whiteAuditorium.Id,
            new DateTime(2023, 10, 22),
            //redAuditorium.Seats.Select(t => new SeatMovieSession(t.Row, t.SeatNumber)).ToList(),
            whiteAuditorium.Seats.Count);
        
        var showtimeItem4 = MovieSession.Create(movieId: movie.Id,
            auditoriumId: blackAuditorium.Id,
            new DateTime(2023, 10, 22),
            //redAuditorium.Seats.Select(t => new SeatMovieSession(t.Row, t.SeatNumber)).ToList(),
            blackAuditorium.Seats.Count);
        
        var showtimeItem5 = MovieSession.Create(movieId: movie2.Id,
            auditoriumId: whiteAuditorium.Id,
            new DateTime(2023, 10, 23),
            //redAuditorium.Seats.Select(t => new SeatMovieSession(t.Row, t.SeatNumber)).ToList(),
            whiteAuditorium.Seats.Count);
        
        var showtimeItem6 = MovieSession.Create(movieId: movie3.Id,
            auditoriumId: whiteAuditorium.Id,
            new DateTime(2023, 10, 27),
            //redAuditorium.Seats.Select(t => new SeatMovieSession(t.Row, t.SeatNumber)).ToList(),
            whiteAuditorium.Seats.Count);
            
        showtimeItem = showtimeItem.SetKey( showtimeItemId);
            
        context.Movies.Add(movie);
        context.Movies.Add(movie2);
        context.Movies.Add(movie3);
        
        context.CinemaHalls.Add(redAuditorium);
        context.CinemaHalls.Add(blackAuditorium);
        context.CinemaHalls.Add(whiteAuditorium);

        context.MovieSessions.Add(showtimeItem);
        context.MovieSessions.Add(showtimeItem2);
        context.MovieSessions.Add(showtimeItem3);
        context.MovieSessions.Add(showtimeItem4);
        context.MovieSessions.Add(showtimeItem5);
        context.MovieSessions.Add(showtimeItem6);

        context.SaveChanges();

        CreateMovieSessionSeats(redAuditorium, showtimeItem, context);
        CreateMovieSessionSeats(redAuditorium, showtimeItem2, context);
        CreateMovieSessionSeats(whiteAuditorium, showtimeItem3, context);
        CreateMovieSessionSeats(blackAuditorium, showtimeItem4, context);
        CreateMovieSessionSeats(whiteAuditorium, showtimeItem5, context);
        CreateMovieSessionSeats(whiteAuditorium, showtimeItem6, context);
        
        context.SaveChanges();
    }

    private static void CreateMovieSessionSeats(CinemaHall redAuditorium, MovieSession showtimeItem, CinemaContext context)
    {
        foreach (var seat in redAuditorium.Seats)
        {
            var showtimeSeat = new MovieSessionSeat(showtimeItem.Id, seat.Row, seat.SeatNumber, 15);

            context.ShowtimeSeats.Add(showtimeSeat);
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