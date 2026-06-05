using System.Collections.Concurrent;
using CinemaTicketBooking.Application.Abstractions.Repositories;
using CinemaTicketBooking.Application.Abstractions.Services;
using CinemaTicketBooking.Domain.ShoppingCarts;
using CinemaTicketBooking.Domain.ShoppingCarts.Abstractions;
using CinemaTicketBooking.Infrastructure.EventBus;
using Newtonsoft.Json;

namespace CinemaTicketBooking.Api.IntegrationTests.Infrastructure;

/// <summary>
/// In-memory stand-in for the Redis-backed cart store. Mirrors the real
/// <c>ActiveShoppingCartRepository</c> exactly — including the Newtonsoft JSON round-trip — so a
/// loaded cart is a detached copy with no domain events, just like the production Redis path.
/// Returns <c>null</c> for a missing cart and <c>Guid.Empty</c> for an absent client mapping.
/// </summary>
public sealed class InMemoryActiveShoppingCartRepository : IActiveShoppingCartRepository
{
    private readonly ConcurrentDictionary<Guid, string> _carts = new();
    private readonly ConcurrentDictionary<Guid, Guid> _clientCarts = new();

    public Task<ShoppingCart> SaveAsync(ShoppingCart shoppingCart)
    {
        _carts[shoppingCart.Id] = JsonConvert.SerializeObject(shoppingCart);
        return Task.FromResult(shoppingCart);
    }

    public Task DeleteAsync(ShoppingCart shoppingCart)
    {
        _carts.TryRemove(shoppingCart.Id, out _);
        return Task.CompletedTask;
    }

    public Task<ShoppingCart> GetByIdAsync(Guid shoppingCartId)
    {
        if (!_carts.TryGetValue(shoppingCartId, out var json) || string.IsNullOrEmpty(json))
            return Task.FromResult<ShoppingCart>(null!);

        return Task.FromResult(JsonConvert.DeserializeObject<ShoppingCart>(json)!);
    }

    public Task<Guid> GetActiveShoppingCartByClientIdAsync(Guid clientId)
        => Task.FromResult(_clientCarts.TryGetValue(clientId, out var id) ? id : Guid.Empty);

    public Task SetClientActiveShoppingCartAsync(Guid clientId, Guid shoppingCartId)
    {
        _clientCarts[clientId] = shoppingCartId;
        return Task.CompletedTask;
    }
}

/// <summary>No-op cart TTL manager — the scenarios never depend on expiry.</summary>
public sealed class NoOpShoppingCartLifecycleManager : IShoppingCartLifecycleManager
{
    public Task DeleteAsync(Guid shoppingCartId) => Task.CompletedTask;

    public Task<SeatShoppingCart> GetAsync(Guid shoppingCartId) => Task.FromResult<SeatShoppingCart>(null!);

    public Task SetAsync(Guid shoppingCartId) => Task.CompletedTask;
}

/// <summary>In-memory idempotency record store (replaces the Redis-backed service for seeding).</summary>
public sealed class InMemoryIdempotencyService : IIdempotencyService
{
    private readonly ConcurrentDictionary<Guid, string> _requests = new();

    public Task<bool> RequestExistsAsync(Guid requestId) => Task.FromResult(_requests.ContainsKey(requestId));

    public Task CreateRequestAsync(Guid requestId, string name)
    {
        _requests[requestId] = name;
        return Task.CompletedTask;
    }
}

/// <summary>No-op event bus so the harness needs no RabbitMQ broker.</summary>
public sealed class NoOpEventBus : IEventBus
{
    public Task PublishAsync(IntegrationEvent @event, string? deduplicationHeader = null) => Task.CompletedTask;

    public Task SubscribeAsync<T, TH>()
        where T : IntegrationEvent
        where TH : IIntegrationEventHandler<T>
        => Task.CompletedTask;

    public void Unsubscribe<T, TH>()
        where TH : IIntegrationEventHandler<T>
        where T : IntegrationEvent
    {
    }
}
