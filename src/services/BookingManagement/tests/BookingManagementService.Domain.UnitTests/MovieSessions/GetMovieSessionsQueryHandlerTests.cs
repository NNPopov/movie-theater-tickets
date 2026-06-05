using System.Linq.Expressions;
using AutoMapper;
using CinemaTicketBooking.Application.MovieSessions.DTOs;
using CinemaTicketBooking.Application.MovieSessions.Queries;
using CinemaTicketBooking.Domain.MovieSessions;
using CinemaTicketBooking.Domain.MovieSessions.Abstractions;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CinemaTicketBooking.Application.UnitTests.MovieSessions;

// Carve-out B for slice 0002_content_not_found_404: the get-movie-sessions query no longer treats
// "no upcoming sessions" as a not-found error. It returns an empty collection (→ 200 [] at the
// endpoint) when the repository yields nothing, and the mapped collection when sessions exist.
public class GetMovieSessionsQueryHandlerTests
{
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly IMovieSessionsRepository _repository = Substitute.For<IMovieSessionsRepository>();

    private GetMovieSessionsQueryHandler CreateHandler() => new(_mapper, _repository);

    [Fact]
    public async Task Handle_Should_ReturnEmptyCollection_When_NoUpcomingSessions()
    {
        // Arrange
        var movieId = Guid.NewGuid();
        _repository.GetAllAsync(Arg.Any<Expression<Func<MovieSession, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<MovieSession>());
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetMovieSessionsQuery(movieId), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_Should_ReturnMappedCollection_When_SessionsExist()
    {
        // Arrange
        var movieId = Guid.NewGuid();
        var session = MovieSession.Create(movieId, Guid.NewGuid(), DateTime.UtcNow.AddDays(1), 100);
        _repository.GetAllAsync(Arg.Any<Expression<Func<MovieSession, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<MovieSession> { session });
        var dto = new MovieSessionsDto { Id = session.Id, MovieId = movieId };
        _mapper.Map<MovieSessionsDto>(session).Returns(dto);
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetMovieSessionsQuery(movieId), CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result.Should().Contain(dto);
    }
}
