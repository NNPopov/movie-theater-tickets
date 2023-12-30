using System.Diagnostics.Metrics;
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
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using Serilog.Sinks.OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
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


Guid instanceId = Guid.NewGuid();

var logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.WithProperty("InstanceId", instanceId.ToString())
    // .WriteTo.OpenTelemetry(options =>
    // {
    //     options.Endpoint = "http://otel-collector:4318/v1/logs";
    //     options.Protocol = OtlpProtocol.HttpProtobuf;
    // })
    .CreateLogger();

builder.Logging.AddSerilog(logger);
builder.Services.AddSingleton<ILogger>(logger);


var services = builder.Services;

Meter MyMeter = new("BookingManagementService", "1.0");

var requestSizeCounter = MyMeter.CreateCounter<long>("request_size", description: "The size of HTTP requests.");
var responseSizeCounter = MyMeter.CreateCounter<long>("response_size", description: "The size of HTTP responses.");



services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(serviceName: builder.Environment.ApplicationName))
    .WithTracing(options =>
        options.AddOtlpExporter(builder => { builder.Endpoint = new Uri("http://otel-collector:4317"); }
        ))
    .WithMetrics(builder =>
    {
        builder
            //.AddRuntimeInstrumentation()
            .AddHttpClientInstrumentation()
            .AddAspNetCoreInstrumentation()
            .AddRuntimeInstrumentation()
            // .AddPrometheusExporter();
            .AddOtlpExporter((exporterOptions, readerOptions) =>
            {
                exporterOptions.Endpoint = new Uri("http://otel-collector:4317"); 
                readerOptions.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = 10_000;
            });

        builder.AddMeter("Microsoft.AspNetCore.Hosting",
            "Microsoft.AspNetCore.Server.Kestrel",
            MyMeter.Name);


        // builder.AddOtlpExporter(options =>
        // {
        //     options.Endpoint = new Uri("http://otel-collector:4317");
        //     // options.Endpoint = "http://otel-collector:4317";
        // });

        builder.AddView("http.server.request.duration",
            new ExplicitBucketHistogramConfiguration
            {
                Boundaries = new double[]
                {
                    0, 0.005, 0.01, 0.025, 0.05,
                    0.075, 0.1, 0.25, 0.5, 0.75, 1, 2.5, 5, 7.5, 10
                }
            });
    });

// services.ConfigureOpenTelemetryTracerProvider((serviceProvider, traceProvider) =>
// {
//     traceProvider.AddOtlpExporter(
//         //     options =>
//         // {
//         //     options.Endpoint = new Uri("http://otel-collector:4318/v1/logs");
//         //     options.Protocol = OtlpExportProtocol.HttpProtobuf;
//         //     // options.Endpoint = "http://otel-collector:4317";
//         // }
//     );
//     traceProvider.AddRedisInstrumentation(
//         serviceProvider.GetRequiredService<IConnectionMultiplexer>());
// });

// var meterProvider = Sdk.CreateMeterProviderBuilder()
//     .AddAspNetCoreInstrumentation()
//     .AddPrometheusExporter()
//     .Build();

//services.AddSingleton<MeterProvider>(meterProvider);

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
    .AddIntegrationEvents(builder.Configuration, logger)
    .AddKeyCloakAuthentication();

services.Configure<KestrelServerOptions>(options => { options.AllowSynchronousIO = true; }
);


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

// app.UseOpenTelemetryPrometheusScrapingEndpoint();
// app.MapPrometheusScrapingEndpoint();


app.Use(async (context, next) =>
{
    var requestSize = context.Request.ContentLength ?? 0;
    requestSizeCounter.Add(requestSize);

    await next();

    var responseSize = context.Response.ContentLength ?? 0;
    responseSizeCounter.Add(responseSize);
});

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