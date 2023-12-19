using CinemaTicketBooking.Api.Sockets.Abstractions;
using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Application.Abstractions.Services;
using CinemaTicketBooking.Application.MovieSessions.Queries;
using CinemaTicketBooking.Application.MovieSessionSeats;
using MediatR;
using Microsoft.AspNetCore.SignalR;


namespace CinemaTicketBooking.Api.Sockets;

public class MovieSessionSeatsNotifier(
    IHubContext<BookingManagementServiceHub, IBookingManagementStateUpdater> context,
    IMovieSessionSeatsDataCacheService movieSessionSeatsDataCacheService,
    Serilog.ILogger logger) : ICinemaHallSeatsNotifier
{
    //call when seat status changed
    public async Task UpdateAndNotifySubscribersAboutSeatUpdates(Guid movieSessionId)
    {
        var movieSessionSeatsData =
            await movieSessionSeatsDataCacheService.GetActualMovieSessionSeatsData(movieSessionId);

        if (movieSessionSeatsData is null)
        {
            logger.Error("Movie session seats not found:{@MovieSessionId}", movieSessionId);
            return;
        }

        await movieSessionSeatsDataCacheService.AddOrUpdateMovieSessionSeatsCache(movieSessionSeatsData);

        await context.Clients.Group(movieSessionId.ToString()).SentCinemaHallSeatsState(movieSessionSeatsData.Seats);

        logger.Debug("Updates have been sent to subscribers of movieSessionId:{@MovieSessionId}",
            movieSessionId);
    }


    //request last seat status
    public async Task SendSeatUpdatesDataToSpecificClient(Guid movieSessionId, string connectionId)
    {
        var movieSessionSeatsData = await movieSessionSeatsDataCacheService.GetMovieSessionSeatsData(movieSessionId);

        if (movieSessionSeatsData is null)
        {
            movieSessionSeatsData =
                await movieSessionSeatsDataCacheService.GetActualMovieSessionSeatsData(movieSessionId);
            
            if (movieSessionSeatsData is null)
            {
                logger.Error("Movie session seats not found:{@MovieSessionId}", movieSessionId);
                return;
            }
            
            await movieSessionSeatsDataCacheService.AddOrUpdateMovieSessionSeatsCache(movieSessionSeatsData);
        }


        await context.Clients.Client(connectionId).SentCinemaHallSeatsState(movieSessionSeatsData.Seats);

        logger.Debug(
            "Movie session seats MovieSessionId: {@MovieSessionId} sent to specific ConnectionId: {connectionId}",
            movieSessionId);
    }
}