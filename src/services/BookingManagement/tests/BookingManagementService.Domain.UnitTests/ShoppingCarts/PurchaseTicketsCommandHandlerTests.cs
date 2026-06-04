using CinemaTicketBooking.Application.Abstractions.Repositories;
using CinemaTicketBooking.Application.ShoppingCarts.Command.PurchaseSeats;
using CinemaTicketBooking.Domain.Error;
using CinemaTicketBooking.Domain.MovieSessions;
using CinemaTicketBooking.Domain.MovieSessions.Abstractions;
using CinemaTicketBooking.Domain.Seats;
using CinemaTicketBooking.Domain.Seats.Abstractions;
using CinemaTicketBooking.Domain.Services;
using CinemaTicketBooking.Domain.ShoppingCarts;
using CinemaTicketBooking.Domain.ShoppingCarts.Abstractions;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CinemaTicketBooking.Application.UnitTests.ShoppingCarts;

// RED acceptance gate for slice 0006_purchase_tickets_result_http: the converted PurchaseTickets
// use-case must RETURN the right Result for each outcome instead of completing unconditionally —
// cart missing => NotFoundError; movie session missing => NotFoundError; sales terminated =>
// ConflictError; a seat held by another cart => ConflictError (the MovieSessionSeat.Sell retype, NOT
// the base InvalidOperation that maps to 500); the seats sellable by this cart and the cart
// SeatsReserved => Result.Success(). On EVERY failure the handler must short-circuit BEFORE SaveAsync
// / the cart-lifecycle DeleteAsync / the per-seat DeleteAsync, so a cart is never persisted as
// PurchaseCompleted when the completion was not legal (the atomicity invariant the thrown path
// provided implicitly).
//
// This gate is RED until the conversion in plan.md section 5 lands: today MovieSessionSeat.Sell's
// "another shopping cart" case returns a base InvalidOperation Error (not a ConflictError), so the
// seat-held-by-another-cart scenario fails its BeOfType<ConflictError>() assertion; and the handler
// calls the void PurchaseComplete() rather than consuming a Result. The scenarios pass once the Sell
// retype, the PurchaseComplete() void -> Result conversion, and the handler short-circuit land.
public class PurchaseTicketsCommandHandlerTests
{
    private readonly IShoppingCartSeatLifecycleManager _seatLifecycle =
        Substitute.For<IShoppingCartSeatLifecycleManager>();

    private readonly IActiveShoppingCartRepository _cartRepository =
        Substitute.For<IActiveShoppingCartRepository>();

    private readonly IMovieSessionSeatRepository _seatRepository =
        Substitute.For<IMovieSessionSeatRepository>();

    private readonly IMovieSessionsRepository _sessionRepository =
        Substitute.For<IMovieSessionsRepository>();

    private readonly IShoppingCartLifecycleManager _cartLifecycle =
        Substitute.For<IShoppingCartLifecycleManager>();

    private readonly IDataHasher _dataHasher = Substitute.For<IDataHasher>();

    private readonly Guid _sessionId = Guid.NewGuid();

    public PurchaseTicketsCommandHandlerTests()
    {
        // By default the movie session exists and its sales are not terminated, so the shared
        // CheckSeatSaleAvailability passes; the session-missing / terminated scenarios re-stub this.
        _sessionRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(NonTerminatedSession());
    }

    private PurchaseTicketsCommandHandler CreateHandler() =>
        new(_seatLifecycle,
            _seatRepository,
            _cartRepository,
            new MovieSessionSeatService(_seatRepository, _sessionRepository),
            _cartLifecycle);

