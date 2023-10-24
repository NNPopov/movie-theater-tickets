namespace CinemaTicketBooking.Api.Sockets;

public class ConnectionManager : IConnectionManager
{

    private ICollection<(Guid shoppingCartId, string connectionId)> _signalRConnections;

    private ConnectionManager()
    {
        _signalRConnections = new List<(Guid shoppingCartId, string connectionId)>();
    }
    
    
    public void AddConnection(Guid id, string connectionId)
    {
        _signalRConnections.Add((id, connectionId));
    }
    
    public void RemoveByConnectionId(string connectionId)
    {
        var item = _signalRConnections.FirstOrDefault(t => t.connectionId == connectionId);
        _signalRConnections.Remove(item);
    }
    
    public void RemoveShoppingCartId(Guid shoppingCartId)
    {
        var item = _signalRConnections.FirstOrDefault(t => t.shoppingCartId == shoppingCartId);
        _signalRConnections.Remove(item);
    }
    
    public string GetConnectionId(Guid shoppingCartId)
    {
        return _signalRConnections.FirstOrDefault(t => t.shoppingCartId == shoppingCartId).connectionId;
    }
    
 
    private static  Lazy<ConnectionManager> Instance =>
        new Lazy<ConnectionManager>(()=> new ConnectionManager());

    public static ConnectionManager Factory() => Instance.Value;

}