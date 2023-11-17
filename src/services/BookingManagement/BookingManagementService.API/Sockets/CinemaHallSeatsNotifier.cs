using CinemaTicketBooking.Api.Sockets.Abstractions;
using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Application.Abstractions.Services;
using CinemaTicketBooking.Application.MovieSessions.Queries;
using Microsoft.AspNetCore.SignalR;


namespace CinemaTicketBooking.Api.Sockets;

public class CinemaHallSeatsNotifier
(IHubContext<CinemaHallSeatsHub, IBookingManagementStateUpdater> context,
    ICacheService cacheService,
    Serilog.ILogger logger) : ICinemaHallSeatsNotifier
{
    public async Task SentCinemaHallSeatsState(Guid movieSessionId,
        ICollection<MovieSessionSeatDto> seats)
    {
        try
        {
            var movieSessionSeatsKey = $"MovieSessionSeats:{movieSessionId}";

            await cacheService.Set(movieSessionSeatsKey, seats, new TimeSpan(0, 5, 0));

            await context.Clients.Group(movieSessionId.ToString()).SentCinemaHallSeatsState(seats);

            logger.Debug("Updates have been sent to subscribers of movieSessionId:{@MovieSessionId}",
                movieSessionId);
        }
        catch (Exception e)
        {
            logger.Error(e, "Failed to sent MovieSessionSeatState");
        }
    }
}