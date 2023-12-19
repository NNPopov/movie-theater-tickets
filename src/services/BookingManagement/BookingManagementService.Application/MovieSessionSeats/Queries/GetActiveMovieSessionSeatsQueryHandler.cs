using CinemaTicketBooking.Application.MovieSessions.Queries;
using CinemaTicketBooking.Domain.MovieSessions.Abstractions;
using CinemaTicketBooking.Domain.Seats;
using CinemaTicketBooking.Domain.Seats.Abstractions;
using Serilog;

namespace CinemaTicketBooking.Application.MovieSessionSeats.Queries;

public record GetActiveMovieSessionSeatsQuery(Guid Id) : IRequest<ActiveMovieSessionSeatsDTO>;

public class
    GetActiveMovieSessionSeatsQueryHandler : IRequestHandler<GetActiveMovieSessionSeatsQuery, ActiveMovieSessionSeatsDTO
    ?>
{
    private IMovieSessionsRepository _movieSessionsRepository;
    private readonly IMovieSessionSeatRepository _movieSessionSeatRepository;
    private readonly ILogger _logger;

    public GetActiveMovieSessionSeatsQueryHandler(
        IMovieSessionsRepository movieSessionsRepository,
        IMovieSessionSeatRepository movieSessionSeatRepository, ILogger logger)
    {
        _movieSessionsRepository = movieSessionsRepository;
        _movieSessionSeatRepository = movieSessionSeatRepository;
        _logger = logger;
    }

    public async Task<ActiveMovieSessionSeatsDTO?> Handle(GetActiveMovieSessionSeatsQuery request,
        CancellationToken cancellationToken)
    {
        
        var movieSession = await _movieSessionsRepository
            .GetByIdAsync(
                request.Id, cancellationToken);


        if (movieSession is null)
        {
            _logger.Error("Movie session not found:{@MovieSessionId}", request.Id);
            return default;
        }

        if (movieSession.SessionDate < TimeProvider.System.GetUtcNow())
        {
            _logger.Error("Movie session already started:{@MovieSession}", movieSession);
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


        return new ActiveMovieSessionSeatsDTO(
            MovieSessionId: movieSession.Id,
            Seats: seats.ToList(),
            MovieSessionExpirationTime: movieSession.SessionDate);
    }
}

public record ActiveMovieSessionSeatsDTO(
    Guid MovieSessionId,
    ICollection<MovieSessionSeatDto> Seats,
    DateTime MovieSessionExpirationTime);