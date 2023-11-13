using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Application.Exceptions;
using CinemaTicketBooking.Application.MovieSessions.DTOs;
using CinemaTicketBooking.Domain.MovieSessions;
using CinemaTicketBooking.Domain.MovieSessions.Abstractions;

namespace CinemaTicketBooking.Application.MovieSessions.Queries;

public record GetMovieSessionByIdQuery(Guid MovieSessionId) : IRequest<MovieSessionsDto>;

public class GetMovieSessionByIdQueryHandler : IRequestHandler<GetMovieSessionByIdQuery, MovieSessionsDto>
{
    private readonly IMapper _mapper;
    private IMovieSessionsRepository _movieSessionsRepository;

    public GetMovieSessionByIdQueryHandler(IMapper mapper,
        IMovieSessionsRepository movieSessionsRepository)
    {
        _mapper = mapper;
        _movieSessionsRepository = movieSessionsRepository;
    }

    public async Task<MovieSessionsDto> Handle(GetMovieSessionByIdQuery request,
        CancellationToken cancellationToken)
    {
        var movieSessions = await _movieSessionsRepository
            .GetByIdAsync(request.MovieSessionId,
                cancellationToken);

        if (movieSessions == null )
            throw new ContentNotFoundException(request.MovieSessionId.ToString(), nameof(MovieSession));

        return _mapper.Map<MovieSessionsDto>(movieSessions);
    }
}