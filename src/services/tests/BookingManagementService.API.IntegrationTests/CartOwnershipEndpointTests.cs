using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CinemaTicketBooking.Api.IntegrationTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace CinemaTicketBooking.Api.IntegrationTests;

// End-to-end gate for slice 0009 over HTTP through the full pipeline (tests.md scenarios 1-5).
// Each test seeds its own cart with a fresh id, so the shared in-memory store needs no reset.
public class CartOwnershipEndpointTests : IClassFixture<BookingApiFactory>
{
    // Fresh ids per test instance (xUnit constructs the class once per test method), so the
    // shared in-memory client->cart mapping never collides between scenarios that both assign.
    private readonly Guid UserA = Guid.NewGuid();
    private readonly Guid UserB = Guid.NewGuid();

    private const string ForbiddenType = "https://tools.ietf.org/html/rfc7231#section-6.5.3";

    private readonly BookingApiFactory _factory;

    public CartOwnershipEndpointTests(BookingApiFactory factory) => _factory = factory;

    [Fact] // Scenario 1: owner reads own assigned cart => 200 (pass-through). Covers F7, F14, F15.
    public async Task Owner_Reading_Own_Assigned_Cart_Returns_200()
    {
        var client = _factory.CreateClient();
        var cartId = await CreateAssignedCartAsync(client, UserA);

        using var response = await SendAsync(client, HttpMethod.Get, $"/api/shoppingcarts/{cartId}", UserA);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var cart = await ReadJsonAsync(response);
        cart.GetProperty("id").GetGuid().Should().Be(cartId);
        cart.GetProperty("clientId").GetGuid().Should().Be(UserA);
    }

    [Fact] // Scenario 2: stranger reads someone else's assigned cart => 403. Covers F8, F9, F14.
    public async Task Stranger_Reading_Assigned_Cart_Returns_403_ProblemDetails()
    {
        var client = _factory.CreateClient();
        var cartId = await CreateAssignedCartAsync(client, UserA);

        using var response = await SendAsync(client, HttpMethod.Get, $"/api/shoppingcarts/{cartId}", UserB);

        await AssertForbiddenProblemDetailsAsync(response);
    }

    [Fact] // Scenario 3: stranger purchases someone else's assigned cart => 403, no side effect. Covers F8, F13.
    public async Task Stranger_Purchasing_Assigned_Cart_Returns_403_And_Leaves_Cart_Unchanged()
    {
        var client = _factory.CreateClient();
        var cartId = await CreateAssignedCartAsync(client, UserA);

        using var purchase = await SendAsync(client, HttpMethod.Post, $"/api/shoppingcarts/{cartId}/purchase", UserB);
        await AssertForbiddenProblemDetailsAsync(purchase);

        // The behaviour blocked the handler before any mutation: the cart still exists, still owned by A.
        using var reread = await SendAsync(client, HttpMethod.Get, $"/api/shoppingcarts/{cartId}", UserA);
        reread.StatusCode.Should().Be(HttpStatusCode.OK);
        var cart = await ReadJsonAsync(reread);
        cart.GetProperty("clientId").GetGuid().Should().Be(UserA);
    }

    [Fact] // Scenario 4: not-found cart => 404, not 403 (existence not leaked). Covers F5, F14.
    public async Task Missing_Cart_Returns_404_Not_403()
    {
        var client = _factory.CreateClient();
        var missingCartId = Guid.NewGuid();

        using var response = await SendAsync(client, HttpMethod.Get, $"/api/shoppingcarts/{missingCartId}", UserB);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact] // Scenario 5: guest on an anonymous cart => 200 (capability preserved). Covers F6, F14, F15.
    public async Task Guest_Reading_Anonymous_Cart_Returns_200()
    {
        var client = _factory.CreateClient();
        var cartId = await CreateAnonymousCartAsync(client);

        using var response = await SendAsync(client, HttpMethod.Get, $"/api/shoppingcarts/{cartId}", testUser: null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var cart = await ReadJsonAsync(response);
        cart.GetProperty("id").GetGuid().Should().Be(cartId);
        cart.GetProperty("clientId").GetGuid().Should().Be(Guid.Empty);
    }

    // --- seeding & helpers (black-box, over HTTP) ---

    private static async Task<Guid> CreateAnonymousCartAsync(HttpClient client)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/shoppingcarts")
        {
            Content = JsonContent.Create(new { maxNumberOfSeats = 4 }),
        };
        request.Headers.Add("X-Idempotency-Key", Guid.NewGuid().ToString());

        using var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await ReadJsonAsync(response);
        return body.GetProperty("shoppingCartId").GetGuid();
    }

    private static async Task<Guid> CreateAssignedCartAsync(HttpClient client, Guid owner)
    {
        var cartId = await CreateAnonymousCartAsync(client);

        using var assign = await SendAsync(client, HttpMethod.Put, $"/api/shoppingcarts/{cartId}/assignclient", owner);
        assign.StatusCode.Should().Be(HttpStatusCode.OK);

        return cartId;
    }

    private static Task<HttpResponseMessage> SendAsync(HttpClient client, HttpMethod method, string uri, Guid? testUser)
    {
        var request = new HttpRequestMessage(method, uri);
        if (testUser is not null)
            request.Headers.Add(TestAuthHandler.UserHeader, testUser.Value.ToString());

        return client.SendAsync(request);
    }

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        return document.RootElement.Clone();
    }

    private static async Task AssertForbiddenProblemDetailsAsync(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var problem = await ReadJsonAsync(response);
        problem.GetProperty("status").GetInt32().Should().Be(403);
        problem.GetProperty("title").GetString().Should().Be("Forbidden");
        problem.GetProperty("type").GetString().Should().Be(ForbiddenType);
        // No ShoppingCart leaked: a ProblemDetails has no cart-shaped members.
        problem.TryGetProperty("clientId", out _).Should().BeFalse();
        problem.TryGetProperty("seats", out _).Should().BeFalse();
    }
}
