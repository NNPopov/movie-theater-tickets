using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Application.Common.Behaviours;
using CinemaTicketBooking.Application.ShoppingCarts.Command.SelectSeats;
using Microsoft.Extensions.DependencyInjection;

namespace CinemaTicketBooking.Application;
using System.Reflection;

public static class ConfigureServices
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddAutoMapper(Assembly.GetExecutingAssembly());

        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        
         
      
        services.AddMediatR(cfg => {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            //cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(UnhandledExceptionBehaviour<,>));
           // cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehaviour<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
            cfg.AddOpenBehavior(typeof(IdempotentCommandPipelineBehaviour<,>));
            
           // cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(PerformanceBehaviour<,>));
        });



        return services;
    }
}