using CinemaTicketBooking.Application.Exceptions;
using CinemaTicketBooking.Domain.Exceptions;
using CinemaTicketBooking.Domain.ShoppingCarts;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CinemaTicketBooking.Application.UnitTests.ShoppingCarts;

public class ShoppingCartSpecification
{
    [Fact]
    public void CreateShoppingCart_property_should_be_not_null()
    {
        // Arrange
        short maxNumberOfSeats = 10;

        // Act
        var shoppingCart = ShoppingCart.Create(maxNumberOfSeats);

        // Assert
        shoppingCart.Should().NotBeNull();
        shoppingCart.HashId.Should().NotBeNull();
        shoppingCart.MaxNumberOfSeats.Should().Be(maxNumberOfSeats);
        shoppingCart.Status.Should().Be(ShoppingCartStatus.InWork);
    }


    [Fact]
    public void AddSeats_ShouldAddSeatToShoppingCart_WhenThereIsSpace()
    {
        var showTimeId = Guid.NewGuid();
        // Arrange
        var shoppingCart = ShoppingCart.Create(5);
        shoppingCart.SetShowTime(showTimeId);
        var seat = Substitute.For<SeatShoppingCart>((short)1, (short)1, (decimal)100.00, null);

        // Act
        shoppingCart.AddSeats(seat, showTimeId);

        // Assert
        shoppingCart.Seats.Should().Contain(seat);
        shoppingCart.Seats.Should().HaveCount(1);
    }
    
    [Fact]
    public void AddSeats_should_be_exception_when_incorrect_showTimeId()
    {
        // Arrange
        var showTimeId = Guid.NewGuid();
        var shoppingCart = ShoppingCart.Create(1);
        shoppingCart.SetShowTime(showTimeId);
        var seat = Substitute.For<SeatShoppingCart>((short)1, (short)1, (decimal)100.00, null);
       

        // Act
        var result = () => shoppingCart.AddSeats(seat, Guid.NewGuid());

        // Assert
        result.Should().Throw<DomainValidationException>().WithMessage("The Seat does not belong to the cinema hall being processed.");
    }

    
        
        [Fact]
    public void AddSeats_should_be_exception_when_Seat_has_already_been_added()
    {
        // Arrange
        var showTimeId = Guid.NewGuid();
        var shoppingCart = ShoppingCart.Create(1);
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
        var shoppingCart = ShoppingCart.Create(1);
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
        var shoppingCart = ShoppingCart.Create(5);
        var showTimeId = Guid.NewGuid();

        // Act
        shoppingCart.SetShowTime(showTimeId);

        // Assert
        shoppingCart.MovieSessionId.Should().Be(showTimeId);
    }


    [Fact]
    public void TryRemoveSeats_ShouldRemoveSeatFromShoppingCart_WhenSeatExists()
    {
        var showTimeId = Guid.NewGuid();
        // Arrange
        var shoppingCart = ShoppingCart.Create(5);
        shoppingCart.SetShowTime(showTimeId);
        var seat = NSubstitute.Substitute.For<SeatShoppingCart>((short)1, (short)1, (decimal)100.00, null);
        shoppingCart.AddSeats(seat, showTimeId);

        // Act
        var result = shoppingCart.TryRemoveSeats(seat.SeatRow, seat.SeatNumber);

        // Assert
        result.Should().Be(true);
        shoppingCart.Seats.Should().NotContain(seat);
    }

    [Fact]
    public void SetShowTime_ShouldThrowArgumentException_WhenClientIdNotAssign()
    {
        // Arrange
        var shoppingCart = ShoppingCart.Create(5);
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
        var shoppingCart = ShoppingCart.Create(5);
        var clientId = Guid.NewGuid();

        // Act
        shoppingCart.AssignClientId(clientId);

        // Assert
        shoppingCart.ClientId.Should().Be(clientId);
    }
}