using System.Security.Cryptography;
using CinemaTicketBooking.Api.Controllers;
using CinemaTicketBooking.Api.Endpoints;
using CinemaTicketBooking.Application.MovieSessions.Queries;
using CinemaTicketBooking.Application.ShoppingCarts.Command.SelectSeats;
using NBomber.CSharp;
using NBomber.Http.CSharp;
using Xunit;
using Xunit.Abstractions;

namespace CinemaTicketBooking.Application.LoadTests;

public class LoadTestExample
{
    private readonly ITestOutputHelper _output;

    public LoadTestExample(ITestOutputHelper output)
    {
        this._output = output;
    }

    [Fact]
    public void SimpleHttpExample()
    {
        using var httpClient = new HttpClient();

        var scenario = Scenario.Create("http_scenario", async context =>
            {
                var baseUrl = "https://localhost:7629";
                //var baseUrl = "https://localhost:7443";

                //var baseUrl = "https://localhost:9443";

                var step1 = await Step.Run("step_1", context, async () =>
                {
                    ShoppingCartResponse cartResponse = default;


                    var randomOrderSeats = RandomNumberGenerator.GetInt32(2, 7);

                    _output.WriteLine($"randomOrderSeats {randomOrderSeats}");

                    var client = new HttpClient();
                    client.BaseAddress = new Uri(baseUrl);

                    var isCartCreated = false;
                    var requestId = Guid.NewGuid();

                    cartResponse = await TryCreateShoppingCart(isCartCreated, randomOrderSeats, client, requestId);


                    var movieSession = await client.GetAsync<MovieSessionDTO[]>("/api/showtimes");

                    var upperBound = movieSession.Length + 1;
                    var rngNum = RandomNumberGenerator.GetInt32(upperBound);

                    var showtime = movieSession[rngNum];

                    var reservedByMeSeats = 0;


                    var allReserverd = false;

                    while (!allReserverd)
                    {
                        try
                        {
                            var showtims2 =
                                await client.GetAsync<MovieSessionSeatDto[]>($"/api/showtimes/{showtime.Id}/seats");
                            if (showtims2.All(t => t.Blocked))
                            {
                                var placeNotExists = showtims2;
                            }


                            var upperBound2 = showtims2.Length;


                            List<SeatDto> seatDTOs = new();

                            var randomSeatNumber = RandomNumberGenerator.GetInt32(1, upperBound2);

                            var isFreePlase = false;
                            while (!isFreePlase)
                            {
                                var randomSeat = showtims2[randomSeatNumber];

                                if (!randomSeat.Blocked && !seatDTOs.Exists(t =>
                                        t.Row == randomSeat.Row && t.Number == randomSeat.SeatNumber))
                                {
                                    isFreePlase = true;

                                    var reserveSeatsRequest = new ReserveSeatsRequest(
                                        randomSeat.Row,
                                        randomSeat.SeatNumber,
                                        ShowtimeId: showtime.Id
                                    );

                                    var reserve = $"api/shopingcarts/{cartResponse.shoppingCartId}";

                                    var reservationResponse =
                                        await client.PostAsync<bool, ReserveSeatsRequest>(
                                            reserve,
                                            reserveSeatsRequest);
                                    if (reservationResponse == null)
                                    {
                                        Console.WriteLine(
                                            $@"ShoppingCartId: {reserveSeatsRequest.ShowtimeId}, 
SeatRow: {reserveSeatsRequest.Row} ,
SeatNumber: {reserveSeatsRequest.Number} ,
ShoppingCartId: {cartResponse.shoppingCartId}"
                                        );
                                    }

                                    if (reservationResponse)
                                    {
                                        reservedByMeSeats += 1;
                                    }
                                    //seatDTOs.Add(new SeatDTO(randomSeat.SeatRow, randomSeat.SeatNumber));
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                            reservedByMeSeats = 0;
                        }

                        if (reservedByMeSeats == randomOrderSeats)
                            allReserverd = true;
                    }

                    var buyTicketsUrl = $"api/shopingcarts/{cartResponse.shoppingCartId}:BuyTikets";

                    try
                    {
                        var buyTicketsResponse = await client.PostAsync(buyTicketsUrl, null);

                        var buyTicketResult =
                            await buyTicketsResponse.DeserializeHttpResponseAsync<bool>();


                        Console.WriteLine("------------------------------------------");
                        Console.WriteLine($@"ShoppingCartId: {buyTicketResult}");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        throw;
                    }

                    ;


                    var request =
                        Http.CreateRequest("GET", $"{baseUrl}/api/showtimes")
                            .WithHeader("Accept", "application/json");
                    //.WithBody(new StringContent("{ some JSON }"));

                    var response1 = await Http.Send(httpClient, request);


                    return response1;
                });

                // var step2 = await Step.Run("step_2", context, async () =>
                // {
                //     var request =
                //         Http.CreateRequest("GET", "https://nbomber.com")
                //             .WithHeader("Accept", "text/html")
                //             .WithBody(new StringContent("{ some JSON }"));
                //
                //     var response = await Http.Send(httpClient, request);
                //
                //     return response;
                // });

                return Response.Ok();
            })
            .WithoutWarmUp()
            .WithLoadSimulations(Simulation.Inject(rate: 20, interval: TimeSpan.FromMilliseconds(500),
                TimeSpan.FromSeconds(10)));

        var result = NBomberRunner
            .RegisterScenarios(scenario)
            .Run();

        var scnStats = result.GetScenarioStats("http_scenario");
        var step1Stats = scnStats.GetStepStats("step_1");
        //var step2Stats = scnStats.GetStepStats("step_2");
    }

    private async Task<ShoppingCartResponse?> TryCreateShoppingCart(bool isCartCreated, 
        int randomOrderSeats,
        HttpClient client,
        Guid requestId)
    {
        ShoppingCartResponse? cartResponse;
        var tryCreateShopping = 0;
       
            _output.WriteLine($"Try create shopping cart: {tryCreateShopping}");
            var cartRequest = new CreateShoppingCartRequest((short)randomOrderSeats);


            cartResponse =
                await client
                    .PostAsync<ShoppingCartResponse,
                        CreateShoppingCartRequest>("api/shopingcarts",
                        cartRequest, new List<(string, string)>
                        {
                            ("X-Idempotency-Key", requestId.ToString())
                        });


            if (cartResponse.shoppingCartId != default)
            {
                return cartResponse;
            }
        
        throw new Exception("Cart not created");
    }
}