using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Application.MovieSessions.Queries;
using Microsoft.AspNetCore.SignalR;

namespace CinemaTicketBooking.Api.Sockets;

public class CinemaHallSeatsNotifier
    (IHubContext<CinemaHallSeatsHub, ICinemaHallSeats> context) : ICinemaHallSeatsNotifier
{
    public async Task SendCinemaHallSeatsState(Guid movieSession,
        ICollection<MovieSessionSeatDto> seats)
    {
        try
        {
            await context.Clients.Group(movieSession.ToString()).SentState(seats);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
}