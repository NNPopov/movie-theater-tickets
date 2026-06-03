using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using CinemaTicketBooking.Domain.Error;
using CinemaTicketBooking.Domain.Exceptions;
using CinemaTicketBooking.Domain.Seats;
using CinemaTicketBooking.Domain.Seats.Events;
using FluentAssertions;
using Xunit;

namespace CinemaTicketBooking.Application.UnitTests.Seats;

public class MovieSessionSeatSpecification
{
    [Fact]
    public async Task MovieSessionSeat_Should_Be_Correct_After_Initialisation()
    {
        Guid movieSessionId = Guid.NewGuid();
        short seatNumber = 1;
        short seatRow = 1;
        decimal price = 20;

        var movieSessionSeat = MovieSessionSeat.Create(movieSessionId,
            seatNumber, seatRow, price);


        movieSessionSeat.Status.Should().Be(SeatStatus.Available);
        movieSessionSeat.MovieSessionId.Should().Be(movieSessionId);
        movieSessionSeat.ShoppingCartId.Should().Be(Guid.Empty);
        movieSessionSeat.Price.Should().Be(20);
    }

    [Fact]
    public async Task TestTrySelect_FromAvailableState()
    {
        Guid movieSessionId = Guid.NewGuid();
        short seatNumber = 1;
        short seatRow = 1;
        decimal price = 20;

        Guid shoppingCartId = Guid.NewGuid();


        var movieSessionSeat = MovieSessionSeat.Create(movieSessionId,
            seatNumber, seatRow, price);

        var result = movieSessionSeat.Select(shoppingCartId, ComputeMD5(shoppingCartId.ToString()));
        result.IsSuccess.Should().Be(true);

        movieSessionSeat.Status.Should().Be(SeatStatus.Selected);
        movieSessionSeat.MovieSessionId.Should().Be(movieSessionId);
        movieSessionSeat.ShoppingCartId.Should().Be(shoppingCartId);
        movieSessionSeat.Price.Should().Be(20);
    }

    static string ComputeMD5(string s)
    {
        StringBuilder sb = new StringBuilder();

        // Initialize a MD5 hash object
        using (MD5 md5 = MD5.Create())
        {
            // Compute the hash of the given string
            byte[] hashValue = md5.ComputeHash(Encoding.UTF8.GetBytes(s));

            // Convert the byte array to string format
            foreach (byte b in hashValue)
            {
                sb.Append($"{b:X2}");
            }
        }

        return sb.ToString();
    }

    [Fact]
    public async Task TestTryReturnToAvailable_FromSelectedState()
    {
        Guid movieSessionId = Guid.NewGuid();
        short seatNumber = 1;
        short seatRow = 1;
        decimal price = 20;

        Guid shoppingCartId = Guid.NewGuid();

        var movieSessionSeat =
            PrepareSelectedMovieSessionSeat(movieSessionId, seatNumber, seatRow, price, shoppingCartId);


        movieSessionSeat.ReturnToAvailable();

        movieSessionSeat.Status.Should().Be(SeatStatus.Available);
        movieSessionSeat.MovieSessionId.Should().Be(movieSessionId);
        movieSessionSeat.ShoppingCartId.Should().Be(Guid.Empty);
    }

    [Fact]
    public async Task TestTryReserve_FromSelectedState()
    {
        Guid movieSessionId = Guid.NewGuid();
        short seatNumber = 1;
        short seatRow = 1;
        decimal price = 20;

        Guid shoppingCartId = Guid.NewGuid();

        var movieSessionSeat =
            PrepareSelectedMovieSessionSeat(movieSessionId, seatNumber, seatRow, price, shoppingCartId);


        movieSessionSeat.Reserve(shoppingCartId);
        movieSessionSeat.Status.Should().Be(SeatStatus.Reserved);
        movieSessionSeat.MovieSessionId.Should().Be(movieSessionId);
        movieSessionSeat.ShoppingCartId.Should().Be(shoppingCartId);
    }


    [Fact]
    public async Task TestTryReturnToAvailable_FromReservedState()
    {
        Guid movieSessionId = Guid.NewGuid();
        short seatNumber = 1;
        short seatRow = 1;
        decimal price = 20;

        Guid shoppingCartId = Guid.NewGuid();

        var movieSessionSeat =
            PrepareSelectedMovieSessionSeat(movieSessionId, seatNumber, seatRow, price, shoppingCartId);

        movieSessionSeat.ReturnToAvailable();

        movieSessionSeat.Status.Should().Be(SeatStatus.Available);
        movieSessionSeat.MovieSessionId.Should().Be(movieSessionId);
        movieSessionSeat.ShoppingCartId.Should().Be(Guid.Empty);
    }


