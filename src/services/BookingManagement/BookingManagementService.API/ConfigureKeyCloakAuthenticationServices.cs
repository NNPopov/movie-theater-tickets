using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Polly;
using Polly.Retry;

namespace CinemaTicketBooking.Api;

public static class ConfigureKeyCloakAuthenticationServices
{
    
    static AsyncRetryPolicy retryPolicy = Policy
        .Handle<HttpRequestException>() 
        .WaitAndRetryAsync(3, 
            retryAttempt => TimeSpan.FromSeconds(Math.Pow(1, retryAttempt)), 
            (exception, delay, retryCount, context) =>
            {
                Console.WriteLine(
                    $"Attemp {retryCount}: Delay {delay}: {exception.Message}");
            });
    
    public static IServiceCollection AddKeyCloakAuthentication(this IServiceCollection serviceCollection,
        IConfiguration configuration)
    {
        serviceCollection.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(async o =>
            {
                await using var serviceProvider = serviceCollection.BuildServiceProvider();
                var identityOptions = serviceProvider.GetRequiredService<IOptions<IdentityOptions>>().Value;

                var rsaKey = await GetRsaSecurityKey(identityOptions);


                o.RequireHttpsMetadata = false;
                o.TokenValidationParameters.ValidateAudience = false;
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = identityOptions.ValidIssuer,
                    ValidateAudience = false,
                    ValidateIssuer = true,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    ValidateIssuerSigningKey = true,
                    ValidateSignatureLast = false,
                    IssuerSigningKey = rsaKey
                };
                //o.Authority = "https://localhost:9443";
                o.Audience = identityOptions.IdentityClientId;
            });

        return serviceCollection;
    }

    private static async Task<RsaSecurityKey> GetRsaSecurityKey(IdentityOptions identityOptions)
    {
        using var httpClient = new HttpClient();

        var wellKnownData = await retryPolicy.ExecuteAsync(async () =>
        {
            var result = await httpClient
                .GetStringAsync(identityOptions.IssuerWellKnown);
            return result;
        });


        var wellKnownObj = JsonConvert.DeserializeObject<JObject>(wellKnownData);
        var jwksUri = wellKnownObj.Value<string>("jwks_uri");

        var jwksData = httpClient.GetStringAsync(jwksUri).Result;
        var jwksObj = JsonConvert.DeserializeObject<JwksRoot>(jwksData);
        var keys = jwksObj.Keys.FirstOrDefault(t => t.Alg == "RS256");
        var rsaKey = BuildRsaKey(keys.X5C.FirstOrDefault());
        return rsaKey;

        static RsaSecurityKey BuildRsaKey(string publicKeyJWT)
        {
            var certificate = new X509Certificate2(Convert.FromBase64String(publicKeyJWT));

            return new RsaSecurityKey(certificate.GetRSAPublicKey());
        }
    }
}

public class Key
{
    [JsonProperty("kid")] public string Kid { get; set; }

    [JsonProperty("kty")] public string Kty { get; set; }

    [JsonProperty("alg")] public string Alg { get; set; }

    [JsonProperty("use")] public string Use { get; set; }

    [JsonProperty("n")] public string N { get; set; }

    [JsonProperty("e")] public string E { get; set; }

    [JsonProperty("x5c")] public List<string> X5C { get; set; }

    [JsonProperty("x5t")] public string X5T { get; set; }

    [JsonProperty("x5t#S256")] public string X5TS256 { get; set; }
}

public class JwksRoot
{
    [JsonProperty("keys")] public List<Key> Keys { get; set; }
}