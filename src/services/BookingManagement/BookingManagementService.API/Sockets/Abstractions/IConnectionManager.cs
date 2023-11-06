namespace CinemaTicketBooking.Api.Sockets.Abstractions;

public interface IConnectionManager
{
    void AddConnection(Guid id, string connectionId);
    void RemoveByConnectionId(string connectionId);
    void RemoveShoppingCartId(Guid shoppingCartId);
    IEnumerable<string> GetConnectionId(Guid shoppingCartId);
}