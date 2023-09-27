using CinemaTicketBooking.Domain.ShoppingCarts;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CinemaTicketBooking.Application.UnitTests.ShoppingCarts;


public class ShoppingCartTest
{
    
    
        
    [Fact]
    public void CanTimeProviderCart()
    {
       var timeProvider=  Substitute.For<TimeProvider>();
        
       timeProvider.GetUtcNow().Returns(new DateTimeOffset(2008, 5, 1, 8, 6, 32,
           new TimeSpan(1, 0, 0)));

        timeProvider.GetUtcNow().Should().Be( new DateTimeOffset(2008, 5, 1, 8, 6, 32,
            new TimeSpan(1, 0, 0)));
    }
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
    public void CanSetShowTime()
    {
        // Arrange
        short maxNumberOfSeats = 10;
        var shoppingCart = ShoppingCart.Create(maxNumberOfSeats);
        Guid showTimeId = Guid.NewGuid();

        // Act
        shoppingCart.SetShowTime(showTimeId);

        // Assert
        shoppingCart.MovieSessionId.Should().Be(showTimeId);
    }

    [Fact]
    public void CanAddSeats()
    {
        // Arrange
        short maxNumberOfSeats = 10;
        var shoppingCart = ShoppingCart.Create(maxNumberOfSeats);
        
        var seat = Substitute.For<SeatShoppingCart>((short)1,(short)1);

        // Act
        shoppingCart.SetShowTime(Guid.NewGuid()); // set showtime before adding seats
        shoppingCart.AddSeats(seat);

        // Assert
        shoppingCart.Seats.Should().HaveCount(1);
    }
}