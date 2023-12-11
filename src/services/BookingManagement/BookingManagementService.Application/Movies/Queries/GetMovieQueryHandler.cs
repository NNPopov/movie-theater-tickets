using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Application.Exceptions;
using CinemaTicketBooking.Domain.Entities;
using CinemaTicketBooking.Domain.Movies;
using FluentValidation.Results;

namespace CinemaTicketBooking.Application.Movies.Queries;

public record GetMovieQuery(Guid Id) : IRequest<MovieDto>;

internal sealed class GetMovieQueryHandler : IRequestHandler<GetMovieQuery, MovieDto>
{
    private readonly IMoviesRepository _moviesRepository;
    private readonly IMapper _mapper;

    public GetMovieQueryHandler(IMoviesRepository moviesRepository, IMapper mapper)
    {
        _moviesRepository = moviesRepository;
        _mapper = mapper;
    }

    public async Task<MovieDto> Handle(GetMovieQuery request, CancellationToken cancellationToken)
    {
        var movie = await _moviesRepository.GetByIdAsync(request.Id,
            cancellationToken);

        if (movie == null)
        {
            throw new ContentNotFoundException(request.Id.ToString(), nameof(Movie));
        }

        return _mapper.Map<MovieDto>(movie);
    }
}