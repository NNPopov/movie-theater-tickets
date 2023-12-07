using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Domain.MovieSessions;
using CinemaTicketBooking.Domain.MovieSessions.Abstractions;
using CinemaTicketBooking.Domain.Seats;
using CinemaTicketBooking.Domain.Seats.Abstractions;

namespace CinemaTicketBooking.Application.MovieSessions.Commands.CreateShowtime;

public record CreateMovieSessionCommand(
    Guid MovieId,
    Guid AuditoriumId,
    DateTime SessionDate) : IRequest<Guid>;

internal sealed class CreateMovieSessionCommandHandler : IRequestHandler<CreateMovieSessionCommand, Guid>
{
    // private readonly IMapper _mapper;
    private IMovieSessionsRepository _movieSessionsRepository;

    private ICinemaHallRepository _cinemaHallRepository;

    //private IShoppingCartSeatLifecycleManager _seatStateRepository;
    private IMoviesRepository _moviesRepository;
    private readonly IMovieSessionSeatRepository _movieSessionSeatRepository;


    public CreateMovieSessionCommandHandler(
        IMovieSessionsRepository movieSessionsRepository,
        ICinemaHallRepository cinemaHallRepository,
        IMoviesRepository moviesRepository,
        IMovieSessionSeatRepository movieSessionSeatRepository)
    {
        _movieSessionsRepository = movieSessionsRepository;
        _cinemaHallRepository = cinemaHallRepository;
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
        {
            throw new Exception();
        }

        var movie = await _moviesRepository.GetByIdAsync(request.MovieId, cancellationToken);

        if (movie == null)
        {
            throw new Exception();
        }

        var showtime = MovieSession.Create(movie.Id,
            auditorium.Id,
            request.SessionDate,
            auditorium.Seats.Count);

        foreach (var seat in auditorium.Seats)
        {
            var showtimeSeat = MovieSessionSeat.Create(showtime.Id, seat.Row, seat.SeatNumber, 15);

            await _movieSessionSeatRepository.AddAsync(showtimeSeat, cancellationToken);
        }

        await _movieSessionsRepository.MovieSession(showtime, cancellationToken);

        return showtime.Id;
    }
}