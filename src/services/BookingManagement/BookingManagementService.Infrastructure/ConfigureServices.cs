using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Infrastructure.Repositories;
using CinemaTicketBooking.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace CinemaTicketBooking.Infrastructure;

public static class ConfigureServices
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<IMovieSessionsRepository, MovieSessionsRepository>();
        services.AddScoped<ITicketsRepository, TicketsRepository>();
        services.AddScoped<IMoviesRepository, MoviesRepository>();
        services.AddScoped<ICinemaHallRepository, CinemaHallRepository>();
        services.AddScoped<ISeatStateRepository, SeatStateRepository>();
        services.AddScoped<IMovieSessionSeatRepository, MovieSessionSeatRepository>();
        services.AddScoped<IShoppingCartRepository, ShoppingCartRepository>();
        services.AddScoped<IIdempotencyService, IdempotencyService>();
        services.AddScoped<IDistributedLock, DistributedLock>();
        

        services.AddScoped<ICacheService, RedisCacheService>();


        var multiplexer = ConnectionMultiplexer.Connect(
            new ConfigurationOptions
            {
                EndPoints = { configuration.GetConnectionString("Redis") },
                AbortOnConnectFail = false
            }
        );
        
        
        services.AddDbContext<CinemaContext>(options =>
        {
            options.UseInMemoryDatabase("CinemaDb")
                //.EnableSensitiveDataLogging()
                .ConfigureWarnings(b => b.Ignore(InMemoryEventId.TransactionIgnoredWarning));
        });
        
        services.AddSingleton<IConnectionMultiplexer>(multiplexer);

        return services;
    }
}