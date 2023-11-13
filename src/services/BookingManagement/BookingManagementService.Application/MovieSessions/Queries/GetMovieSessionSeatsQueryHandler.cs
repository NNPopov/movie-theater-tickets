using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Domain.MovieSessions.Abstractions;
using CinemaTicketBooking.Domain.Seats;
using CinemaTicketBooking.Domain.Seats.Abstractions;

namespace CinemaTicketBooking.Application.MovieSessions.Queries;

public record GetMovieSessionSeatsQuery(Guid Id) : IRequest<ICollection<MovieSessionSeatDto>>;

public class GetMovieSessionSeatsQueryHandler : IRequestHandler<GetMovieSessionSeatsQuery, ICollection<MovieSessionSeatDto>>
{
    private readonly IMapper _mapper;
    private IMovieSessionsRepository _movieSessionsRepository;
    private ISeatStateRepository _seatStateRepository;
    private readonly IMovieSessionSeatRepository _movieSessionSeatRepository;

    public GetMovieSessionSeatsQueryHandler(IMapper mapper,
        IMovieSessionsRepository movieSessionsRepository,
        ISeatStateRepository seatStateRepository,
        IMovieSessionSeatRepository movieSessionSeatRepository)
    {
        _mapper = mapper;
        _movieSessionsRepository = movieSessionsRepository;
        _seatStateRepository = seatStateRepository;
        _movieSessionSeatRepository = movieSessionSeatRepository;
    }

    public async Task<ICollection<MovieSessionSeatDto>> Handle(GetMovieSessionSeatsQuery request,
        CancellationToken cancellationToken)
    {
        var showtimes = await _movieSessionsRepository
            .GetWithTicketsByIdAsync(
                request.Id, cancellationToken);

        var reservedSeatsTask = _seatStateRepository.GetReservedSeats(showtimes.Id);

        var seatsInAuditoriumTask =
            _movieSessionSeatRepository.GetByMovieSessionIdAsync(showtimes.Id, cancellationToken);

        await Task.WhenAll(seatsInAuditoriumTask, reservedSeatsTask);

        var reservedSeats = reservedSeatsTask.Result;

        var seatsInAuditorium = seatsInAuditoriumTask.Result;
        
        var soldSeats = seatsInAuditorium.Count(t => t.Status == SeatStatus.Reserved);

        // var seats =
        //     from allSeats in seatsInAuditorium.ToList()
        //     join purchasedS in reservedSeats.ToList()
        //         on new { seat = allSeats.SeatNumber, row = allSeats.SeatRow } equals
        //         new { seat = purchasedS.Number, row = purchasedS.Row } into gj
        //     from purchased in gj.DefaultIfEmpty()
        //     select new MovieSessionSeatDto
        //     {
        //         SeatNumber = allSeats.SeatNumber,
        //         Row = allSeats.SeatRow,
        //         Blocked = purchased != null ? true : allSeats.Status == SeatStatus.Available ? false : true,
        //         SeatStatus = purchased != null ? SeatStatus.Blocked : allSeats.Status
            // };
        
        var seats =
            seatsInAuditorium.Select( allSeats=>
             new MovieSessionSeatDto
            {
                SeatNumber = allSeats.SeatNumber,
                Row = allSeats.SeatRow,
                Blocked = allSeats.Status == SeatStatus.Available ? false : true,
                SeatStatus =  allSeats.Status,
                HashId = allSeats.HashId
            });

        return seats.ToList();
    }
}

public class MovieSessionSeatDto
{
    public short SeatNumber { get; init; }
    public short Row { get; init; }
    public bool Blocked { get; init; }
    public SeatStatus SeatStatus { get; init; }
    public string HashId { get; init; }
}