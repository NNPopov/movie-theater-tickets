using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Application.Exceptions;
using CinemaTicketBooking.Application.MovieSessions.DTOs;
using CinemaTicketBooking.Domain.MovieSessions;
using CinemaTicketBooking.Domain.MovieSessions.Abstractions;

namespace CinemaTicketBooking.Application.MovieSessions.Queries;

public record GetMovieSessionsQuery(Guid MovieId) : IRequest<IReadOnlyCollection<MovieSessionsDto>>;

public class
    GetMovieSessionsQueryHandler : IRequestHandler<GetMovieSessionsQuery, IReadOnlyCollection<MovieSessionsDto>>
{
    private readonly IMapper _mapper;
    private readonly IMovieSessionsRepository _movieSessionsRepository;

    public GetMovieSessionsQueryHandler(IMapper mapper,
        IMovieSessionsRepository movieSessionsRepository)
    {
        _mapper = mapper;
        _movieSessionsRepository = movieSessionsRepository;
    }

    public async Task<IReadOnlyCollection<MovieSessionsDto>> Handle(GetMovieSessionsQuery request,
        CancellationToken cancellationToken)
    {
        var movieSessions = await _movieSessionsRepository
            .GetAllAsync(
                t => t.MovieId == request.MovieId &&
                     t.SessionDate >= TimeProvider.System.GetUtcNow().DateTime &&
                     t.TicketsForSale > t.SoldTickets,
                cancellationToken);

        if (movieSessions == null || !movieSessions.Any())
            throw new ContentNotFoundException(request.MovieId.ToString(), nameof(MovieSession));

        return movieSessions.Select(t => _mapper.Map<MovieSessionsDto>(t)).ToList();
    }
}