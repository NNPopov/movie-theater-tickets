using CinemaTicketBooking.Application.Abstractions.Repositories;
using CinemaTicketBooking.Application.ShoppingCarts.Command.ReserveSeats;
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
using ILogger = Serilog.ILogger;

namespace CinemaTicketBooking.Application.UnitTests.ShoppingCarts;

// RED acceptance gate for slice 0005_reserve_tickets_result_http: the converted ReserveTickets
// use-case must RETURN a failing Result for each expected business outcome instead of throwing or
// swallowing — cart missing => NotFoundError; movie session missing => NotFoundError; sales
// terminated => ConflictError (was a bare Exception => 500); a seat not reservable => ConflictError
// (was the bare `throw new Exception("Couldn't Reserve")` bridge => 500); cart already purchased =>
// ConflictError (was a thrown ConflictException). On EVERY failure the handler must short-circuit
// BEFORE SaveAsync / the lifecycle side-effects, so a cart is never persisted as SeatsReserved when a
// seat could not be reserved (the atomicity invariant the thrown/bare-Exception path provided
// implicitly). The happy path is status-preserving (Result.Success, cart saved) and is pinned as a
// regression guard.
//
// This gate is RED until the conversion in plan.md section 5 lands: today the handler throws
// ContentNotFoundException for the missing cart, cart.SeatsReserve() throws ConflictException for an
// already-purchased cart, MovieSessionSeatService.CheckSeatSaleAvailability throws
// ContentNotFoundException / a bare Exception, and a failing ReserveSeats Result is re-thrown as
// `throw new Exception("Couldn't Reserve …")` — so no failing Result is ever returned.
public class ReserveTicketsCommandHandlerTests
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

    private readonly ILogger _logger = Substitute.For<ILogger>();

    private readonly IDataHasher _dataHasher = Substitute.For<IDataHasher>();

    private readonly Guid _sessionId = Guid.NewGuid();

    public ReserveTicketsCommandHandlerTests()
    {
        // By default the movie session exists and its sales are not terminated, so the shared
        // CheckSeatSaleAvailability passes; the session-missing / terminated scenarios re-stub this.
        _sessionRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(NonTerminatedSession());
    }

    private ReserveTicketsCommandHandler CreateHandler() =>
        new(_seatLifecycle,
            _cartRepository,
            new MovieSessionSeatService(_seatRepository, _sessionRepository),
            _cartLifecycle,
            _logger);

    [Fact]
    public async Task Handle_Should_ReturnSuccess_And_Persist_When_AllSeatsAreReservable()
    {
        // Arrange
        var cart = InWorkCartWithOneSeat();
        var command = new ReserveTicketsCommand(cart.Id);
        _cartRepository.GetByIdAsync(cart.Id).Returns(cart);
        _seatRepository.GetByIdAsync(_sessionId, (short)1, (short)1, Arg.Any<CancellationToken>())
            .Returns(ReservableSeat());
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _cartRepository.Received(1).SaveAsync(Arg.Any<ShoppingCart>());
        await _cartLifecycle.Received(1).SetAsync(cart.Id);
        await _seatLifecycle.Received(1).DeleteAsync(_sessionId, (short)1, (short)1);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFoundError_And_NotPersist_When_CartDoesNotExist()
    {
        // Arrange
        var command = new ReserveTicketsCommand(Guid.NewGuid());
        _cartRepository.GetByIdAsync(command.ShoppingCartId).Returns((ShoppingCart)null!);
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<NotFoundError>();
        await _cartRepository.DidNotReceive().SaveAsync(Arg.Any<ShoppingCart>());
        await _cartLifecycle.DidNotReceive().SetAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFoundError_And_NotPersist_When_MovieSessionDoesNotExist()
    {
        // Arrange
        var cart = InWorkCartWithOneSeat();
        var command = new ReserveTicketsCommand(cart.Id);
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
        await _cartLifecycle.DidNotReceive().SetAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task Handle_Should_ReturnConflictError_And_NotPersist_When_SalesAreTerminated()
    {
        // Arrange
        var cart = InWorkCartWithOneSeat();
        var command = new ReserveTicketsCommand(cart.Id);
        _cartRepository.GetByIdAsync(cart.Id).Returns(cart);
        _sessionRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(TerminatedSession());
        var handler = CreateHandler();

        // Act — was a bare `throw new Exception(...)` => 500 before the slice; must now be a ConflictError => 409.
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ConflictError>();
        await _cartRepository.DidNotReceive().SaveAsync(Arg.Any<ShoppingCart>());
        await _cartLifecycle.DidNotReceive().SetAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task Handle_Should_ReturnConflictError_And_NotPersist_When_ASeatIsNotReservable()
    {
        // Arrange
        var cart = InWorkCartWithOneSeat();
        var command = new ReserveTicketsCommand(cart.Id);
        _cartRepository.GetByIdAsync(cart.Id).Returns(cart);
        _seatRepository.GetByIdAsync(_sessionId, (short)1, (short)1, Arg.Any<CancellationToken>())
            .Returns(NotReservableSeat());
        var handler = CreateHandler();

        // Act — was the bare `throw new Exception("Couldn't Reserve …")` bridge => 500; the failing
        // Result from ReserveSeats must now be propagated unchanged as a ConflictError => 409.
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ConflictError>();
        await _cartRepository.DidNotReceive().SaveAsync(Arg.Any<ShoppingCart>());
        await _cartLifecycle.DidNotReceive().SetAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task Handle_Should_ReturnConflictError_And_NotPersist_When_CartAlreadyPurchased()
    {
        // Arrange
        var cart = PurchasedCart();
        var command = new ReserveTicketsCommand(cart.Id);
        _cartRepository.GetByIdAsync(cart.Id).Returns(cart);
        var handler = CreateHandler();

        // Act — was a thrown ConflictException (same 409); the converted SeatsReserve() must return a
        // ConflictError and short-circuit before any seat reservation or persistence.
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ConflictError>();
        await _cartRepository.DidNotReceive().SaveAsync(Arg.Any<ShoppingCart>());
        await _cartLifecycle.DidNotReceive().SetAsync(Arg.Any<Guid>());
    }

    private ShoppingCart InWorkCartWithOneSeat()
    {
        var cart = ShoppingCart.Create(5, _dataHasher);
        cart.SetShowTime(_sessionId);
        cart.AddSeats(new SeatShoppingCart((short)1, (short)1, 10m), _sessionId);
        return cart;
    }

    private ShoppingCart PurchasedCart()
    {
        // Drive the cart to PurchaseCompleted through its legal transitions (no seats are needed —
        // SeatsReserve must short-circuit before the seat service on a purchased cart).
        var cart = ShoppingCart.Create(5, _dataHasher);
        cart.AssignClientId(Guid.NewGuid());
        cart.SeatsReserve();
        cart.PurchaseComplete();
        return cart;
    }

    private MovieSessionSeat ReservableSeat() =>
        MovieSessionSeat.Create(_sessionId, seatNumber: 1, seatRow: 1, price: 10m); // Available

    private MovieSessionSeat NotReservableSeat()
    {
        var seat = ReservableSeat();
        // Drive it past Available/Selected so a subsequent Reserve returns a ConflictError.
        seat.Reserve(Guid.NewGuid());
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
