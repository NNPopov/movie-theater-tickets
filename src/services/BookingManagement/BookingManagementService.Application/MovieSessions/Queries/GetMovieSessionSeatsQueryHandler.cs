using CinemaTicketBooking.Application.Abstractions.Repositories;
using CinemaTicketBooking.Domain.MovieSessions.Abstractions;
using CinemaTicketBooking.Domain.Seats;
using CinemaTicketBooking.Domain.Seats.Abstractions;

namespace CinemaTicketBooking.Application.MovieSessions.Queries;

public record GetMovieSessionSeatsQuery(Guid Id) : IRequest<ICollection<MovieSessionSeatDto>>;

public class
    GetMovieSessionSeatsQueryHandler : IRequestHandler<GetMovieSessionSeatsQuery, ICollection<MovieSessionSeatDto>>
{
    private IMovieSessionsRepository _movieSessionsRepository;
    private readonly IMovieSessionSeatRepository _movieSessionSeatRepository;

    public GetMovieSessionSeatsQueryHandler(
        IMovieSessionsRepository movieSessionsRepository,
        IMovieSessionSeatRepository movieSessionSeatRepository)
    {
        _movieSessionsRepository = movieSessionsRepository;
        _movieSessionSeatRepository = movieSessionSeatRepository;
    }

    public async Task<ICollection<MovieSessionSeatDto>> Handle(GetMovieSessionSeatsQuery request,
        CancellationToken cancellationToken)
    {
        var movieSession = await _movieSessionsRepository
            .GetByIdAsync(
                request.Id, cancellationToken);


        var seatsInAuditorium = await
            _movieSessionSeatRepository.GetByMovieSessionIdAsync(movieSession.Id, cancellationToken);

        var seats =
            seatsInAuditorium.Select(allSeats =>
                new MovieSessionSeatDto
                {
                    SeatNumber = allSeats.SeatNumber,
                    Row = allSeats.SeatRow,
                    Blocked = allSeats.Status == SeatStatus.Available ? false : true,
                    SeatStatus = allSeats.Status,
                    HashId = allSeats.HashId
                });

        return seats.ToList();
    }
}

public record MovieSessionSeatDto
{
    public short SeatNumber { get; init; }
    public short Row { get; init; }
    public bool Blocked { get; init; }
    public SeatStatus SeatStatus { get; init; }
    public string HashId { get; init; }
}