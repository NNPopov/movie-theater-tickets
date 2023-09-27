using System.Reflection;
using System.Text.Json.Serialization;
using CinemaTicketBooking.Api.Infrastructure;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;

namespace CinemaTicketBooking.Api;

public static class ConfigureApiServices
{
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAutoMapper(Assembly.GetExecutingAssembly());
        services.AddDatabaseDeveloperPageExceptionFilter();
        services.AddExceptionHandler<CustomExceptionHandler>();

        services.AddSwaggerExtensions(configuration);

        services
            .AddHealthChecks()
            .AddRedis(configuration.GetConnectionString("Redis"), "Redis", HealthStatus.Unhealthy);

        return services;
    }

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

    public static IServiceCollection AddSwaggerExtensions(this IServiceCollection serviceCollection,
        IConfiguration configuration)
    {
        serviceCollection.AddSwaggerGen(options =>
        {
            using var sp = serviceCollection.BuildServiceProvider();
            var cofigs = sp.GetService<IOptions<IdentityOptions>>().Value;

            if (cofigs is null)
                throw new Exception("identityOptions is null");

            var scheme = new OpenApiSecurityScheme
            {
                In = ParameterLocation.Header,
                Name = "Authorization",
                Flows = new OpenApiOAuthFlows
                {
                    AuthorizationCode = new OpenApiOAuthFlow
                    {
                        AuthorizationUrl = new Uri(cofigs.AuthorizationUrl),
                        TokenUrl = new Uri(cofigs.TokenUrl),

                        Scopes = cofigs.AuthScopes.Select(d => new KeyValuePair<string, string>(d, d))
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
}

public class IdentityOptions
{
    public const string SectionName = "IdentityOptions";

    [JsonProperty("IssuerWellKnown")]
    [JsonPropertyName("IssuerWellKnown")]
    public string IssuerWellKnown { get; set; }


    [JsonProperty("ValidIssuer")]
    [JsonPropertyName("ValidIssuer")]
    public string ValidIssuer { get; set; }


    [JsonProperty("ValidateIssuerSigningKey")]
    [JsonPropertyName("ValidateIssuerSigningKey")]
    public bool ValidateIssuerSigningKey { get; set; }


    [JsonProperty("ValidateIssuer")]
    [JsonPropertyName("ValidateIssuer")]
    public bool ValidateIssuer { get; set; }


    [JsonProperty("ValidateLifetime")]
    [JsonPropertyName("ValidateLifetime")]
    public bool ValidateLifetime { get; set; }


    [JsonProperty("ValidAudience")]
    [JsonPropertyName("ValidAudience")]
    public string ValidAudience { get; set; }


    [JsonProperty("ValidateAudience")]
    [JsonPropertyName("ValidateAudience")]
    public bool ValidateAudience { get; set; }


    [JsonProperty("RoleClaimType")]
    [JsonPropertyName("RoleClaimType")]
    public string RoleClaimType { get; set; }


    [JsonProperty("ClientSecret")]
    [JsonPropertyName("ClientSecret")]
    public string ClientSecret { get; set; }


    [JsonProperty("RedirectUrl")]
    [JsonPropertyName("RedirectUrl")]
    public string RedirectUrl { get; set; }

    [JsonProperty("IdentityClientId")]
    [JsonPropertyName("IdentityClientId")]
    public string IdentityClientId { get; set; }


    [JsonProperty("AuthScopes")]
    [JsonPropertyName("AuthScopes")]
    public string[] AuthScopes { get; set; }


    [JsonProperty("AuthorizationUrl")]
    [JsonPropertyName("AuthorizationUrl")]
    public string AuthorizationUrl { get; set; }


    [JsonProperty("TokenUrl")]
    [JsonPropertyName("TokenUrl")]
    public string TokenUrl { get; set; }
}