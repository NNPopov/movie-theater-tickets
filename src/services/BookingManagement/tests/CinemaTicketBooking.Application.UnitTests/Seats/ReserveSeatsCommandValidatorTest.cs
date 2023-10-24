using System.Security.Cryptography;
using System.Text;
using CinemaTicketBooking.Domain.Seats;
using FluentAssertions;
using Xunit;

namespace CinemaTicketBooking.Application.UnitTests.Seats;

public class MovieSessionSeatTest
{
    [Fact]
    public async Task MovieSessionSeat_Should_Be_Correct_After_Initialisation()
    {
        Guid movieSessionId = Guid.NewGuid();
        short seatNumber = 1;
        short seatRow = 1;
        decimal price = 20;
            
        var movieSessionSeat =  MovieSessionSeat.Create(movieSessionId,
            seatNumber,seatRow, price);
        
    
        movieSessionSeat.Status.Should().Be( SeatStatus.Available);
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
        
        
            
        var movieSessionSeat =  MovieSessionSeat.Create(movieSessionId,
            seatNumber,seatRow, price);

        movieSessionSeat.Select(shoppingCartId, ComputeMD5(shoppingCartId.ToString()));
        
        movieSessionSeat.Status.Should().Be( SeatStatus.Selected);
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
            foreach (byte b in hashValue) {
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
        
        var movieSessionSeat = PrepareSelectedMovieSessionSeat(movieSessionId, seatNumber, seatRow, price, shoppingCartId);
        
        
        movieSessionSeat.ReturnToAvailable();
     
        movieSessionSeat.Status.Should().Be( SeatStatus.Available);
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
        
        var movieSessionSeat = PrepareSelectedMovieSessionSeat(movieSessionId, seatNumber, seatRow, price, shoppingCartId);
        
        
        movieSessionSeat.Reserve(shoppingCartId);
        movieSessionSeat.Status.Should().Be( SeatStatus.Reserved);
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
        
        var movieSessionSeat = PrepareSelectedMovieSessionSeat(movieSessionId, seatNumber, seatRow, price, shoppingCartId);
        
        movieSessionSeat.ReturnToAvailable();
        
        movieSessionSeat.Status.Should().Be( SeatStatus.Available);
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
            
        var movieSessionSeat = PrepareReservedMovieSessionSeat(movieSessionId, seatNumber, seatRow, price, shoppingCartId);
        
        movieSessionSeat.Sel(shoppingCartId);

        movieSessionSeat.Status.Should().Be( SeatStatus.Sold);
        movieSessionSeat.MovieSessionId.Should().Be(movieSessionId);
        movieSessionSeat.ShoppingCartId.Should().Be(shoppingCartId);
        movieSessionSeat.Price.Should().Be(20);
    }
    
    private static MovieSessionSeat PrepareSelectedMovieSessionSeat(Guid movieSessionId, short seatNumber, short seatRow,
        decimal price, Guid shoppingCartId)
    {
        var movieSessionSeat =  MovieSessionSeat.Create(movieSessionId,
            seatNumber, seatRow, price);

        movieSessionSeat.MovieSessionId.Should().Be(movieSessionId);
        movieSessionSeat.Price.Should().Be(price);
        
        
        movieSessionSeat.Select(shoppingCartId, ComputeMD5(shoppingCartId.ToString()));
        movieSessionSeat.ShoppingCartId.Should().Be(shoppingCartId);

        return movieSessionSeat;
    }

    private static MovieSessionSeat PrepareReservedMovieSessionSeat(Guid movieSessionId, short seatNumber, short seatRow,
        decimal price, Guid shoppingCartId)
    {
        var movieSessionSeat = PrepareSelectedMovieSessionSeat(movieSessionId, seatNumber, seatRow, price, shoppingCartId);

        movieSessionSeat.Reserve(shoppingCartId);
        movieSessionSeat.Status.Should().Be(SeatStatus.Reserved);

        return movieSessionSeat;
    }
}