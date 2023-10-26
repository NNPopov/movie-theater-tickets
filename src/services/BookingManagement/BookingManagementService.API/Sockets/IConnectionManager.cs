namespace CinemaTicketBooking.Api.Sockets;

public interface IConnectionManager
{
    void AddConnection(Guid id, string connectionId);
    void RemoveByConnectionId(string connectionId);
    void RemoveShoppingCartId(Guid shoppingCartId);
    IEnumerable<string> GetConnectionId(Guid shoppingCartId);
}