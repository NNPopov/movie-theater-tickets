using CinemaTicketBooking.Domain.Exceptions;
using CinemaTicketBooking.Domain.ShoppingCarts;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CinemaTicketBooking.Application.UnitTests.ShoppingCarts;


public class ShoppingCartTest
{

    [Fact]
    public void CanCreateShoppingCart()
    {
        // Arrange
        short maxNumberOfSeats = 10;

        // Act
        var shoppingCart = ShoppingCart.Create(maxNumberOfSeats);

        // Assert
        shoppingCart.Should().NotBeNull();
        shoppingCart.MaxNumberOfSeats.Should().Be(maxNumberOfSeats);
        shoppingCart.Status.Should().Be(ShoppingCartStatus.InWork);
    }



    
    [Fact]
    public void AddSeats_ShouldAddSeatToShoppingCart_WhenThereIsSpace()
    {
        // Arrange
        var shoppingCart = ShoppingCart.Create(5);
        shoppingCart.SetShowTime(Guid.NewGuid());
        var seat = NSubstitute.Substitute.For<SeatShoppingCart>((short)1, (short)1);
    
        // Act
        shoppingCart.AddSeats(seat);
    
        // Assert
        shoppingCart.Seats.Should().Contain(seat);
        shoppingCart.Seats.Should().HaveCount(1);
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
        // Arrange
        var shoppingCart = ShoppingCart.Create(5);
        shoppingCart.SetShowTime(Guid.NewGuid());
        var seat = NSubstitute.Substitute.For<SeatShoppingCart>((short)1,(short)1);
        shoppingCart.AddSeats(seat);

        // Act
        var result = shoppingCart.TryRemoveSeats(seat);
    
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
        Action act = () =>shoppingCart.PurchaseComplete();

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