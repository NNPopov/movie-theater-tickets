using System.Net;
using System.Text.Json.Serialization;
using CinemaTicketBooking.Api;
using CinemaTicketBooking.Api.Authentication;
using CinemaTicketBooking.Api.Database;
using CinemaTicketBooking.Api.Endpoints.Common;
using CinemaTicketBooking.Api.Sockets;
using CinemaTicketBooking.Api.WorkerServices;
using CinemaTicketBooking.Application;
using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Infrastructure;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Core;
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


var identityOptionsSection =
    builder.Configuration.GetSection(IdentityOptions.SectionName);

IdentityOptions identityOptions = new IdentityOptions();

identityOptionsSection.Bind(identityOptions);
services.Configure<IdentityOptions>(identityOptionsSection);

//builder.Services.AddSingleton<IValidateOptions<IdentityOptions>, IdentityOptionsValidator>();

services.AddApplicationServices()
    .AddInfrastructureServices(builder.Configuration)
    .AddApiServices(builder.Configuration)
    .AddWebSockets(builder.Configuration, logger)
    .AddKeyCloakAuthentication();

services.AddControllers(opt => { opt.OutputFormatters.RemoveType<HttpNoContentOutputFormatter>(); })
    .AddJsonOptions(options => { options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve; });

services.AddSingleton<RedisSubscriber>();
services.AddHostedService<RedisSubscriber>();

builder.Services.AddEndpointsApiExplorer();

services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("BookingDbContext"))
    .AddDbContextCheck<CinemaContext>("CinemaContext", HealthStatus.Unhealthy)
    .AddRedis(builder.Configuration.GetConnectionString("Redis"), "Redis", HealthStatus.Unhealthy);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwaggerExtensions(builder.Configuration);
}

app.UseExceptionHandler(options => { });
app.UseSerilogRequestLogging();
app.UseRouting();
app.UseCors(defaultCorsPolicy);

app.UseHealthChecks("/Health");
app.UseAuthentication();
app.UseAuthorization();

app.UseEndpoints(endpoints => { endpoints.MapHub<CinemaHallSeatsHub>("/cinema-hall-seats-hub"); });
app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
app.UseEndpoints(typeof(Program));


//app.Migrate();
SampleData.Initialize(app);


app.Run();
