using System.Text.Json.Serialization;
using CinemaTicketBooking.Api;
using CinemaTicketBooking.Api.Authentication;
using CinemaTicketBooking.Api.Endpoints.Common;
using CinemaTicketBooking.Api.IntegrationEvents.Events;
using CinemaTicketBooking.Api.Sockets;
using CinemaTicketBooking.Api.WorkerServices;
using CinemaTicketBooking.Application;
using CinemaTicketBooking.Infrastructure;
using CinemaTicketBooking.Infrastructure.Data;
using CinemaTicketBooking.Infrastructure.EventBus;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Serilog;
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


var identityOptionsSection =
    builder.Configuration.GetSection(IdentityOptions.SectionName);

IdentityOptions identityOptions = new IdentityOptions();

identityOptionsSection.Bind(identityOptions);
services.Configure<IdentityOptions>(identityOptionsSection);

services.AddApplicationServices(builder.Configuration)
    .AddInfrastructureServices(builder.Configuration)
    .AddApiServices(builder.Configuration)
    .AddWebSockets(builder.Configuration, logger)
    .AddKeyCloakAuthentication();

services.AddControllers(opt => { opt.OutputFormatters.RemoveType<HttpNoContentOutputFormatter>(); })
    .AddJsonOptions(options => { options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve; });

services.AddSingleton<RedisSubscriber>();
services.AddSingleton<TimeWorker>();
services.AddHostedService<RedisSubscriber>();
services.AddHostedService<TimeWorker>();


builder.Services.AddEndpointsApiExplorer();

services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("BookingDbContext"))
    .AddDbContextCheck<CinemaContext>("CinemaContext", HealthStatus.Unhealthy)
    .AddRedis(builder.Configuration.GetConnectionString("Redis"), "Redis", HealthStatus.Unhealthy)
    .AddRabbitMQ(rabbitConnectionString: $"{builder.Configuration.GetConnectionString("EventBus")}:5672",
        name: "RabbitMQ",
        failureStatus: HealthStatus.Unhealthy);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwaggerExtensions(builder.Configuration);
}

app.UseExceptionHandler(options => { });
//app.UseSerilogRequestLogging();
app.UseRouting();
app.UseCors(defaultCorsPolicy);

app.UseHealthChecks("/Health");
app.UseAuthentication();
app.UseAuthorization();

app.MapHub<BookingManagementServiceHub>("/ws/cinema-hall-seats-hub",
    options =>
    {
        options.Transports =
            HttpTransportType.WebSockets |
            HttpTransportType.LongPolling |
            HttpTransportType.ServerSentEvents;
        options.CloseOnAuthenticationExpiration = false;
        options.ApplicationMaxBufferSize = 65_536;
        options.TransportMaxBufferSize = 65_536;
        options.MinimumProtocolVersion = 0;
        options.TransportSendTimeout = TimeSpan.FromSeconds(20);
        options.WebSockets.CloseTimeout = TimeSpan.FromSeconds(30);
        options.LongPolling.PollTimeout = TimeSpan.FromSeconds(20);
    }
);
app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
app.UseEndpoints(typeof(Program));


app.UseMigrationsEndPoint();
await app.InitialiseDatabaseAsync();


var eventBus = app.Services.GetRequiredService<IEventBus>();

eventBus
    .Subscribe<SeatExpiredSelectionIntegrationEvent, IIntegrationEventHandler<SeatExpiredSelectionIntegrationEvent>>();
eventBus
    .Subscribe<ShoppingCartExpiredIntegrationEvent, IIntegrationEventHandler<ShoppingCartExpiredIntegrationEvent>>();


app.Run();