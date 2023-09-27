using System.Security.Cryptography.X509Certificates;
using System.Text.Json.Serialization;
using CinemaTicketBooking.Api;
using CinemaTicketBooking.Api.Database;
using CinemaTicketBooking.Api.Endpoints.Common;
using CinemaTicketBooking.Api.WorkerServices;
using CinemaTicketBooking.Application;
using CinemaTicketBooking.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using ILogger = Serilog.ILogger;

//using Newtonsoft.Json;

//using ILogger = Serilog.ILogger;

var builder = WebApplication.CreateBuilder(args);

//var localhostHTTPUrls = builder.Configuration.GetSection("ASPNETCORE_URLS").Value.Split(";");

// var localhostHTTPSports = localhostHTTPUrls.Select(t => (Int32.Parse(t!.Split(new Char[] { ':' })[2]),
//     t!.Split(new Char[] { ':' })[0]));


// builder.WebHost.ConfigureKestrel((context, options) =>
// {
//     foreach (var localhostHTTPSport in localhostHTTPSports)
//     {
//         options.Listen(IPAddress.Any, localhostHTTPSport.Item1, listenOptions =>
//         {
//             listenOptions.Protocols = HttpProtocols.Http1AndHttp2AndHttp3;
//             if (localhostHTTPSport.Item2.Equals("https", StringComparison.CurrentCultureIgnoreCase))
//                 listenOptions.UseHttps();
//         });
//     }
// });
//builder.WebHost.UseUrls();

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

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

services.AddApiServices(builder.Configuration);

var identityOptionsSection =
    builder.Configuration.GetSection(IdentityOptions.SectionName);

IdentityOptions identityOptions = new IdentityOptions();

identityOptionsSection.Bind(identityOptions);

services.Configure<IdentityOptions>(identityOptionsSection);

services.AddKeyCloakAuthentication(builder.Configuration);


services.AddControllers(opt => // or AddMvc()
    {
        // remove formatter that turns nulls into 204 - No Content responses
        // this formatter breaks Angular's Http response JSON parsing
        opt.OutputFormatters.RemoveType<HttpNoContentOutputFormatter>();
    })
    // .AddNewtonsoftJson(x =>
    //     x.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore)
    .AddJsonOptions(options => { options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve; });

services.AddHostedService<RedisWorker>();


// builder.ConfigureServices(services =>
//     {
//         services.Configure<HostOptions>(options =>
//         {
//             options.ServicesStartConcurrently = true;
//             options.ServicesStopConcurrently = true;
//         });
//         services.AddHostedService<WorkerOne>();
//         services.AddHostedService<WorkerTwo>();
//     })
//     .Build();

//services.AddHttpClient();

builder.Services.AddEndpointsApiExplorer();


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwaggerExtensions(builder.Configuration);
}

//app.UseHttpsRedirection();

//app.UseSerilogRequestLogging();
app.UseRouting();
app.UseHealthChecks("/Health");
app.UseAuthentication();
app.UseAuthorization();
app.UseExceptionHandler(options => { });
app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
app.UseEndpoints(typeof(Program));

SampleData.Initialize(app);

app.Run();

