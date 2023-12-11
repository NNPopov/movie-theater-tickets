using CinemaTicketBooking.Application.Abstractions.Repositories;
using CinemaTicketBooking.Domain.MovieSessions.Abstractions;
using CinemaTicketBooking.Domain.Seats;
using CinemaTicketBooking.Domain.Seats.Abstractions;

namespace CinemaTicketBooking.Application.MovieSessions.Queries;

public record GetActiveMoviesQuery() : IRequest<ICollection<ActiveMovieDto>>;

public class
    GetActiveMovieQueryHandler : IRequestHandler<GetActiveMoviesQuery, ICollection<ActiveMovieDto>>
{
    private IMovieSessionsRepository _movieSessionsRepository;
    private readonly IMovieSessionSeatRepository _movieSessionSeatRepository;

    public GetActiveMovieQueryHandler(
        IMovieSessionsRepository movieSessionsRepository,
        IMovieSessionSeatRepository movieSessionSeatRepository)
    {
        _movieSessionsRepository = movieSessionsRepository;
        _movieSessionSeatRepository = movieSessionSeatRepository;
    }

    public async Task<ICollection<ActiveMovieDto>> Handle(GetActiveMoviesQuery request,
        CancellationToken cancellationToken)
    {
        var movieSession = await _movieSessionsRepository
            .GetAllAsync(t => t.SessionDate > TimeProvider.System.GetUtcNow(), cancellationToken);


        return movieSession.Select(t => new ActiveMovieDto(t.MovieId, "Movie")).Distinct().ToList();
    }
}

public record ActiveMovieDto(Guid Id, string Title);