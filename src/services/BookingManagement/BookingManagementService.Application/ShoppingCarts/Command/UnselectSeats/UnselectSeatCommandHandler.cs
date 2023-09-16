using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Application.Exceptions;
using CinemaTicketBooking.Domain.MovieSessions;
using CinemaTicketBooking.Domain.ShoppingCarts;

namespace CinemaTicketBooking.Application.ShoppingCarts.Command.UnselectSeats;
public record UnselectSeatCommand(Guid MovieSessionId, short SeatRow, short SeatNumber, Guid ShoppingCartId) : IRequest<bool>;


public class UnselectSeatCommandHandler : IRequestHandler<UnselectSeatCommand, bool>
{
    private IMovieSessionsRepository _movieSessionsRepository;
    private IShoppingCartRepository _shoppingCartRepository;

    private ISeatStateRepository _seatStateRepository;
    
    private readonly IPublisher _publisher;
    
   // private readonly IMovieSessionSeatRepository _movieSessionSeatRepository;

    public UnselectSeatCommandHandler(
        IMovieSessionsRepository movieSessionsRepository,
        ISeatStateRepository seatStateRepository,
        //IMovieSessionSeatRepository movieSessionSeatRepository,
        IShoppingCartRepository shoppingCartRepository,
        IPublisher publisher)
    {
        _movieSessionsRepository = movieSessionsRepository;
        _seatStateRepository = seatStateRepository;
        //_movieSessionSeatRepository = movieSessionSeatRepository;
        _shoppingCartRepository = shoppingCartRepository;
        _publisher = publisher;
    }

    public async Task<bool> Handle(UnselectSeatCommand request,
        CancellationToken cancellationToken)
    {
        var movieSession = await _movieSessionsRepository
            .GetWithTicketsByIdAsync(
                request.MovieSessionId, cancellationToken);

        if (movieSession is null)
            throw new ContentNotFoundException(request.MovieSessionId.ToString(), nameof(MovieSession));
        
        //Step 1: Remove seat from cart

        var cart = await _shoppingCartRepository.TryGetCart(request.ShoppingCartId);
        
        if (cart is null)
            throw new ContentNotFoundException(request.ShoppingCartId.ToString(), nameof(ShoppingCart));

        if (cart.MovieSessionId != request.MovieSessionId)
        {
            throw new ContentNotFoundException(request.ShoppingCartId.ToString(), nameof(ShoppingCart));
        }
        cart.TryRemoveSeats(new SeatShoppingCart(request.SeatRow, request.SeatNumber));
        
                
        await _shoppingCartRepository.TrySetCart(cart);
        
        //Step 2: Remove teptorary select

        var reservedInRedis = await _seatStateRepository.GetAsync(request.MovieSessionId,request.SeatRow,request.SeatNumber);

        if (reservedInRedis is not null)
        {
            await _seatStateRepository.DeleteAsync(request.MovieSessionId,request.SeatRow,request.SeatNumber);
        }
        
        //Step 3: return seat back to store 
        
        
        // await _publisher.Publish(new MovieSessionSeatExpiredSelectionEvent(
        //     MovieSessionId: request.MovieSessionId,
        //     SeatRow:request.SeatRow,
        //     SeatNumber: request.SeatRow,
        //     ShoppingKartId: cart.Id), cancellationToken);
        
        
        // var movieSessionSeat =
        //     await _movieSessionSeatRepository.GetByIdAsync(request.MovieSessionId, request.SeatRow, request.SeatNumber, cancellationToken);
        //
        // if (movieSessionSeat is null)
        //     throw new Exception();
        //
        // var result = movieSessionSeat.TryReturnToAvailable();
        //
        // if (!result)
        // {
        //     return false;
        // }
        //
        // await _movieSessionSeatRepository.UpdateAsync(movieSessionSeat, cancellationToken);

        return true;
    }
}
