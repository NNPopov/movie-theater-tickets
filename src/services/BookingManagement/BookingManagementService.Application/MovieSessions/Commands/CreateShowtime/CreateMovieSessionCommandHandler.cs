using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Domain.MovieSessions;
using CinemaTicketBooking.Domain.Seats;

namespace CinemaTicketBooking.Application.MovieSessions.Commands.CreateShowtime;

public record CreateMovieSessionCommand(Guid MovieId,
    Guid AuditoriumId,
    DateTime SessionDate) : IRequest<Guid>;

public class CreateMovieSessionCommandHandler : IRequestHandler<CreateMovieSessionCommand, Guid>
{
   // private readonly IMapper _mapper;
    private IMovieSessionsRepository _movieSessionsRepository;
    private ICinemaHallRepository _cinemaHallRepository;
    //private ISeatStateRepository _seatStateRepository;
    private IMoviesRepository _moviesRepository;
    private readonly IMovieSessionSeatRepository _movieSessionSeatRepository;


    public CreateMovieSessionCommandHandler(//IMapper mapper,
        IMovieSessionsRepository movieSessionsRepository,
        ICinemaHallRepository cinemaHallRepository,
       // ISeatStateRepository seatStateRepository,
        IMoviesRepository moviesRepository,
        IMovieSessionSeatRepository movieSessionSeatRepository)
    {
      //  _mapper = mapper;
        _movieSessionsRepository = movieSessionsRepository;
        _cinemaHallRepository = cinemaHallRepository;
      //  _seatStateRepository = seatStateRepository;
        _moviesRepository = moviesRepository;
        _movieSessionSeatRepository = movieSessionSeatRepository;
    }

    public async Task<Guid> Handle(CreateMovieSessionCommand request,
        CancellationToken cancellationToken)
    {
        var auditorium = await _cinemaHallRepository
            .GetAsync(
                request.AuditoriumId, cancellationToken);

        if (auditorium == null)
            throw new Exception();

        var movie = await _moviesRepository.GetByIdAsync(request.MovieId, cancellationToken);

        if (movie == null)
            throw new Exception();

        var showtime = MovieSession.Create(movie.Id,
            auditorium.Id,
            request.SessionDate,
           // auditorium.Seats.Select(t => new SeatMovieSession(t.SeatNumber, t.Row)).ToList(), 
            auditorium.Seats.Count);

        foreach (var seat in auditorium.Seats)
        {
            var showtimeSeat =  MovieSessionSeat.Create(showtime.Id, seat.Row, seat.SeatNumber, 15);

            await _movieSessionSeatRepository.AddAsync(showtimeSeat, cancellationToken);
        }

        await _movieSessionsRepository.MovieSession(showtime, cancellationToken);

        return showtime.Id;
    }
}