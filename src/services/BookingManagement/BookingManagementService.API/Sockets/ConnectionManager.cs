using CinemaTicketBooking.Application.Abstractions;

namespace CinemaTicketBooking.Api.Sockets;

public class ConnectionManager : IConnectionManager
{
    private ICacheService _cacheService;

    private ICollection<(Guid shoppingCartId, string connectionId)> _signalRConnections;

    private ConnectionManager(ICacheService cacheService)
    {
        _cacheService = cacheService;
        _signalRConnections = new List<(Guid shoppingCartId, string connectionId)>();
    }
    
    
    public void AddConnection(Guid shoppingCartId, string connectionId)
    {
        List<string> connectionIds = _cacheService.TryGet<List<string>>($"hub:{shoppingCartId}").Result;
        
        if(connectionIds != null)
            connectionIds.Add(connectionId);
        else
        {
            connectionIds= new List<string> { connectionId };
        }
        
        _cacheService.Set($"hub:{shoppingCartId}", connectionIds, new TimeSpan(2, 0, 0));
        //_signalRConnections.Add((shoppingCartId, connectionId));
    }
    
    public void RemoveByConnectionId(string connectionId)
    {
        
      //  var item = _signalRConnections.FirstOrDefault(t => t.connectionId == connectionId);
       // _signalRConnections.Remove(item);
    }
    
    public void RemoveShoppingCartId(Guid shoppingCartId)
    {
       _cacheService.Remove($"hub:{shoppingCartId}");
        
       var item = _signalRConnections.FirstOrDefault(t => t.shoppingCartId == shoppingCartId);
        _signalRConnections.Remove(item);
    }
    
    public IEnumerable<string> GetConnectionId(Guid shoppingCartId)
    {
        List<string> connectionIds = _cacheService.TryGet<List<string>>($"hub:{shoppingCartId}").Result;

        if (connectionIds != null) return connectionIds;
        return new List<string>();
        // return _signalRConnections.Where(t => t.shoppingCartId == shoppingCartId).Select(t=>t.connectionId);
    }


    private static Lazy<ConnectionManager> Instance(ICacheService cacheService)
    {
      return  new Lazy<ConnectionManager>(() => new ConnectionManager(cacheService));
    }

    public static ConnectionManager Factory(ICacheService cacheService) => Instance(cacheService).Value;

}