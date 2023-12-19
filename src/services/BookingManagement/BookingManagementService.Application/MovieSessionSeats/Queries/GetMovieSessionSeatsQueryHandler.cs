using CinemaTicketBooking.Application.Abstractions.Repositories;
using CinemaTicketBooking.Domain.MovieSessions.Abstractions;
using CinemaTicketBooking.Domain.Seats;
using CinemaTicketBooking.Domain.Seats.Abstractions;
using Serilog;

namespace CinemaTicketBooking.Application.MovieSessions.Queries;

public record GetMovieSessionSeatsQuery(Guid Id) : IRequest<ICollection<MovieSessionSeatDto>>;

public class
    GetMovieSessionSeatsQueryHandler : IRequestHandler<GetMovieSessionSeatsQuery, ICollection<MovieSessionSeatDto>>
{
    private readonly IMovieSessionSeatRepository _movieSessionSeatRepository;
    private readonly IMediator _mediator;
    private readonly ILogger _logger;
    
    public GetMovieSessionSeatsQueryHandler(
        IMovieSessionSeatRepository movieSessionSeatRepository, 
        IMediator mediator, ILogger logger)
    {
        _movieSessionSeatRepository = movieSessionSeatRepository;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<ICollection<MovieSessionSeatDto>> Handle(GetMovieSessionSeatsQuery request,
        CancellationToken cancellationToken)
    {
        var movieSession = await _mediator.Send(new GetMovieSessionByIdQuery(request.Id), cancellationToken);

        if (movieSession is null)
        {
            _logger.Error("Movie session not found:{@MovieSessionId}", request.Id);
            return default;
        }

        var seatsInAuditorium = await
            _movieSessionSeatRepository.GetByMovieSessionIdAsync(movieSession.Id, cancellationToken);

        var seats =
            seatsInAuditorium.Select(allSeats =>
                new MovieSessionSeatDto(
                    SeatNumber: allSeats.SeatNumber,
                    Row: allSeats.SeatRow,
                    Blocked: allSeats.Status == SeatStatus.Available ? false : true,
                    SeatStatus: allSeats.Status,
                    HashId: allSeats.ShoppingCartHashId)
            );

        return seats.ToList();
    }
}

public record MovieSessionSeatDto(short SeatNumber, short Row, bool Blocked, SeatStatus SeatStatus, string HashId);