namespace CinemaTicketBooking.Api.Sockets;

public class ConnectionManager : IConnectionManager
{

    private ICollection<(Guid shoppingCartId, string connectionId)> _signalRConnections;

    private ConnectionManager()
    {
        _signalRConnections = new List<(Guid shoppingCartId, string connectionId)>();
    }
    
    
    public void AddConnection(Guid shoppingCartId, string connectionId)
    {
       
        
        _signalRConnections.Add((shoppingCartId, connectionId));
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
    
    public IEnumerable<string> GetConnectionId(Guid shoppingCartId)
    {
        return _signalRConnections.Where(t => t.shoppingCartId == shoppingCartId).Select(t=>t.connectionId);
    }
    
 
    private static  Lazy<ConnectionManager> Instance =>
        new Lazy<ConnectionManager>(()=> new ConnectionManager());

    public static ConnectionManager Factory() => Instance.Value;

}