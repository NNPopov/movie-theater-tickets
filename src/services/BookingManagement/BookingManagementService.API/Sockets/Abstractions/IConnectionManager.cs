namespace CinemaTicketBooking.Api.Sockets.Abstractions;

public interface IConnectionManager
{
    void AddConnection(Guid id, string connectionId);

    void AddConnections(Guid shoppingCartId, IEnumerable<string> connectionIds);
    void RemoveByConnectionId(string connectionId);
    void RemoveShoppingCartId(Guid shoppingCartId);
    
    void RemoveSubscriptionShoppingCartId(Guid shoppingCartId, string  connectionId);
    IEnumerable<string> GetConnectionId(Guid shoppingCartId);
    
}