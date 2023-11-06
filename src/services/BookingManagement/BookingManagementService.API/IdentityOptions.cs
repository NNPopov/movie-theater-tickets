using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace CinemaTicketBooking.Api;

// [OptionsValidator]
// public partial class IdentityOptionsValidator : IValidateOptions<IdentityOptions>
// {
//
// }

public class IdentityOptions
{
    public const string SectionName = "IdentityOptions";
    [Required]
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

