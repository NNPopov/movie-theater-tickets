using CinemaTicketBooking.Application.Exceptions;
using CinemaTicketBooking.Domain.Error;
using CinemaTicketBooking.Domain.Exceptions;
using CinemaTicketBooking.Domain.ShoppingCarts;
using CinemaTicketBooking.Domain.ShoppingCarts.Abstractions;
using CinemaTicketBooking.Domain.ShoppingCarts.Events;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CinemaTicketBooking.Application.UnitTests.ShoppingCarts;

public class ShoppingCartSpecification
{
    private IDataHasher dataHasher;

    public ShoppingCartSpecification()
    {
        dataHasher = Substitute.For<IDataHasher>();
    }

    [Fact]
    public void CreateShoppingCart_property_should_be_not_null()
    {
        // Arrange
        short maxNumberOfSeats = 10;

        // Act
        var shoppingCart = ShoppingCart.Create(maxNumberOfSeats, dataHasher);

        // Assert
        shoppingCart.Should().NotBeNull();
        shoppingCart.HashId.Should().NotBeNull();
        shoppingCart.MaxNumberOfSeats.Should().Be(maxNumberOfSeats);
        shoppingCart.Status.Should().Be(ShoppingCartStatus.InWork);
        shoppingCart.GetDomainEvents().Should().Contain(x => x is ShoppingCartCreatedDomainEvent);
    }


    [Fact]
    public void AddSeats_ShouldAddSeatToShoppingCart_WhenThereIsSpace()
    {
        var showTimeId = Guid.NewGuid();
        // Arrange
        var shoppingCart = ShoppingCart.Create(5, dataHasher);
        shoppingCart.SetShowTime(showTimeId);
        var seat = Substitute.For<SeatShoppingCart>((short)1, (short)1, (decimal)100.00, null);

        // Act
        shoppingCart.AddSeats(seat, showTimeId);

        // Assert
        shoppingCart.Seats.Should().Contain(seat);
        shoppingCart.Seats.Should().HaveCount(1);
        shoppingCart.GetDomainEvents().Should().Contain(x => x is SeatAddedToShoppingCartDomainEvent);
    }

    [Fact]
    public void AddSeats_should_be_exception_when_incorrect_showTimeId()
    {
        // Arrange
        var showTimeId = Guid.NewGuid();
        var shoppingCart = ShoppingCart.Create(1, dataHasher);
        shoppingCart.SetShowTime(showTimeId);
        var seat = Substitute.For<SeatShoppingCart>((short)1, (short)1, (decimal)100.00, null);


        // Act
        var result = () => shoppingCart.AddSeats(seat, Guid.NewGuid());

        // Assert
        result.Should().Throw<DomainValidationException>()
            .WithMessage("The Seat does not belong to the cinema hall being processed.");
    }


    [Fact]
    public void AddSeats_should_be_exception_when_Seat_has_already_been_added()
    {
        // Arrange
        var showTimeId = Guid.NewGuid();
        var shoppingCart = ShoppingCart.Create(1, dataHasher);
        shoppingCart.SetShowTime(showTimeId);
        var seat = Substitute.For<SeatShoppingCart>((short)1, (short)1, (decimal)100.00, null);
        shoppingCart.AddSeats(seat, showTimeId);

        // Act

        var result = () => shoppingCart.AddSeats(seat, showTimeId);

        // Assert
        result.Should().Throw<DomainValidationException>();
    }

    [Fact]
    public void AddSeats_should_be_exception_when_there_is_no_space()
    {
        // Arrange
        var showTimeId = Guid.NewGuid();
        var shoppingCart = ShoppingCart.Create(1, dataHasher);
        shoppingCart.SetShowTime(showTimeId);
        var seat = Substitute.For<SeatShoppingCart>((short)1, (short)1, (decimal)100.00, null);
        shoppingCart.AddSeats(seat, showTimeId);

        // Act
        var seat2 = Substitute.For<SeatShoppingCart>((short)1, (short)2, (decimal)100.00, null);
        var result = () => shoppingCart.AddSeats(seat2, showTimeId);

        // Assert
        result.Should().Throw<DomainValidationException>().WithMessage("Number of seats cannot be greater than 1.");
    }

    [Fact]
    public void SetShowTime_ShouldSetShowTime()
    {
        // Arrange
        var shoppingCart = ShoppingCart.Create(5, dataHasher);
        var showTimeId = Guid.NewGuid();

        // Act
        shoppingCart.SetShowTime(showTimeId);

        // Assert
        shoppingCart.MovieSessionId.Should().Be(showTimeId);
        shoppingCart.GetDomainEvents().Should().Contain(x => x is ShoppingCartCreatedDomainEvent);
    }


    [Fact]
    public void TryRemoveSeats_ShouldRemoveSeatFromShoppingCart_WhenSeatExists()
    {
        var showTimeId = Guid.NewGuid();
        // Arrange
        var shoppingCart = ShoppingCart.Create(5, dataHasher);
        shoppingCart.SetShowTime(showTimeId);
        var seat = NSubstitute.Substitute.For<SeatShoppingCart>((short)1, (short)1, (decimal)100.00, null);
        shoppingCart.AddSeats(seat, showTimeId);

        // Act
        var result = shoppingCart.TryRemoveSeats(seat.SeatRow, seat.SeatNumber);

        // Assert
        result.Should().Be(true);
        shoppingCart.Seats.Should().NotContain(seat);
        shoppingCart.GetDomainEvents().Should().Contain(x => x is SeatRemovedFromShoppingCartDomainEvent);
    }

