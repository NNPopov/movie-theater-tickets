using System.Configuration;
using System.Security.Cryptography;
using CinemaTicketBooking.Api.Controllers;
using CinemaTicketBooking.Api.Endpoints;
using CinemaTicketBooking.Application.MovieSessions.Queries;
using CinemaTicketBooking.Application.ShoppingCarts.Command.SelectSeats;
using Xunit;
using Xunit.Abstractions;

namespace CinemaTicketBooking.Application.LoadTests;

public class EndToEndTests
{
    //var baseUrl = "https://localhost:7629";
    private string baseUrl = "https://localhost:7443";

    //var baseUrl = "https://localhost:9443";


    private string ShoppingCartBaseRoute = @"api/shoppingcarts";
    private string MovieSessionBaseRoute = @"api/moviesessions";
    private readonly ITestOutputHelper _output;

    public EndToEndTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async void SimpleHttpExample()
    {
        var test = new EndToEndShopingCartTest(_output);

        List<Task?> testExecution = new List<Task?>();
        for (int i = 0; i < 450; i++)
        {
            testExecution.Add(test.EndToEndShoppingCartExecution());
            await TestRandom.RandomDelay();
        }


        await Task.WhenAll(testExecution);
    }
}

public class EndToEndShopingCartTest
{
    private string baseUrl = "https://localhost:7443";

    //var baseUrl = "https://localhost:9443";


    private string ShoppingCartBaseRoute = @"api/shoppingcarts";
    private string MovieSessionBaseRoute = @"api/moviesessions";
    private readonly ITestOutputHelper _output;

    public EndToEndShopingCartTest(ITestOutputHelper output)
    {
        _output = output;
    }

    public async Task EndToEndShoppingCartExecution()
    {
        using var httpClient = new HttpClient();


        ShoppingCartResponse cartResponse = default;


        var randomOrderSeats = RandomNumberGenerator.GetInt32(1, 5);
        var client = new HttpClient();
        client.BaseAddress = new Uri(baseUrl);

        

        var createdCard = false;
        while (!createdCard)
        {
            var requestId = Guid.NewGuid();

            var cartRequest = new CreateShoppingCartRequest((short)randomOrderSeats);
            try
            {
                cartResponse =
                    await client
                        .PostAsync<ShoppingCartResponse,
                            CreateShoppingCartRequest>(ShoppingCartBaseRoute,
                            cartRequest, new List<(string, string)>
                            {
                                ("X-Idempotency-Key", requestId.ToString())
                            });

                createdCard = true;
            }
            catch (Exception e)
            {
                _output.WriteLine(
                    $@"Create shopping cart with {cartRequest.MaxNumberOfSeats} return {e.Message}");
               // return;
            }
        }
       

        
        
      
        if (cartResponse.shoppingCartId == default)
        {
            _output.WriteLine("ShoppingCart was not created");
            return;
        }
        _output.WriteLine($"{cartResponse.shoppingCartId} shopping cart created");

        await TestRandom.RandomDelay();

        var movieSession = await client.GetAsync<MovieSessionDTO[]>(MovieSessionBaseRoute);

        var upperBound = movieSession.Length;
        var rngNum = RandomNumberGenerator.GetInt32(0, upperBound);

        var showtime = movieSession[rngNum];

        var reservedByMeSeats = 0;

        await TestRandom.RandomDelay();

        var allReserverd = false;

        while (!allReserverd)
        {
            try
            {
                var showtims2 =
                    await client.GetAsync<MovieSessionSeatDto[]>($"{MovieSessionBaseRoute}/{showtime.Id}/seats");
                

                var showtims3 = showtims2.Where(t=>!t.Blocked).ToArray();
                
                _output.WriteLine(
                    $@"{cartResponse.shoppingCartId} Blocked cards: {showtims3.Length}");
                if (showtims3.Length<=1)
                {
                    _output.WriteLine(
                        $@"{cartResponse.shoppingCartId}, AllSeats blocked !!!!!!");
                    var placeNotExists = showtims3;
                    return;
                }
                
                var upperBound2 = showtims3.Length;


                List<SeatDto> seatDTOs = new();

                var randomSeatNumber = RandomNumberGenerator.GetInt32(0, upperBound2);

                var isFreePlase = false;
                while (!isFreePlase)
                {
                    await TestRandom.RandomDelay();

                    _output.WriteLine(
                        $@"{cartResponse.shoppingCartId} Try select places");

                    var randomSeat = showtims3[randomSeatNumber];

                    if (!randomSeat.Blocked && !seatDTOs.Exists(t =>
                            t.Row == randomSeat.Row && t.Number == randomSeat.SeatNumber))
                    {
                        isFreePlase = true;

                        var reserveSeatsRequest = new ReserveSeatsRequest(
                            randomSeat.Row,
                            randomSeat.SeatNumber,
                            ShowtimeId: showtime.Id
                        );

                        var reserve = $"{ShoppingCartBaseRoute}/{cartResponse.shoppingCartId}/seats/select";

                        var reservationResponse =
                            await client.PostAsync<bool, ReserveSeatsRequest>(
                                reserve,
                                reserveSeatsRequest);
                        if (reservationResponse == null)
                        {
                            _output.WriteLine(
                                $@"{cartResponse.shoppingCartId} ShowtimeId: {reserveSeatsRequest.ShowtimeId}, 
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
                _output.WriteLine($"{cartResponse.shoppingCartId} {e.Message}");
                reservedByMeSeats = 0;
            }

            if (reservedByMeSeats == randomOrderSeats)
                allReserverd = true;
        }

        await TestRandom.RandomDelay();

        var reserveTicketsUrl = $"{ShoppingCartBaseRoute}/{cartResponse.shoppingCartId}/reservations";

        try
        {
            var reserveTicketsResponse = await client.PostAsync(reserveTicketsUrl, null);

            var reserveTicketResult =
                await reserveTicketsResponse.DeserializeHttpResponseAsync<bool>();

            _output.WriteLine($"{cartResponse.shoppingCartId} ReserveTicketResult ShoppingCartId: {reserveTicketResult}");
        }
        catch (Exception e)
        {
            _output.WriteLine($"{cartResponse.shoppingCartId} Reserve error {e.Message}");
            return;
        }

        await TestRandom.RandomDelay();

        var buyTicketsUrl = $"{ShoppingCartBaseRoute}/{cartResponse.shoppingCartId}/purchase";

        try
        {
            var buyTicketsResponse = await client.PostAsync(buyTicketsUrl, null);

            Console.WriteLine(buyTicketsResponse.StatusCode);

            var buyTicketResult =
                await buyTicketsResponse.DeserializeHttpResponseAsync<bool>();

            _output.WriteLine($@"{cartResponse.shoppingCartId} BuyTicketResult ShoppingCartId: {buyTicketResult}");
        }
        catch (Exception e)
        {
            _output.WriteLine($"{cartResponse.shoppingCartId} Reserve error {e.Message}");
            return;
        }
    }
    
}

public static class TestRandom
{
    public static async Task RandomDelay()
    {
        var randomDelaySeconds = RandomNumberGenerator.GetInt32(1, 2);
        await Task.Delay(TimeSpan.FromSeconds(randomDelaySeconds));
    }
}