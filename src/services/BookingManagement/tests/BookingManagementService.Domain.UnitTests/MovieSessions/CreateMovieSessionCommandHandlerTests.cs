using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Application.MovieSessions.Commands.CreateShowtime;
using CinemaTicketBooking.Domain.CinemaHalls;
using CinemaTicketBooking.Domain.Error;
using CinemaTicketBooking.Domain.MovieSessions;
using CinemaTicketBooking.Domain.MovieSessions.Abstractions;
using CinemaTicketBooking.Domain.Movies;
using CinemaTicketBooking.Domain.Seats;
using CinemaTicketBooking.Domain.Seats.Abstractions;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CinemaTicketBooking.Application.UnitTests.MovieSessions;

// RED acceptance gate for slice 0007_create_movie_session_result_http: the converted
// CreateMovieSession use-case must RETURN a Result<Guid> for each outcome instead of returning a bare
// Guid and throwing a bare Exception — referenced cinema hall (auditorium) missing => NotFoundError
// (was `throw new Exception()` => 500); referenced movie missing => NotFoundError (was
// `throw new Exception()` => 500); both present => Result.Success whose Value is the created session id
// (with one MovieSessionSeat created per auditorium seat and the session persisted). On EITHER
// missing-reference outcome the handler must short-circuit BEFORE any seat is added or the session is
// saved (the atomicity invariant the thrown path provided implicitly). This is the first endpoint use
// of the generic Result<Guid> built in slice 0001.
//
// This gate is RED until the conversion in plan.md section 5 lands: today Handle returns Task<Guid>
// (so a Result<Guid> cannot be asserted) and the two missing-reference cases `throw new Exception()`
// rather than returning a NotFoundError.
public class CreateMovieSessionCommandHandlerTests
{
    private readonly IMovieSessionsRepository _sessionRepository =
        Substitute.For<IMovieSessionsRepository>();

    private readonly ICinemaHallRepository _cinemaHallRepository =
        Substitute.For<ICinemaHallRepository>();

    private readonly IMoviesRepository _moviesRepository =
        Substitute.For<IMoviesRepository>();

    private readonly IMovieSessionSeatRepository _seatRepository =
        Substitute.For<IMovieSessionSeatRepository>();

    private CreateMovieSessionCommandHandler CreateHandler() =>
        new(_sessionRepository,
            _cinemaHallRepository,
            _moviesRepository,
            _seatRepository);

    [Fact]
    public async Task Handle_Should_ReturnSuccessWithSessionId_And_Persist_When_BothReferencesExist()
    {
        // Arrange — an auditorium with two seats and an existing movie.
        var auditorium = AuditoriumWithSeats((1, 1), (1, 2));
        var movie = ExistingMovie();
        var command = new CreateMovieSessionCommand(
            MovieId: movie.Id,
            AuditoriumId: auditorium.Id,
            SessionDate: DateTime.UtcNow.AddDays(1));

        _cinemaHallRepository.GetAsync(auditorium.Id, Arg.Any<CancellationToken>()).Returns(auditorium);
        _moviesRepository.GetByIdAsync(movie.Id, Arg.Any<CancellationToken>()).Returns(movie);
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert — Result<Guid> success carrying the created session id.
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        // One MovieSessionSeat created per auditorium seat, and the session persisted with that id.
        await _seatRepository.Received(2)
            .AddAsync(Arg.Any<MovieSessionSeat>(), Arg.Any<CancellationToken>());
        await _sessionRepository.Received(1)
            .MovieSession(Arg.Is<MovieSession>(s => s.Id == result.Value), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFoundError_And_NotPersist_When_AuditoriumDoesNotExist()
    {
        // Arrange — the referenced cinema hall (auditorium) is missing.
        var command = new CreateMovieSessionCommand(
            MovieId: Guid.NewGuid(),
            AuditoriumId: Guid.NewGuid(),
            SessionDate: DateTime.UtcNow.AddDays(1));

        _cinemaHallRepository.GetAsync(command.AuditoriumId, Arg.Any<CancellationToken>())
            .Returns((CinemaHall)null!);
        var handler = CreateHandler();

        // Act — was a bare `throw new Exception()` => 500; must now be a NotFoundError => 404.
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<NotFoundError>();

        // Atomicity: nothing created or persisted when the auditorium is missing.
        await _seatRepository.DidNotReceive()
            .AddAsync(Arg.Any<MovieSessionSeat>(), Arg.Any<CancellationToken>());
        await _sessionRepository.DidNotReceive()
            .MovieSession(Arg.Any<MovieSession>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFoundError_And_NotPersist_When_MovieDoesNotExist()
    {
        // Arrange — the auditorium exists, but the referenced movie is missing.
        var auditorium = AuditoriumWithSeats((1, 1), (1, 2));
        var command = new CreateMovieSessionCommand(
            MovieId: Guid.NewGuid(),
            AuditoriumId: auditorium.Id,
            SessionDate: DateTime.UtcNow.AddDays(1));

        _cinemaHallRepository.GetAsync(auditorium.Id, Arg.Any<CancellationToken>()).Returns(auditorium);
        _moviesRepository.GetByIdAsync(command.MovieId, Arg.Any<CancellationToken>())
            .Returns((Movie)null!);
        var handler = CreateHandler();

        // Act — was a bare `throw new Exception()` => 500; must now be a NotFoundError => 404.
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<NotFoundError>();

        // Atomicity: nothing created or persisted when the movie is missing, even though the
        // auditorium existed.
        await _seatRepository.DidNotReceive()
            .AddAsync(Arg.Any<MovieSessionSeat>(), Arg.Any<CancellationToken>());
        await _sessionRepository.DidNotReceive()
            .MovieSession(Arg.Any<MovieSession>(), Arg.Any<CancellationToken>());
    }

    private static CinemaHall AuditoriumWithSeats(params (short Row, short SeatNumber)[] seats) =>
        CinemaHall.Create("Hall A", "Main auditorium", seats.ToList());

    private static Movie ExistingMovie() =>
        Movie.Create("Some Title", DateTime.UtcNow.AddYears(-1), "tt0000000", "Some Stars");
}
