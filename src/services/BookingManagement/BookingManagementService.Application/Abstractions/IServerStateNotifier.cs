namespace CinemaTicketBooking.Application.Abstractions;

public interface IServerStateNotifier
{
    Task SentServerState(ServerState serverState);
    
}

public record ServerState(DateTime ServerDateTime);