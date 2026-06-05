using CinemaTicketBooking.Api.WorkerServices;
using CinemaTicketBooking.Application.Abstractions.Repositories;
using CinemaTicketBooking.Application.Abstractions.Services;
using CinemaTicketBooking.Domain.ShoppingCarts.Abstractions;
using CinemaTicketBooking.Infrastructure.EventBus;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace CinemaTicketBooking.Api.IntegrationTests.Infrastructure;

/// <summary>
/// First <see cref="WebApplicationFactory{TEntryPoint}"/> harness in the repo. Boots the real
/// ASP.NET pipeline — exception handler, MediatR behaviours (incl. CartOwnershipBehaviour),
/// CurrentUser over IHttpContextAccessor — while replacing only true external boundaries with
/// in-memory doubles: the Redis cart store, the cart-TTL manager, idempotency, and the RabbitMQ
/// event bus. Background workers and a real broker/database are not required.
/// </summary>
public sealed class BookingApiFactory : WebApplicationFactory<Program>
{
    public BookingApiFactory()
    {
        // These connection strings are absent from appsettings.json and are read eagerly by
        // AddInfrastructureServices during Program startup (before any deferred ConfigureAppConfiguration
        // runs), so they must be present as environment variables when WebApplication.CreateBuilder runs.
        // Redis connects with AbortOnConnectFail=false, so the value just has to parse — the cart store is
        // overridden with an in-memory double below and the real multiplexer is never used on the request path.
        Environment.SetEnvironmentVariable("ConnectionStrings__Redis", "localhost:6379");
        Environment.SetEnvironmentVariable("ConnectionStrings__EventBus", "localhost");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");

        builder.ConfigureTestServices(services =>
        {
            // Background workers need Redis pub/sub and the SignalR backplane — drop them.
            RemoveHostedService<RedisSubscriber>(services);
            RemoveHostedService<TimeWorker>(services);
            services.RemoveAll<RedisSubscriber>();
            services.RemoveAll<TimeWorker>();

            // External boundaries → in-memory doubles.
            services.RemoveAll<IActiveShoppingCartRepository>();
            services.AddSingleton<IActiveShoppingCartRepository, InMemoryActiveShoppingCartRepository>();

            services.RemoveAll<IShoppingCartLifecycleManager>();
            services.AddSingleton<IShoppingCartLifecycleManager, NoOpShoppingCartLifecycleManager>();

            services.RemoveAll<IIdempotencyService>();
            services.AddSingleton<IIdempotencyService, InMemoryIdempotencyService>();

            services.RemoveAll<IEventBus>();
            services.AddSingleton<IEventBus, NoOpEventBus>();

            // Make the test scheme the default so UseAuthentication populates HttpContext.User
            // from the X-Test-User header (the endpoints stay anonymous either way).
            services.AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        });
    }

    private static void RemoveHostedService<T>(IServiceCollection services)
        where T : IHostedService
    {
        var descriptors = services
            .Where(d => d.ServiceType == typeof(IHostedService) && d.ImplementationType == typeof(T))
            .ToList();

        foreach (var descriptor in descriptors)
            services.Remove(descriptor);
    }
}
