using System.Text;
using Newtonsoft.Json;
using Polly;
using Polly.Retry;

namespace CinemaTicketBooking.Application.LoadTests
{
    public static class HttpSecClient
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
        // public static async Task<string> GetAdminTokenResponse(string SSOUrl, string userName)
        // {
        //     var client = new HttpClient();
        //     var disco = await client.GetDiscoveryDocumentAsync(SSOUrl);
        //
        //     if (disco.IsError) throw new Exception(disco.Error);
        //
        //     var tokenRequest = new PasswordTokenRequest()
        //     {
        //         Address = disco.TokenEndpoint,
        //         ClientId = "fhcadminnativeapp",
        //         ClientSecret = "secret",
        //         Scope = "fhcadminapi roles openid profile IdentityServerApi",
        //         UserName = userName,
        //         Password = @"Passdr0W/",
        //     };
        //
        //     var tokenClient = new HttpClient();
        //     var tokenResult = await tokenClient.RequestPasswordTokenAsync(tokenRequest);
        //
        //     return tokenResult.AccessToken;
        // }
        //
        // public static async Task<string> GetClientTokenResponse(string SSOUrl, string userName, string password = default)
        // {
        //     var client = new HttpClient();
        //     var disco = await client.GetDiscoveryDocumentAsync(SSOUrl);
        //
        //     if (disco.IsError) throw new Exception(disco.Error);
        //
        //     var tokenRequest = new PasswordTokenRequest()
        //     {
        //         Address = disco.TokenEndpoint,
        //         ClientId = "fhcclientnativeapp",
        //         ClientSecret = "secret",
        //         Scope = "fhcclientapi roles openid profile IdentityServerApi",
        //         UserName = userName,
        //         Password = password ?? @"Passdr0W/",
        //     };
        //
        //     var tokenClient = new HttpClient();
        //     var tokenResult = await tokenClient.RequestPasswordTokenAsync(tokenRequest);
        //
        //     Console.WriteLine(tokenResult.HttpStatusCode);
        //     Console.WriteLine(tokenResult.AccessToken);
        //     return tokenResult.AccessToken;
        // }

        public static async Task<HttpResponseMessage> PostJsonAsync(this HttpClient httpClient,
            string requestUri,
            string request)
        {
            var jsonRequest = request.ToHttpContent();
            return await httpClient.PostAsync(requestUri, jsonRequest);
        }

        public static async Task<HttpResponseMessage> PutJsonAsync(this HttpClient httpClient,
            string requestUri,
            string request)
        {
            var jsonRequest = request.ToHttpContent();
            return await httpClient.PutAsync(requestUri, jsonRequest);
        }

        public static async Task<HttpResponseMessage> GetJsonAsync(this HttpClient httpClient,
            string requestUri)
        {
            return await httpClient.GetAsync(requestUri);
        }

        public static async Task<HttpResponseMessage> PostAsync<R>(this HttpClient httpClient,
            string requestUri,
            R request) where R : class
        {
            var jsonRequest = request.ToHttpContent();
            return await httpClient.PostAsync(requestUri, jsonRequest);
        }

        public static async Task<T> PostAsync<T>(this HttpClient httpClient,
            string requestUri,
            MultipartFormDataContent formDataContent)
        {
            var response = await httpClient.PostAsync(requestUri, formDataContent);
            return await response.DeserializeHttpResponseAsync<T>();
        }

        public static async Task<T> PostAsync<T, R>(this HttpClient httpClient,
            string requestUri,
            R request, List<(string, string)> headers = default) where R : class
        {
            var jsonRequest = request.ToHttpContent();

            if (headers is not null && headers.Any())
            {
                foreach (var header in headers)
                {
                    jsonRequest.Headers.Add(header.Item1,header.Item2);
                }
                
            }

            var response = await retryPolicy.ExecuteAsync(async () =>
            {
                var result = await httpClient.PostAsync(requestUri, jsonRequest);
                result.EnsureSuccessStatusCode();
                return result;
            });

            if (!response.IsSuccessStatusCode)
            {
                
                var code = response.StatusCode;
            }

            return await response.DeserializeHttpResponseAsync<T>();
        }

        public static async Task<T> PutAsync<T>(this HttpClient httpClient,
            string requestUri,
            object request)
        {
            var response = await httpClient.PutAsync(requestUri, request);
            return await response.DeserializeHttpResponseAsync<T>();
        }

