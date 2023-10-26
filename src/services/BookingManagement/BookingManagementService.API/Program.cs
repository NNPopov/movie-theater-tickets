using System.Net;
using System.Text.Json.Serialization;
using CinemaTicketBooking.Api;
using CinemaTicketBooking.Api.Database;
using CinemaTicketBooking.Api.Endpoints.Common;
using CinemaTicketBooking.Api.Sockets;
using CinemaTicketBooking.Api.WorkerServices;
using CinemaTicketBooking.Application;
using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Infrastructure;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Options;
using Serilog;
using StackExchange.Redis;
using ILogger = Serilog.ILogger;

var defaultCorsPolicy = "defaultCorsPolicy";
var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<HostOptions>(options =>
{
    options.ServicesStartConcurrently = true;
    options.ServicesStopConcurrently = false;
});

var services = builder.Services;

Guid instanceId = Guid.NewGuid();

var logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.WithProperty("InstanceId", instanceId.ToString())
    .CreateLogger();

builder.Logging.AddSerilog(logger);
builder.Services.AddSingleton<ILogger>(logger);

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: defaultCorsPolicy,
        policy =>
        {
            policy.WithOrigins(builder.Configuration.GetSection("AllowedOrigins").Get<string[]>())
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
});

//builder.Services.AddSingleton<IValidateOptions<IdentityOptions>, IdentityOptionsValidator>();

services.AddApplicationServices()
    .AddInfrastructureServices(builder.Configuration)
    .AddApiServices(builder.Configuration);


services.AddScoped<ICinemaHallSeatsNotifier, CinemaHallSeatsNotifier>();
services.AddScoped<IShoppingCartNotifier, ShoppingCartNotifier>();
services.AddSingleton<IConnectionManager>(ConnectionManager.Factory());

var identityOptionsSection =
    builder.Configuration.GetSection(IdentityOptions.SectionName);

IdentityOptions identityOptions = new IdentityOptions();

identityOptionsSection.Bind(identityOptions);

services.Configure<IdentityOptions>(identityOptionsSection);

services.AddKeyCloakAuthentication(builder.Configuration);

services.AddControllers(opt => { opt.OutputFormatters.RemoveType<HttpNoContentOutputFormatter>(); })
    .AddJsonOptions(options => { options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve; });

services.AddSingleton<RedisSubscriber>();

services.AddHostedService<RedisSubscriber>();

builder
    .Services
    .AddSignalR()
    .AddStackExchangeRedis(builder.Configuration.GetConnectionString("Redis") ?? throw new InvalidOperationException()
        ,
        o =>
        {
            o.Configuration.ChannelPrefix = "CinemaBooking";
            o.ConnectionFactory = async writer =>
            {
                var config = new ConfigurationOptions
                {
                    
                    EndPoints = { builder.Configuration.GetConnectionString("Redis") },
                    AbortOnConnectFail = false,
                    
                };
                //config.EndPoints.Add(IPAddress.Loopback, 0);
                //config.SetDefaultPorts();
                var connection = await ConnectionMultiplexer.ConnectAsync(config, writer);
                connection.ConnectionFailed += (_, e) => { Console.WriteLine("Connection to Redis failed."); };
            
                if (!connection.IsConnected)
                {
                    Console.WriteLine("Did not connect to Redis.");
                }
                
               
            
                return connection;
            };
        }
    ); //.AddMessagePackProtocol();

builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwaggerExtensions(builder.Configuration);
}

app.UseExceptionHandler(options => { });
//app.UseSerilogRequestLogging();
app.UseRouting();


//app.MapHub<ShoppingCartHub>("/shopping-cart-hub");


app.UseCors(defaultCorsPolicy);
//app.MapHub<CinemaHallSeatsHub>("/cinema-hall-seats-hub");

app.UseEndpoints(endpoints =>
{
    endpoints.MapHub<CinemaHallSeatsHub>("/cinema-hall-seats-hub");
});

app.UseHealthChecks("/Health");
app.UseAuthentication();
app.UseAuthorization();

app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
app.UseEndpoints(typeof(Program));


SampleData.Initialize(app);


app.Run();