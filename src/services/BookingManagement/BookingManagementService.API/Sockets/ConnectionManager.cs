﻿using CinemaTicketBooking.Api.Sockets.Abstractions;
using CinemaTicketBooking.Application.Abstractions.Services;

namespace CinemaTicketBooking.Api.Sockets;

public class ConnectionManager : IConnectionManager
{
    private readonly ICacheService _cacheService;
    
    private const string CacheKeyPrefix = "hub";

    private ConnectionManager(ICacheService cacheService)
    {
        _cacheService = cacheService;
    }
    
    
    public void AddConnections(Guid shoppingCartId, IEnumerable<string> connectionIds)
    {
        
        _cacheService.Set(GetCacheKey(shoppingCartId), connectionIds, new TimeSpan(2, 0, 0));
    }
    
    public void AddConnection(Guid shoppingCartId, string connectionId)
    {
        List<string> connectionIds = _cacheService.TryGet<List<string>>(GetCacheKey(shoppingCartId)).Result;

        if (connectionIds != null)
        {

            if (!connectionIds.Exists(t=>t.Equals(connectionId)))
            {
                connectionIds.Add(connectionId);
            }
        }
        else
        {
            connectionIds= new List<string> { connectionId };
        }
        
        _cacheService.Set(GetCacheKey(shoppingCartId), connectionIds, new TimeSpan(2, 0, 0));
    }

    private static string GetCacheKey(Guid shoppingCartId)
    {
        return $"{CacheKeyPrefix}:{shoppingCartId}";
    }

    public void RemoveByConnectionId(string connectionId)
    {
        
      //  var item = _signalRConnections.FirstOrDefault(t => t.connectionId == connectionId);
       // _signalRConnections.Remove(item);
    }
    
    public void RemoveShoppingCartId(Guid shoppingCartId)
    {
       _cacheService.Remove(GetCacheKey(shoppingCartId));
       
    }

    public void RemoveSubscriptionShoppingCartId(Guid shoppingCartId, string connectionId)
    {
        List<string> connectionIds = _cacheService.TryGet<List<string>>(GetCacheKey(shoppingCartId)).Result;

        if (connectionIds != null)
        {

            if (!connectionIds.Exists(t=>t.Equals(connectionId)))
            {
                connectionIds.Remove(connectionId);
            }
        }
        else
        {
            connectionIds= new List<string> { connectionId };
        }
        
        _cacheService.Set(GetCacheKey(shoppingCartId), connectionIds, new TimeSpan(2, 0, 0));
    }

    public IEnumerable<string> GetConnectionId(Guid shoppingCartId)
    {
        List<string> connectionIds = _cacheService.TryGet<List<string>>(GetCacheKey(shoppingCartId)).Result;

        if (connectionIds != null) return connectionIds;
        return new List<string>();
    }


    private static Lazy<ConnectionManager> Instance(ICacheService cacheService)
    {
      return  new Lazy<ConnectionManager>(() => new ConnectionManager(cacheService));
    }

    public static ConnectionManager Factory(ICacheService cacheService) => Instance(cacheService).Value;

}