    [Fact]
    public async Task TestTrySel_FromReservedState()
    {
        Guid movieSessionId = Guid.NewGuid();
        short seatNumber = 1;
        short seatRow = 1;
        decimal price = 20;

        Guid shoppingCartId = Guid.NewGuid();

        var movieSessionSeat =
            PrepareReservedMovieSessionSeat(movieSessionId, seatNumber, seatRow, price, shoppingCartId);

        movieSessionSeat.Sell(shoppingCartId);

        movieSessionSeat.Status.Should().Be(SeatStatus.Sold);
        movieSessionSeat.MovieSessionId.Should().Be(movieSessionId);
        movieSessionSeat.ShoppingCartId.Should().Be(shoppingCartId);
        movieSessionSeat.Price.Should().Be(20);
    }

    private static MovieSessionSeat PrepareSelectedMovieSessionSeat(Guid movieSessionId, short seatNumber,
        short seatRow,
        decimal price, Guid shoppingCartId)
    {
        var movieSessionSeat = MovieSessionSeat.Create(movieSessionId,
            seatNumber, seatRow, price);

        movieSessionSeat.MovieSessionId.Should().Be(movieSessionId);
        movieSessionSeat.Price.Should().Be(price);


        movieSessionSeat.Select(shoppingCartId, ComputeMD5(shoppingCartId.ToString()));
        movieSessionSeat.ShoppingCartId.Should().Be(shoppingCartId);

        return movieSessionSeat;
    }

    private static MovieSessionSeat PrepareReservedMovieSessionSeat(Guid movieSessionId, short seatNumber,
        short seatRow,
        decimal price, Guid shoppingCartId)
    {
        var movieSessionSeat =
            PrepareSelectedMovieSessionSeat(movieSessionId, seatNumber, seatRow, price, shoppingCartId);

        movieSessionSeat.Reserve(shoppingCartId);
        movieSessionSeat.Status.Should().Be(SeatStatus.Reserved);

        return movieSessionSeat;
    }

    // Slice 0004_select_seats_result_http: Select expresses both seat conflicts as a returned
    // ConflictError (the "another shopping cart" case changed from a base Error via InvalidOperation
    // to a ConflictError so it maps to 409, not 500) and appends the status-updated domain event only
    // on the success branch.

    [Fact]
    public void Select_Should_ReturnConflictError_And_RaiseNoEvent_When_StatusIsNotAvailable()
    {
        // Arrange — the seat is already Selected (status is not Available).
        var seat = SeatInState(SeatStatus.Selected, Guid.NewGuid());

        // Act
        var result = seat.Select(Guid.NewGuid(), "hash");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ConflictError>();
        seat.GetDomainEvents().Should().NotContain(x => x is MovieSessionSeatStatusUpdatedDomainEvent);
    }

    [Fact]
    public void Select_Should_ReturnConflictError_And_RaiseNoEvent_When_HeldByAnotherShoppingCart()
    {
        // Arrange — Available, but already owned by a different, non-empty cart.
        var seat = SeatInState(SeatStatus.Available, Guid.NewGuid());

        // Act
        var result = seat.Select(Guid.NewGuid(), "hash");

        // Assert — was a base Error via InvalidOperation before this slice; must be a ConflictError now.
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ConflictError>();
        seat.GetDomainEvents().Should().NotContain(x => x is MovieSessionSeatStatusUpdatedDomainEvent);
    }

    [Fact]
    public void Select_Should_RaiseStatusUpdatedEvent_When_SeatIsAvailableAndUnclaimed()
    {
        // Arrange — a freshly created, Available, unclaimed seat (no prior domain events).
        var seat = MovieSessionSeat.Create(Guid.NewGuid(), seatNumber: 1, seatRow: 1, price: 20);

        // Act
        var result = seat.Select(Guid.NewGuid(), "hash");

        // Assert
        result.IsSuccess.Should().BeTrue();
        seat.Status.Should().Be(SeatStatus.Selected);
        seat.GetDomainEvents().Should().ContainSingle(x => x is MovieSessionSeatStatusUpdatedDomainEvent);
    }

    // Build a materialized seat state via the private [JsonConstructor] — the aggregate's own
    // transitions never leave an Available seat owned by another cart (Select also flips the status),
    // so this state can only be constructed as if deserialized from storage.
    private static MovieSessionSeat SeatInState(SeatStatus status, Guid shoppingCartId)
    {
        var ctor = typeof(MovieSessionSeat).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            new[]
            {
                typeof(Guid), typeof(short), typeof(short), typeof(decimal),
                typeof(SeatStatus), typeof(Guid), typeof(string)
            },
            modifiers: null);

        return (MovieSessionSeat)ctor!.Invoke(new object[]
        {
            Guid.NewGuid(), (short)1, (short)1, 20m, status, shoppingCartId, "owner-hash"
        });
    }
}