using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Application.MovieSessions.Queries;
using CinemaTicketBooking.Domain.MovieSessions;
using Microsoft.AspNetCore.SignalR;
using Polly;

namespace CinemaTicketBooking.Api.Sockets;

public class CinemaHallSeatsHub : Hub<ICinemaHallSeats>
{
    public async Task JoinGroup(Guid movieSession)
    {
        try
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, movieSession.ToString());
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            // throw;
        }
    }

    public async Task SendCinemaHallSeatsState(Guid movieSession,
        ICollection<MovieSessionSeatDto> seats)
    {
        try
        {
            await Clients.Group(movieSession.ToString()).SentState(seats);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            // throw;
        }
    }
}

public interface ICinemaHallSeats
{
    Task SentState(ICollection<MovieSessionSeatDto> seats);
}

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
            // throw;
        }
    }
}