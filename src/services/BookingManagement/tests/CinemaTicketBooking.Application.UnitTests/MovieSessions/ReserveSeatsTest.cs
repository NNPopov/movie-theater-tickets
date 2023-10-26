using CinemaTicketBooking.Domain.MovieSessions;
using FluentAssertions;
using Xunit;

namespace CinemaTicketBooking.Application.UnitTests.MovieSessions;

public class MovieSessionSpecification
{
   private const int ALLOTTED_TICKETS = 100;
    
   [Fact]
   public void SetSoldTickets_UpdatesValue()
   {
      // Arrange
      var movieSession = MovieSession.Create(Guid.NewGuid(), Guid.NewGuid(), DateTime.Now, 
       //  new List<SeatMovieSession>(),
         ALLOTTED_TICKETS);

      // Act
      movieSession.SetSoldTickets(10);

      // Assert
      movieSession.SoldTickets.Should().Be(10);
   }

   [Fact]
   public void Create_ReturnsNewMovieSession()
   {
      // Arrange
      Guid movieId = Guid.NewGuid();
      Guid auditoriumId = Guid.NewGuid();
      DateTime sessionDate = DateTime.Now;

      // Act
      var movieSession = MovieSession.Create(movieId, auditoriumId, 
         sessionDate,
         //new List<SeatMovieSession>(),
         ALLOTTED_TICKETS);

      // Assert
      
      movieSession.Should().NotBeNull();
      movieSession.MovieId.Should().Be(movieId);
      movieSession.AuditoriumId.Should().Be(auditoriumId);
      movieSession.SessionDate.Should().Be(sessionDate);
      
   }

}