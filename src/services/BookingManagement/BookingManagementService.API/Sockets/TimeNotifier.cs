using CinemaTicketBooking.Api.Sockets.Abstractions;
using CinemaTicketBooking.Application.Abstractions;
using Microsoft.AspNetCore.SignalR;

namespace CinemaTicketBooking.Api.Sockets;

public class ServerStateNotifier(
    IHubContext<BookingManagementServiceHub, IBookingManagementStateUpdater> context,
    Serilog.ILogger logger) : IServerStateNotifier
{

    public async Task SentServerState(ServerState serverState)
    {
        try
        {
            await context.Clients.All.SentServerState(serverState);
        }
        catch (Exception e)
        {
            logger.Error(e, "Failed to sent ServerState");
        }
    }
}