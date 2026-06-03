using System.Reflection;
using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Application.Abstractions.Repositories;
using CinemaTicketBooking.Application.ShoppingCarts.Command.SelectSeats;
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

// RED acceptance gate for slice 0004_select_seats_result_http: the converted SelectSeats use-case
// must RETURN a failing Result for each expected business outcome (cart missing => NotFoundError;
// seat status not Available => ConflictError; seat held by another cart => ConflictError) instead of
// throwing, propagate the domain service's Result, and short-circuit BEFORE SaveShoppingCart so a
// cart is never persisted holding a seat whose claim failed (the atomicity invariant). The happy
// path is status-preserving (200 / Result.Success) and is pinned as a regression guard.
//
// This gate is RED until the conversion in plan.md section 5 lands: today the handler throws
// ContentNotFoundException for the missing cart and MovieSessionSeatService.SelectSeat throws
// ConflictException for both seat conflicts, so no failing Result is ever returned.
public class SelectSeatCommandHandlerTests
{
    private readonly IShoppingCartSeatLifecycleManager _seatLifecycle =
        Substitute.For<IShoppingCartSeatLifecycleManager>();

    private readonly IActiveShoppingCartRepository _cartRepository =
        Substitute.For<IActiveShoppingCartRepository>();

    private readonly IDistributedLock _distributedLock = Substitute.For<IDistributedLock>();

    private readonly ILockHandler _lockHandler = Substitute.For<ILockHandler>();

    private readonly IMovieSessionSeatRepository _seatRepository =
        Substitute.For<IMovieSessionSeatRepository>();

    private readonly IMovieSessionsRepository _sessionRepository =
        Substitute.For<IMovieSessionsRepository>();

    private readonly IShoppingCartLifecycleManager _cartLifecycle =
        Substitute.For<IShoppingCartLifecycleManager>();

    private readonly ILogger _logger = Substitute.For<ILogger>();

    // Real hasher so the created cart has a non-empty HashId (the handler passes cart.HashId into
    // MovieSessionSeatService.SelectSeat, which Ensure.NotEmpty-guards it).
    private readonly IDataHasher _dataHasher = new DataHasher();

    private readonly Guid _sessionId = Guid.NewGuid();

    public SelectSeatCommandHandlerTests()
    {
        // The distributed lock is always acquired in these scenarios.
        _lockHandler.IsLocked.Returns(true);
        _distributedLock
            .TryAcquireAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(_lockHandler);

        // The movie session exists and its sales are not terminated, so CheckSeatSaleAvailability passes.
        _sessionRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(MovieSession.Create(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddDays(1), 100));

        // The Redis seat-lifecycle write succeeds on the happy path (not reached on the failing claims).
        _seatLifecycle
            .SetAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<short>(), Arg.Any<short>(),
                Arg.Any<DateTime>())
            .Returns(true);
    }

    private SelectSeatCommandHandler CreateHandler() =>
        new(_seatLifecycle,
            _cartRepository,
            _distributedLock,
            _logger,
            new MovieSessionSeatService(_seatRepository, _sessionRepository),
            _cartLifecycle);

    private SelectSeatCommand SelectSeatOneOne() =>
        new(MovieSessionId: _sessionId, SeatRow: 1, SeatNumber: 1, ShoppingCartId: Guid.NewGuid());

    [Fact]
    public async Task Handle_Should_ReturnSuccess_And_SaveTheCart_When_SeatIsAvailable()
    {
        // Arrange
        var cart = ShoppingCart.Create(5, _dataHasher);
        var command = SelectSeatOneOne() with { ShoppingCartId = cart.Id };
        _cartRepository.GetByIdAsync(cart.Id).Returns(cart);
        _seatRepository.GetByIdAsync(_sessionId, (short)1, (short)1, Arg.Any<CancellationToken>())
            .Returns(AvailableSeat());
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _cartRepository.Received(1).SaveAsync(Arg.Any<ShoppingCart>());
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFoundError_And_NotSave_When_CartDoesNotExist()
    {
        // Arrange
        var command = SelectSeatOneOne();
        _cartRepository.GetByIdAsync(command.ShoppingCartId).Returns((ShoppingCart)null!);
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<NotFoundError>();
        await _cartRepository.DidNotReceive().SaveAsync(Arg.Any<ShoppingCart>());
    }

    [Fact]
    public async Task Handle_Should_ReturnConflictError_And_NotSave_When_SeatStatusIsNotAvailable()
    {
        // Arrange
        var cart = ShoppingCart.Create(5, _dataHasher);
        var command = SelectSeatOneOne() with { ShoppingCartId = cart.Id };
        _cartRepository.GetByIdAsync(cart.Id).Returns(cart);
        _seatRepository.GetByIdAsync(_sessionId, (short)1, (short)1, Arg.Any<CancellationToken>())
            .Returns(SeatAlreadySelected());
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ConflictError>();
        await _cartRepository.DidNotReceive().SaveAsync(Arg.Any<ShoppingCart>());
    }

    [Fact]
    public async Task Handle_Should_ReturnConflictError_And_NotSave_When_SeatIsHeldByAnotherCart()
    {
        // Arrange
        var cart = ShoppingCart.Create(5, _dataHasher);
        var command = SelectSeatOneOne() with { ShoppingCartId = cart.Id };
        _cartRepository.GetByIdAsync(cart.Id).Returns(cart);
        _seatRepository.GetByIdAsync(_sessionId, (short)1, (short)1, Arg.Any<CancellationToken>())
            .Returns(SeatOwnedByAnotherCart());
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert — was a base Error via InvalidOperation before the slice; must be a ConflictError so it maps to 409, not 500.
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ConflictError>();
        await _cartRepository.DidNotReceive().SaveAsync(Arg.Any<ShoppingCart>());
    }

    private MovieSessionSeat AvailableSeat() =>
        MovieSessionSeat.Create(_sessionId, seatNumber: 1, seatRow: 1, price: 10m);

    private MovieSessionSeat SeatAlreadySelected()
    {
        var seat = AvailableSeat();
        // Drive the seat out of the Available status (it is now Selected by some other cart).
        seat.Select(Guid.NewGuid(), "other-cart-hash");
        return seat;
    }

    private MovieSessionSeat SeatOwnedByAnotherCart()
    {
        // Available + owned by a different, non-empty cart is a materialized state the aggregate's own
        // transitions never produce (Select also flips the status to Selected), so build it via the
        // private [JsonConstructor] used for deserialization.
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
            _sessionId, (short)1, (short)1, 10m, SeatStatus.Available, Guid.NewGuid(), "other-cart-hash"
        });
    }
}
