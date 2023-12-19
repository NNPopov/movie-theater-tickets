using CinemaTicketBooking.Application.Abstractions.Services;
using CinemaTicketBooking.Application.MovieSessions.Queries;
using CinemaTicketBooking.Application.MovieSessionSeats.Queries;
using CinemaTicketBooking.Infrastructure.Services;
using MediatR;
using NSubstitute;
using FluentAssertions;


namespace BookingManagementService.Infrastructure.UnitTests;
public class ActiveMovieSessionSeatsDataCacheServiceSpecification
{
    [Fact]
    public async Task AddOrUpdateMovieSessionSeatsCache_UpdatesCacheCorrectly()
    {
        // Arrange
        var cacheService = Substitute.For<ICacheService>();
        var mediator = Substitute.For<IMediator>();
        var logger = Substitute.For<Serilog.ILogger>();
        var service = new ActiveMovieSessionSeatsDataCacheService(cacheService, mediator, logger);

        var movieSessionId = Guid.NewGuid();
        var seats = new List<MovieSessionSeatDto>(); // Populate with test data
        var expirationTime = DateTime.UtcNow.AddHours(1);
        var data = new ActiveMovieSessionSeatsDTO(movieSessionId, seats, expirationTime);

        // Act
        await service.AddOrUpdateMovieSessionSeatsCache(data);

        // Assert
    cacheService.Received(1)
            .Set(
            Arg.Is<string>(key => key.Contains(movieSessionId.ToString())),
            Arg.Is<ActiveMovieSessionSeatsDTO>(s => s.Equals(data)),
            Arg.Is<TimeSpan>(ts => ts.ToString().Substring(0,8) == expirationTime.Subtract(TimeProvider.System.GetUtcNow().DateTime).ToString().Substring(0,8))
        )
           .ConfigureAwait(false);
    }

    [Fact]
    public async Task AddOrUpdateMovieSessionSeatsCache_LogsUpdate()
    {
        // Arrange
        var cacheService = Substitute.For<ICacheService>();
        var mediator = Substitute.For<IMediator>();
        var logger = Substitute.For<Serilog.ILogger>();
        var service = new ActiveMovieSessionSeatsDataCacheService(cacheService, mediator, logger);
        var data = new ActiveMovieSessionSeatsDTO(Guid.NewGuid(), new List<MovieSessionSeatDto>(), DateTime.UtcNow.AddHours(1));

        // Act
        await service.AddOrUpdateMovieSessionSeatsCache(data);

        // Assert
        
     
        logger.Received(1).Debug(Arg.Is<string>(str => str.Contains("MovieSessionSeatsCache has been updated")), data.MovieSessionId);
    }
    
    
    
    [Fact]
    public async Task GetMovieSessionSeatsData_ReturnsCorrectData_WhenFoundInCache()
    {
        // Arrange
        var cacheService = Substitute.For<ICacheService>();
        var movieSessionId = Guid.NewGuid();
        var expectedDto = new ActiveMovieSessionSeatsDTO(movieSessionId, new List<MovieSessionSeatDto>(), DateTime.UtcNow);
        cacheService.TryGet<ActiveMovieSessionSeatsDTO>(Arg.Any<string>())
            .Returns(Task.FromResult(expectedDto));

        var mediator = Substitute.For<IMediator>();
        var logger = Substitute.For<Serilog.ILogger>();
        var service = new ActiveMovieSessionSeatsDataCacheService(cacheService, mediator, logger);

        // Act
        var result = await service.GetMovieSessionSeatsData(movieSessionId);

        // Assert
        result.Should().BeEquivalentTo(expectedDto);
    }

    [Fact]
    public async Task GetMovieSessionSeatsData_ReturnsNull_WhenNotInCache()
    {
        // Arrange
        var cacheService = Substitute.For<ICacheService>();
        var movieSessionId = Guid.NewGuid();
        cacheService.TryGet<ActiveMovieSessionSeatsDTO>(Arg.Any<string>())
            .Returns(Task.FromResult<ActiveMovieSessionSeatsDTO>(null));

        var mediator = Substitute.For<IMediator>();
        var logger = Substitute.For<Serilog.ILogger>();
        var service = new ActiveMovieSessionSeatsDataCacheService(cacheService, mediator, logger);

        // Act
        var result = await service.GetMovieSessionSeatsData(movieSessionId);

        // Assert
        result.Should().BeNull();
    }
    
    
    
    [Fact]
    public async Task GetActualMovieSessionSeatsData_ReturnsData_WhenQuerySuccessful()
    {
        // Arrange
        var mediator = Substitute.For<IMediator>();
        var cacheService = Substitute.For<ICacheService>();
        var logger = Substitute.For<Serilog.ILogger>();
        var service = new ActiveMovieSessionSeatsDataCacheService(cacheService, mediator, logger);

        var movieSessionId = Guid.NewGuid();
        var expectedDto = new ActiveMovieSessionSeatsDTO(movieSessionId, new List<MovieSessionSeatDto>(), DateTime.UtcNow);
        mediator.Send(Arg.Any<GetActiveMovieSessionSeatsQuery>()).Returns(expectedDto);

        // Act
        var result = await service.GetActualMovieSessionSeatsData(movieSessionId);

        // Assert
        result.Should().BeEquivalentTo(expectedDto);
    }

    [Fact]
    public async Task GetActualMovieSessionSeatsData_LogsErrorAndReturnsNull_WhenDataNotFound()
    {
        // Arrange
        var mediator = Substitute.For<IMediator>();
        var cacheService = Substitute.For<ICacheService>();
        var logger = Substitute.For<Serilog.ILogger>();
        var service = new ActiveMovieSessionSeatsDataCacheService(cacheService, mediator, logger);

        var movieSessionId = Guid.NewGuid();
        mediator.Send(Arg.Any<GetActiveMovieSessionSeatsQuery>()).Returns((ActiveMovieSessionSeatsDTO)null);

        // Act
        var result = await service.GetActualMovieSessionSeatsData(movieSessionId);

        // Assert
        result.Should().BeNull();
        logger.Received(1).Error(Arg.Is<string>(s => s.Contains("Movie session seats not found")), movieSessionId);
    }
}