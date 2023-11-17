namespace CinemaTicketBooking.Api.Sockets.Abstractions;

public interface IConnectionManager
{
    void AddConnection(Guid id, string connectionId);

    void AddConnections(Guid id, IEnumerable<string> connectionIds);
    void RemoveByConnectionId(string connectionId);
    void RemoveShoppingCartId(Guid shoppingCartId);
    IEnumerable<string> GetConnectionId(Guid shoppingCartId);
    
}