    [Fact]
    public void SetShowTime_ShouldThrowArgumentException_WhenClientIdNotAssign()
    {
        // Arrange
        var shoppingCart = ShoppingCart.Create(5, dataHasher);
        shoppingCart.SetShowTime(Guid.NewGuid());
        shoppingCart.SeatsReserve();

        // Act
        Action act = () => shoppingCart.PurchaseComplete();

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AssignClientId_ShouldAssignClientId()
    {
        // Arrange
        var shoppingCart = ShoppingCart.Create(5, dataHasher);
        var clientId = Guid.NewGuid();

        // Act
        var result = shoppingCart.AssignClientId(clientId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        shoppingCart.ClientId.Should().Be(clientId);
        shoppingCart.GetDomainEvents().Should().Contain(x => x is ShoppingCartAssignedToClientDomainEvent);
    }

    // Slice 0003_assign_client_cart_result_http: AssignClientId now expresses the already-assigned
    // case as a returned ConflictError (no longer a thrown ConflictException) and appends the
    // domain event only on the success branch.
    [Fact]
    public void AssignClientId_ShouldReturnConflictError_When_CartAlreadyHasAnOwner()
    {
        // Arrange
        var shoppingCart = ShoppingCart.Create(5, dataHasher);
        shoppingCart.AssignClientId(Guid.NewGuid());

        // Act
        var result = shoppingCart.AssignClientId(Guid.NewGuid());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ConflictError>();
        result.Error.Code.Should().Be("ShoppingCart.ConflictException");
    }

    [Fact]
    public void AssignClientId_ShouldNotRaiseASecondEvent_When_CartAlreadyHasAnOwner()
    {
        // Arrange
        var shoppingCart = ShoppingCart.Create(5, dataHasher);
        shoppingCart.AssignClientId(Guid.NewGuid());

        // Act — the conflicting second assignment must not append another assigned-to-client event.
        shoppingCart.AssignClientId(Guid.NewGuid());

        // Assert
        shoppingCart.GetDomainEvents()
            .Count(x => x is ShoppingCartAssignedToClientDomainEvent)
            .Should().Be(1);
    }

    // Slice 0005_reserve_tickets_result_http: SeatsReserve() is retyped void -> Result. On a genuine
    // InWork -> SeatsReserved transition it succeeds and raises exactly one ShoppingCartReservedDomainEvent;
    // re-reserving an already-SeatsReserved cart is an idempotent success with no second event; reserving a
    // PurchaseCompleted cart returns a ConflictError (was a thrown ConflictException) and raises no event.
    [Fact]
    public void SeatsReserve_ShouldTransitionToSeatsReservedAndRaiseEvent_When_CartIsInWork()
    {
        // Arrange
        var shoppingCart = ShoppingCart.Create(5, dataHasher);
        shoppingCart.SetShowTime(Guid.NewGuid());

        // Act
        var result = shoppingCart.SeatsReserve();

        // Assert
        result.IsSuccess.Should().BeTrue();
        shoppingCart.Status.Should().Be(ShoppingCartStatus.SeatsReserved);
        shoppingCart.GetDomainEvents()
            .Count(x => x is ShoppingCartReservedDomainEvent)
            .Should().Be(1);
    }

    [Fact]
    public void SeatsReserve_ShouldSucceedWithoutASecondEvent_When_CartIsAlreadySeatsReserved()
    {
        // Arrange
        var shoppingCart = ShoppingCart.Create(5, dataHasher);
        shoppingCart.SetShowTime(Guid.NewGuid());
        shoppingCart.SeatsReserve();

        // Act — re-reserving an already-reserved cart is an idempotent success, no duplicate event.
        var result = shoppingCart.SeatsReserve();

        // Assert
        result.IsSuccess.Should().BeTrue();
        shoppingCart.Status.Should().Be(ShoppingCartStatus.SeatsReserved);
        shoppingCart.GetDomainEvents()
            .Count(x => x is ShoppingCartReservedDomainEvent)
            .Should().Be(1);
    }

    [Fact]
    public void SeatsReserve_ShouldReturnConflictErrorAndRaiseNoEvent_When_CartIsPurchaseCompleted()
    {
        // Arrange
        var shoppingCart = ShoppingCart.Create(5, dataHasher);
        shoppingCart.AssignClientId(Guid.NewGuid());
        shoppingCart.SeatsReserve();
        shoppingCart.PurchaseComplete();

        // Act
        var result = shoppingCart.SeatsReserve();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ConflictError>();
        result.Error.Code.Should().Be("ShoppingCart.ConflictException");
        shoppingCart.GetDomainEvents()
            .Count(x => x is ShoppingCartReservedDomainEvent)
            .Should().Be(1);
    }
}