        public static async Task<HttpResponseMessage> PutAsync(this HttpClient httpClient,
            string requestUri,
            object request)
        {
            var requestSerialized = JsonConvert.SerializeObject(request);
            var jsonRequest = requestSerialized.ToHttpContent();
            return await httpClient.PutAsync(requestUri, jsonRequest);
        }

        public static async Task<T> GetAsync<T>(this HttpClient httpClient,
            string requestUri)
        {
           
            var response = await retryPolicy.ExecuteAsync(async () =>
            {
                var result = await httpClient.GetAsync(requestUri);
                result.EnsureSuccessStatusCode();
                return result;
            });

            if (!response.IsSuccessStatusCode)
            {
                var code = response.StatusCode;
            }

            return await response.DeserializeHttpResponseAsync<T>();
        }


        public static async Task<string> GetJsonStringAsync(this HttpClient httpClient,
            string requestUri)
        {
            var response = await httpClient.GetAsync(requestUri);
            return await response.Content.ReadAsStringAsync();
        }

        public static StringContent ToHttpContent(this string jsonRequest)
        {
            return new StringContent(jsonRequest, Encoding.UTF8, "application/json");
        }

        // public static async Task<string> GetAccessToken(this HttpClient identityClient, string userName, string password = @"Passdr0W/")
        // {
        //     var disco = await identityClient.GetDiscoveryDocumentAsync();
        //
        //     if (disco.IsError) throw new Exception(disco.Error);
        //
        //     var tokenRequest = new PasswordTokenRequest()
        //     {
        //         Address = disco.TokenEndpoint,
        //         ClientId = "fhcclientnativeapp",
        //         ClientSecret = "secret",
        //         Scope = "fhcclientapi roles openid profile IdentityServerApi",
        //         UserName = userName,
        //         Password = password
        //     };
        //
        //     var tokenResult = await identityClient.RequestPasswordTokenAsync(tokenRequest);
        //
        //     Console.WriteLine(tokenResult.HttpStatusCode);
        //     Console.WriteLine(tokenResult.AccessToken);
        //     return tokenResult.AccessToken;
        // }

        // public static async Task<HttpClient> GetAuthenticatedHttpClient<T>(this WebApplicationFactory<T> clientfactory, HttpClient identityClient, string userEmail)
        //     where T : class
        // {
        //     var token = await identityClient.GetAccessToken(userEmail);
        //     var client = clientfactory.CreateClient();
        //     client.SetBearerToken(token);
        //     return client;
        // }

        // public static async Task<string> GetClientCredentialsToken(this HttpClient identityClient)
        // {
        //     var disco = await identityClient.GetDiscoveryDocumentAsync();
        //
        //     if (disco.IsError) throw new Exception(disco.Error);
        //
        //     var tokenRequest = new ClientCredentialsTokenRequest()
        //     {
        //         Address = disco.TokenEndpoint,
        //         ClientId = "lmsClientCredential",
        //         ClientSecret = "5$gC4lT4RO69",
        //         Scope = "IdentityServerApi lmsClientCredential ClientService/Subscriptions.update ClientService/ClientPrimaryAccount-PTCProgress.update"
        //     };
        //
        //     var tokenResult = identityClient.RequestClientCredentialsTokenAsync(tokenRequest).Result;
        //
        //     Console.WriteLine(tokenResult.HttpStatusCode);
        //     Console.WriteLine(tokenResult.AccessToken);
        //     return tokenResult.AccessToken;
        // }

        // public static async Task<HttpClient> GetAuthenticatedClientCredentialsHttpClient<T>(this WebApplicationFactory<T> clientfactory, HttpClient identityClient, string userEmail)
        //     where T : class
        // {
        //     var token = await identityClient.GetClientCredentialsToken();
        //     var client = clientfactory.CreateClient();
        //     client.SetBearerToken(token);
        //     return client;
        // }
        //
        //
        // public static async Task<HttpClient> GetAuthenticatedDevHttpClient<T>(this WebApplicationFactory<T> clientfactory, HttpClient identityClient, string userEmail)
        //     where T : class
        // {
        //     var token = await identityClient.GetAccessToken(userEmail);
        //
        //     var httpClient = new HttpClient();
        //
        //     httpClient.BaseAddress = new Uri("https://api.client.fhc-dev.net");
        //     httpClient.SetBearerToken(token);
        //     return httpClient;
        // }
    }
}