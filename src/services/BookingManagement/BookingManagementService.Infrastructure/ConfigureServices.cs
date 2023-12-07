using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Application.Abstractions.Repositories;
using CinemaTicketBooking.Application.Abstractions.Services;
using CinemaTicketBooking.Domain.MovieSessions.Abstractions;
using CinemaTicketBooking.Domain.Seats.Abstractions;
using CinemaTicketBooking.Domain.Services;
using CinemaTicketBooking.Domain.ShoppingCarts.Abstractions;
using CinemaTicketBooking.Infrastructure.Data;
using CinemaTicketBooking.Infrastructure.EventBus;
using CinemaTicketBooking.Infrastructure.Repositories;
using CinemaTicketBooking.Infrastructure.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RabbitMQ.Client;
using Serilog;
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
        services.AddScoped<IShoppingCartSeatLifecycleManager, ShoppingCartSeatLifecycleManager>();
        services.AddScoped<IShoppingCartLifecycleManager, ShoppingCartLifecycleManager>();
        services.AddScoped<IMovieSessionSeatRepository, MovieSessionSeatRepository>();
        services.AddScoped<IActiveShoppingCartRepository, ActiveShoppingCartRepository>();
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
        
        services.AddSingleton<IRabbitMQPersistentConnection>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger>();

            var factory = new ConnectionFactory()
            {
                HostName = configuration.GetConnectionString("EventBus"),
                DispatchConsumersAsync = true
            };

            // if (!string.IsNullOrEmpty(eventBusSection["UserName"]))
            // {
            //     factory.UserName = eventBusSection["UserName"];
            // }
            //
            // if (!string.IsNullOrEmpty(eventBusSection["Password"]))
            // {
            //     factory.Password = eventBusSection["Password"];
            // }
            //
            // var retryCount = eventBusSection.GetValue("RetryCount", 5);

            return new DefaultRabbitMQPersistentConnection(factory, logger, 5);
        });

        services.AddSingleton<IEventBus, EventBusRabbitMQ>(sp =>
        {
            var subscriptionClientName = "booking";//eventBusSection.GetRequiredValue("SubscriptionClientName");
            var rabbitMQPersistentConnection = sp.GetRequiredService<IRabbitMQPersistentConnection>();
            var logger = sp.GetRequiredService<ILogger>();
            var eventBusSubscriptionsManager = sp.GetRequiredService<IEventBusSubscriptionsManager>();
            var retryCount = 5;// eventBusSection.GetValue("RetryCount", 5);

            return new EventBusRabbitMQ(rabbitMQPersistentConnection, 
                logger, 
                sp, 
                eventBusSubscriptionsManager, 
                subscriptionClientName, 
                retryCount);
        });
        
        services.AddSingleton<IEventBusSubscriptionsManager, InMemoryEventBusSubscriptionsManager>();
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