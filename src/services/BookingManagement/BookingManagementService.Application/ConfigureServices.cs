﻿using CinemaTicketBooking.Application.Common.Behaviours;
using CinemaTicketBooking.Application.Common.Events;
using CinemaTicketBooking.Application.ShoppingCarts;
using CinemaTicketBooking.Application.ShoppingCarts.Events.SeatStatusUpdated;
using CinemaTicketBooking.Domain.ShoppingCarts;
using CinemaTicketBooking.Domain.ShoppingCarts.Abstractions;
using CinemaTicketBooking.Domain.ShoppingCarts.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CinemaTicketBooking.Application;

using System.Reflection;

public static class ConfigureServices
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<ShoppingCartConfiguration>().Bind(configuration.GetSection("ShoppingCartConfiguration"))
            .Validate(options =>
                {
                    var properties = typeof(ShoppingCartConfiguration).GetProperties();
                    foreach (var property in properties)
                    {
                        if (property.GetValue(options) == null)
                            return false;
                    }

                    return true;
                }, $"None of the {nameof(ShoppingCartConfiguration)} properties can be empty")
            .ValidateOnStart();


        services.AddAutoMapper(Assembly.GetExecutingAssembly());

        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());


        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            //cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(UnhandledExceptionBehaviour<,>));
            // cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehaviour<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
            cfg.AddOpenBehavior(typeof(IdempotentCommandPipelineBehaviour<,>));


            // cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(PerformanceBehaviour<,>));
        });

        services.AddTransient(typeof(INotificationHandler<BaseApplicationEvent<SeatAddedToShoppingCartDomainEvent>>),
            typeof(ShoppingCartUpdatedEventHandler<SeatAddedToShoppingCartDomainEvent>));
        services.AddTransient(
            typeof(INotificationHandler<BaseApplicationEvent<SeatRemovedFromShoppingCartDomainEvent>>),
            typeof(ShoppingCartUpdatedEventHandler<SeatRemovedFromShoppingCartDomainEvent>));
        services.AddTransient(typeof(INotificationHandler<BaseApplicationEvent<ShoppingCartCreatedDomainEvent>>),
            typeof(ShoppingCartUpdatedEventHandler<ShoppingCartCreatedDomainEvent>));
        services.AddTransient(typeof(INotificationHandler<BaseApplicationEvent<ShoppingCartReservedDomainEvent>>),
            typeof(ShoppingCartUpdatedEventHandler<ShoppingCartReservedDomainEvent>));
        services.AddTransient(typeof(INotificationHandler<BaseApplicationEvent<ShoppingCartPurchaseDomainEvent>>),
            typeof(ShoppingCartUpdatedEventHandler<ShoppingCartPurchaseDomainEvent>));
        services.AddTransient(typeof(INotificationHandler<BaseApplicationEvent<ShoppingCartCleanedDomainEvent>>),
            typeof(ShoppingCartUpdatedEventHandler<ShoppingCartCleanedDomainEvent>));


        services.AddScoped<IDataHasher, DataHasher>();

        return services;
    }
}