    [Fact]
    public async Task Handle_Should_ReturnSuccess_And_Persist_When_SeatsAreSellable_And_CartReserved()
    {
        // Arrange
        var cart = ReservedCartWithOneSeat();
        var command = new PurchaseTicketsCommand(cart.Id);
        _cartRepository.GetByIdAsync(cart.Id).Returns(cart);
        _seatRepository.GetByIdAsync(_sessionId, (short)1, (short)1, Arg.Any<CancellationToken>())
            .Returns(SeatOwnedBy(cart.Id));
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _cartRepository.Received(1).SaveAsync(Arg.Any<ShoppingCart>());
        await _cartLifecycle.Received(1).DeleteAsync(cart.Id);
        await _seatLifecycle.Received(1).DeleteAsync(_sessionId, (short)1, (short)1);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFoundError_And_NotPersist_When_CartDoesNotExist()
    {
        // Arrange
        var command = new PurchaseTicketsCommand(Guid.NewGuid());
        _cartRepository.GetByIdAsync(command.ShoppingCartId).Returns((ShoppingCart)null!);
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<NotFoundError>();
        await _cartRepository.DidNotReceive().SaveAsync(Arg.Any<ShoppingCart>());
        await _cartLifecycle.DidNotReceive().DeleteAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFoundError_And_NotPersist_When_MovieSessionDoesNotExist()
    {
        // Arrange
        var cart = ReservedCartWithOneSeat();
        var command = new PurchaseTicketsCommand(cart.Id);
        _cartRepository.GetByIdAsync(cart.Id).Returns(cart);
        _sessionRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((MovieSession)null!);
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<NotFoundError>();
        await _cartRepository.DidNotReceive().SaveAsync(Arg.Any<ShoppingCart>());
        await _cartLifecycle.DidNotReceive().DeleteAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task Handle_Should_ReturnConflictError_And_NotPersist_When_SalesAreTerminated()
    {
        // Arrange
        var cart = ReservedCartWithOneSeat();
        var command = new PurchaseTicketsCommand(cart.Id);
        _cartRepository.GetByIdAsync(cart.Id).Returns(cart);
        _sessionRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(TerminatedSession());
        var handler = CreateHandler();

        // Act — was an interim 200 (serialized Result body); must now be a ConflictError => 409.
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ConflictError>();
        await _cartRepository.DidNotReceive().SaveAsync(Arg.Any<ShoppingCart>());
        await _cartLifecycle.DidNotReceive().DeleteAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task Handle_Should_ReturnConflictError_And_NotPersist_When_ASeatIsHeldByAnotherCart()
    {
        // Arrange
        var cart = ReservedCartWithOneSeat();
        var command = new PurchaseTicketsCommand(cart.Id);
        _cartRepository.GetByIdAsync(cart.Id).Returns(cart);
        _seatRepository.GetByIdAsync(_sessionId, (short)1, (short)1, Arg.Any<CancellationToken>())
            .Returns(SeatOwnedBy(Guid.NewGuid())); // a DIFFERENT shopping cart
        var handler = CreateHandler();

        // Act — today MovieSessionSeat.Sell returns a base InvalidOperation Error (=> 500); the retype
        // must make seat contention on the purchase path a ConflictError (=> 409), like Select/Reserve.
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ConflictError>();
        await _cartRepository.DidNotReceive().SaveAsync(Arg.Any<ShoppingCart>());
        await _cartLifecycle.DidNotReceive().DeleteAsync(Arg.Any<Guid>());
    }

    private ShoppingCart ReservedCartWithOneSeat()
    {
        // A cart in status SeatsReserved with an assigned client and one held seat, so
        // PurchaseComplete() performs a genuine SeatsReserved -> PurchaseCompleted transition.
        var cart = ShoppingCart.Create(5, _dataHasher);
        cart.SetShowTime(_sessionId);
        cart.AssignClientId(Guid.NewGuid());
        cart.AddSeats(new SeatShoppingCart((short)1, (short)1, 10m), _sessionId);
        cart.SeatsReserve();
        return cart;
    }

    private MovieSessionSeat SeatOwnedBy(Guid ownerCartId)
    {
        // Create an Available seat and Select it into ownerCartId so its ShoppingCartId is set; a
        // subsequent Sell(cartId) succeeds only when ownerCartId == cartId, else hits the
        // "another shopping cart" branch.
        var seat = MovieSessionSeat.Create(_sessionId, seatNumber: 1, seatRow: 1, price: 10m);
        seat.Select(ownerCartId, "hash");
        return seat;
    }

    private static MovieSession NonTerminatedSession() =>
        MovieSession.Create(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddDays(1), ticketsForSale: 100);

    private static MovieSession TerminatedSession()
    {
        // SalesTerminated == SessionDate (future) >= now && TicketsForSale <= SoldTickets.
        var session = MovieSession.Create(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddDays(1), ticketsForSale: 1);
        session.SetSoldTickets(1);
        return session;
    }
}
