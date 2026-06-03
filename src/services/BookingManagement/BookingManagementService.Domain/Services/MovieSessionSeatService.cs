using CinemaTicketBooking.Application.Exceptions;
using CinemaTicketBooking.Domain.Error;
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


    public async Task<Result> SelSeats(Guid movieSessionId,
        ICollection<(short seatRow, short seatNumber)> seats,
        Guid shoppingCartId,
        CancellationToken cancellationToken)
    {
        var availability = await CheckSeatSaleAvailability(movieSessionId, cancellationToken);
        if (availability.IsFailure)
            return availability;

        var movieSessionSeats = new List<MovieSessionSeat>();

        foreach (var seat in seats)
        {
            var movieSessionSeat =
                await GetMovieSessionSeat(movieSessionId, seat.seatRow, seat.seatNumber, cancellationToken);


            var result = movieSessionSeat.Sell(shoppingCartId);

            if (result.IsFailure)
            {
                return result;
            }

            movieSessionSeats.Add(movieSessionSeat);
        }

        await _movieSessionSeatRepository.UpdateRangeAsync(movieSessionSeats, cancellationToken);

        return Result.Success();
    }


    public async Task<Result> ReserveSeats(Guid movieSessionId,
        ICollection<(short seatRow, short seatNumber)> seats,
        Guid shoppingCartId,
        CancellationToken cancellationToken)
    {
        var availability = await CheckSeatSaleAvailability(movieSessionId, cancellationToken);
        if (availability.IsFailure)
            return availability;

        var movieSessionSeats = new List<MovieSessionSeat>();
        foreach (var seat in seats)
        {
            var movieSessionSeat =
                await GetMovieSessionSeat(movieSessionId, seat.seatRow, seat.seatNumber, cancellationToken);

            var result = movieSessionSeat.Reserve(shoppingCartId);

            if (result.IsFailure)
            {
                return result;
            }

            movieSessionSeats.Add(movieSessionSeat);

        }
        await _movieSessionSeatRepository.UpdateRangeAsync(movieSessionSeats, cancellationToken);

        return Result.Success();
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

    public async Task<Result> SelectSeat(Guid movieSessionId,
        short seatRow,
        short seatNumber,
        Guid shoppingCartId,
        string hashId,
        CancellationToken cancellationToken)
    {
        var availability = await CheckSeatSaleAvailability(movieSessionId, cancellationToken);
        if (availability.IsFailure)
            return availability;

        var movieSessionSeat = await GetMovieSessionSeat(movieSessionId, seatRow, seatNumber, cancellationToken);

        var result = movieSessionSeat.Select(shoppingCartId, hashId);

        if (result.IsFailure)
        {
            return result;
        }

        await _movieSessionSeatRepository.UpdateAsync(movieSessionSeat, cancellationToken);

        return Result.Success();
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

    private async Task<Result> CheckSeatSaleAvailability(Guid movieSessionId,
        CancellationToken cancellationToken)
    {
        var movieSession = await _movieSessionsRepository.GetByIdAsync(movieSessionId, cancellationToken);

        if (movieSession is null)
            return DomainErrors<MovieSession>.NotFound(movieSessionId.ToString());

        if (movieSession.SalesTerminated)
            return DomainErrors<MovieSession>.ConflictException(
                $"{nameof(MovieSession)} has been terminated.");

        return Result.Success();
    }

    public Task<MovieSessionSeat> GetSeat(Guid movieSessionId, short seatRow, short seatNumber, CancellationToken cancellationToken)
    {
        return GetMovieSessionSeat(movieSessionId, seatRow, seatNumber, cancellationToken);
    }
}