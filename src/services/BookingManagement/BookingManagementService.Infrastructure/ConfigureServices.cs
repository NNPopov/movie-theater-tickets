using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Application.Abstractions.Services;
using CinemaTicketBooking.Domain.MovieSessions.Abstractions;
using CinemaTicketBooking.Domain.Seats.Abstractions;
using CinemaTicketBooking.Domain.Services;
using CinemaTicketBooking.Infrastructure.Data;
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
        services.AddScoped<MovieSessionSeatService>();
        

        var redisConnectionString = configuration.GetConnectionString("Redis");
        var multiplexer = ConnectionMultiplexer.Connect(
            new ConfigurationOptions
            {
                EndPoints = { redisConnectionString },
                AbortOnConnectFail = false
            }
        );
        
        multiplexer.ConnectionFailed += (_, e) => { Console.WriteLine("Connection to Redis failed."); };
            
        if (!multiplexer.IsConnected)
        {
            Console.WriteLine("Did not connect to Redis.");
        }
        services.AddSingleton<IConnectionMultiplexer>(multiplexer);


        var cinemaContextConnectionString = configuration.GetConnectionString("BookingDbContext");
        
        services.AddDbContextPool<CinemaContext>(options =>
        {
            options.UseNpgsql(cinemaContextConnectionString)
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
                .EnableSensitiveDataLogging();
        });
        services.AddScoped<ICinemaContext>(provider => provider.GetRequiredService<CinemaContext>());
        services.AddScoped<SampleDataInitializer>();
        

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