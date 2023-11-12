using CinemaTicketBooking.Application.Exceptions;
using CinemaTicketBooking.Domain.Exceptions;
using CinemaTicketBooking.Domain.MovieSessions;
using CinemaTicketBooking.Domain.MovieSessions.Abstractions;
using CinemaTicketBooking.Domain.Seats;
using CinemaTicketBooking.Domain.Seats.Abstractions;

namespace CinemaTicketBooking.Domain.Services;

public sealed class MovieSessionSeatService
{
    private readonly IMovieSessionSeatRepository _movieSessionSeatRepository;
    private readonly IMovieSessionsRepository _movieSessionsRepository;

    public MovieSessionSeatService(IMovieSessionSeatRepository movieSessionSeatRepository,
        IMovieSessionsRepository movieSessionsRepository)
    {
        _movieSessionSeatRepository = movieSessionSeatRepository;
        _movieSessionsRepository = movieSessionsRepository;
    }


    public async Task PurchaseSeat(Guid movieSessionId,
        short seatRow,
        short seatNumber,
        Guid shoppingCartId,
        CancellationToken cancellationToken)
    {
        await CheckSeatSaleAvailability(movieSessionId, cancellationToken);

        var movieSessionSeat = await GetMovieSessionSeat(movieSessionId, seatRow, seatNumber, cancellationToken);

        movieSessionSeat.Sel(shoppingCartId);

        await _movieSessionSeatRepository.UpdateAsync(movieSessionSeat, cancellationToken);
    }


    public async Task ReserveSeat(Guid movieSessionId,
        short seatRow,
        short seatNumber,
        Guid shoppingCartId,
        CancellationToken cancellationToken)
    {
        await CheckSeatSaleAvailability(movieSessionId, cancellationToken);

        var movieSessionSeat = await GetMovieSessionSeat(movieSessionId, seatRow, seatNumber, cancellationToken);

        movieSessionSeat.Reserve(shoppingCartId);

        await _movieSessionSeatRepository.UpdateAsync(movieSessionSeat, cancellationToken);
    }

    public async Task ReturnToAvailable(Guid movieSessionId,
        short seatRow,
        short seatNumber,
        CancellationToken cancellationToken)
    {
        var movieSessionSeat = await GetMovieSessionSeat(movieSessionId, seatRow, seatNumber, cancellationToken);


        movieSessionSeat.ReturnToAvailable();
        await _movieSessionSeatRepository.UpdateAsync(movieSessionSeat, cancellationToken);
    }

    public async Task SelectSeat(Guid movieSessionId,
        short seatRow,
        short seatNumber,
        Guid shoppingCartId,
        string hashId,
        CancellationToken cancellationToken)
    {
        await CheckSeatSaleAvailability(movieSessionId, cancellationToken);

        var movieSessionSeat = await GetMovieSessionSeat(movieSessionId, seatRow, seatNumber, cancellationToken);

        var result = movieSessionSeat.Select(shoppingCartId, hashId);

        if (result.IsSuccess)
        {
            await _movieSessionSeatRepository.UpdateAsync(movieSessionSeat, cancellationToken);
        }
        else
        {
            throw new ConflictException(nameof(MovieSessionSeat), this.ToString());
        }
    }

    private async Task<MovieSessionSeat> GetMovieSessionSeat(Guid movieSessionId,
        short seatRow,
        short seatNumber,
        CancellationToken cancellationToken)
    {
        return
            await _movieSessionSeatRepository.GetByIdAsync(movieSessionId,
                seatRow,
                seatNumber, cancellationToken) ??
            throw new ContentNotFoundException(
                $@"movieSessionId:{movieSessionId}, seatRow:{seatRow}, seatNumber:{seatNumber}",
                nameof(MovieSessionSeat));
    }

    private async Task CheckSeatSaleAvailability(Guid movieSessionId,
        CancellationToken cancellationToken)
    {
        var movieSession = await _movieSessionsRepository
                               .GetWithTicketsByIdAsync(
                                   movieSessionId, cancellationToken) ??
                           throw new ContentNotFoundException(movieSessionId.ToString(), nameof(MovieSession));

        if (movieSession.SalesTerminated)
        {
            throw new Exception($"{nameof(MovieSession)} has been terminated.");
        }
    }
}