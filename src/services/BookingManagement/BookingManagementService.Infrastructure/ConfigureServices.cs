using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Infrastructure.Repositories;
using CinemaTicketBooking.Infrastructure.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace CinemaTicketBooking.Infrastructure;

public static class ConfigureServices
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<IMovieSessionsRepository, MovieSessionsRepository>();
        //services.AddScoped<ITicketsRepository, TicketsRepository>();
        services.AddScoped<IMoviesRepository, MoviesRepository>();
        services.AddScoped<ICinemaHallRepository, CinemaHallRepository>();
        services.AddScoped<ISeatStateRepository, SeatStateRepository>();
        services.AddScoped<IMovieSessionSeatRepository, MovieSessionSeatRepository>();
        services.AddScoped<IShoppingCartRepository, ShoppingCartRepository>();
        services.AddScoped<IIdempotencyService, IdempotencyService>();
        services.AddScoped<IDistributedLock, DistributedLock>();
        services.AddScoped<IDomainEventTracker, DomainEventTracker>();
        services.AddSingleton<ICacheService, RedisCacheService>();

        
        var multiplexer = ConnectionMultiplexer.Connect(
            new ConfigurationOptions
            {
                EndPoints = { configuration.GetConnectionString("Redis") },
                AbortOnConnectFail = false
            }
        );
        
        multiplexer.ConnectionFailed += (_, e) => { Console.WriteLine("Connection to Redis failed."); };
            
        if (!multiplexer.IsConnected)
        {
            Console.WriteLine("Did not connect to Redis.");
        }
        services.AddSingleton<IConnectionMultiplexer>(multiplexer);        
        
        services.AddDbContextPool<CinemaContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("BookingDbContext"))
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
                .EnableSensitiveDataLogging();
        });
        

        return services;
    }

    public static WebApplication Migrate(this WebApplication app)
    {
        var options = new DbContextOptionsBuilder<CinemaContext>()
            .UseNpgsql(app.Configuration.GetConnectionString("BookingDbContext")).Options;
        
        using var dbContext = new CinemaContext(options);
    
        dbContext.Database.Migrate();
    
        return app;
    }
}