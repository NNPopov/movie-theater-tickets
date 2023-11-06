using System.Reflection;
using CinemaTicketBooking.Api.Infrastructure;
using CinemaTicketBooking.Api.Sockets;
using CinemaTicketBooking.Api.Sockets.Abstractions;
using CinemaTicketBooking.Application.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Serilog.Core;
using StackExchange.Redis;

namespace CinemaTicketBooking.Api;

public static class ApiApplicationBuilderExtensions
{
    public static WebApplication UseSwaggerExtensions(this WebApplication webApplication,
        IConfiguration configuration)
    {
        webApplication.UseSwagger()
            .UseSwaggerUI(options =>
            {
                var identityOptions = webApplication.Services.GetService<IOptions<IdentityOptions>>().Value;

                options.OAuthAppName(identityOptions.IdentityClientId);
                options.OAuthScopes(identityOptions.AuthScopes);
                options.OAuthScopeSeparator(" ");
                options.OAuth2RedirectUrl(identityOptions.RedirectUrl);
                options.OAuthClientId(identityOptions.IdentityClientId);
                options.OAuthUsePkce();
            });

        return webApplication;
    }
}

public static class ConfigureApiServices
{
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAutoMapper(Assembly.GetExecutingAssembly());
        services.AddDatabaseDeveloperPageExceptionFilter();
        services.AddExceptionHandler<CustomExceptionHandler>();

        services.AddSwaggerExtensions(configuration);


        return services;
    }


    public static IServiceCollection AddSwaggerExtensions(this IServiceCollection serviceCollection,
        IConfiguration configuration)
    {
        serviceCollection.AddSwaggerGen(options =>
        {
            using var sp = serviceCollection.BuildServiceProvider();
            var configs = sp.GetService<IOptions<IdentityOptions>>().Value;

            if (configs is null)
                throw new Exception("identityOptions is null");

            var scheme = new OpenApiSecurityScheme
            {
                In = ParameterLocation.Header,
                Name = "Authorization",
                Flows = new OpenApiOAuthFlows
                {
                    AuthorizationCode = new OpenApiOAuthFlow
                    {
                        AuthorizationUrl = new Uri(configs.AuthorizationUrl),
                        TokenUrl = new Uri(configs.TokenUrl),

                        Scopes = configs.AuthScopes.Select(d => new KeyValuePair<string, string>(d, d))
                            .ToDictionary(d => d.Key, f => f.Value),
                    }
                },
                Type = SecuritySchemeType.OAuth2
            };

            options.AddSecurityDefinition("OAuth", scheme);

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Id = "OAuth", Type = ReferenceType.SecurityScheme }
                    },
                    new List<string> { }
                }
            });
        });
        return serviceCollection;
    }


    public static IServiceCollection AddWebSockets(this IServiceCollection services,
        IConfiguration configuration,
        Logger logger)
    {
        services
            .AddSignalR(
                hubOptions => {
                     hubOptions.KeepAliveInterval = TimeSpan.FromSeconds(20);
                     hubOptions.MaximumReceiveMessageSize = 65_536;
                     hubOptions.HandshakeTimeout = TimeSpan.FromSeconds(15);
                     hubOptions.MaximumParallelInvocationsPerClient = 2;
                     hubOptions.EnableDetailedErrors = true; 
                    hubOptions.StreamBufferCapacity = 15;
                    if (hubOptions?.SupportedProtocols is not null)
                         {
                         foreach (var protocol in hubOptions.SupportedProtocols)
                             logger.Error($"SignalR supports {protocol} protocol.");
                         }
                     })
            .AddStackExchangeRedis(
                o =>
                {
                    o.Configuration.ChannelPrefix = "CinemaBooking";
                    o.ConnectionFactory = async writer =>
                    {
                        var config = new ConfigurationOptions
                        {
                            EndPoints =
                            {
                                configuration.GetConnectionString("Redis") ??
                                throw new InvalidOperationException()
                            },
                            AbortOnConnectFail = false,
                        };

                        var connection = await ConnectionMultiplexer.ConnectAsync(config, writer);
                        connection.ConnectionFailed += (_, e) =>
                        {
                            logger.Error("Connection to Redis failed. {@E}", e);
                        };

                        if (!connection.IsConnected)
                        {
                            logger.Error("Did not connect to Redis");
                        }

                        return connection;
                    };
                }
            );


        services.AddScoped<ICinemaHallSeatsNotifier, CinemaHallSeatsNotifier>()
            .AddScoped<IShoppingCartNotifier, ShoppingCartNotifier>()
            .AddSingleton<IConnectionManager>(t => ConnectionManager.Factory(t.GetRequiredService<ICacheService>()));

        return services;
    }
}