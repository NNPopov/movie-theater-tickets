using System.ComponentModel.DataAnnotations;
using CinemaTicketBooking.Domain.Common;
using CinemaTicketBooking.Domain.Common.Ensure;
using CinemaTicketBooking.Domain.Common.Events;
using CinemaTicketBooking.Domain.Exceptions;
using CinemaTicketBooking.Domain.MovieSessions;
using Newtonsoft.Json;

namespace CinemaTicketBooking.Domain.ShoppingCarts;

public class ShoppingCart : AggregateRoot
{
    private List<SeatShoppingCart> _seats = new();

    public short MaxNumberOfSeats { get; private set; }

    public DateTime CreatedCard { get; private set; }

    public Guid MovieSessionId { get; private set; }

    public ShoppingCartStatus Status { get; private set; }

    public IReadOnlyList<SeatShoppingCart> Seats => _seats.AsReadOnly();

    [JsonConstructor]
    public ShoppingCart(Guid id,
        Guid? movieSessionId,
        DateTime createdCard,
        short maxNumberOfSeats,
        SeatShoppingCart[]? seats,
        ShoppingCartStatus status) : base(id: id)
    {
        MovieSessionId = movieSessionId ?? Guid.Empty;
        CreatedCard = createdCard;
        MaxNumberOfSeats = maxNumberOfSeats;
        Status = status;
        _seats = seats == null ? default : seats.ToList();
    }

    private ShoppingCart(Guid id, short maxNumberOfSeats) : base(id: id)
    {
        Ensure.NotEmpty(maxNumberOfSeats, "The maxNumberOfSeats is required.", nameof(maxNumberOfSeats));
        Ensure.NotEmpty(id, "The id is required.", nameof(id));

        CreatedCard = TimeProvider.System.GetUtcNow().DateTime;
        MaxNumberOfSeats = maxNumberOfSeats;
        Status = ShoppingCartStatus.InWork;
    }

    public void SetShowTime(Guid showTimeId)
    {
        Ensure.NotEmpty(showTimeId, "The movieSessionId is required.", nameof(showTimeId));

        if (Status == ShoppingCartStatus.PurchaseCompleted)
            throw new ConflictException(nameof(ShoppingCart), Id.ToString());

        if (MovieSessionId != showTimeId)
        {
            MovieSessionId = showTimeId;

            _seats = new List<SeatShoppingCart>();
        }
    }

    public static ShoppingCart Create(short maxNumberOfSeats)
    {
        return new(Guid.NewGuid(), maxNumberOfSeats);
    }


    public void AddSeats(SeatShoppingCart seat)
    {
        Ensure.NotEmpty(MovieSessionId, "The MovieSessionId is required.", nameof(MovieSessionId));

        if (Status != ShoppingCartStatus.InWork)
            throw new ConflictException(nameof(ShoppingCart), Id.ToString());

        if ((_seats.Count() + 1) <= MaxNumberOfSeats)
        {
            if (!_seats.Any(t => t.SeatRow == seat.SeatRow && t.SeatNumber == seat.SeatNumber))
                _seats.Add(seat);
        }
    }

    public bool TryRemoveSeats(SeatShoppingCart seat)
    {
        Ensure.NotEmpty(MovieSessionId, "The MovieSessionId is required.", nameof(MovieSessionId));

        if (Status == ShoppingCartStatus.PurchaseCompleted)
            throw new ConflictException(nameof(ShoppingCart), Id.ToString());

        if (!_seats.Exists(t => t == seat))
            return false;

        _seats.Remove(seat);

        _domainEvents.Add(new SeatRemovedFromShoppingCartDomainEvent(MovieSessionId,
            seat.SeatRow, seat.SeatNumber, Id));

        return true;
    }

    public void ClearCart()
    {
        if (Status == ShoppingCartStatus.PurchaseCompleted)
            throw new ConflictException(nameof(ShoppingCart), Id.ToString());

        Status = ShoppingCartStatus.InWork;
        MovieSessionId = Guid.Empty;

        foreach (var seat in _seats)
        {
            _domainEvents.Add(new SeatRemovedFromShoppingCartDomainEvent(MovieSessionId,
                seat.SeatRow, seat.SeatNumber, Id));
        }

        _seats = new List<SeatShoppingCart>();
    }

    public void SeatsReserve()
    {
        if (Status == ShoppingCartStatus.PurchaseCompleted)
            throw new ConflictException(nameof(ShoppingCart), Id.ToString());

        if (Status == ShoppingCartStatus.InWork)
            Status = ShoppingCartStatus.SeatsReserved;
    }

    public void PurchaseComplete()
    {
        if (Status == ShoppingCartStatus.PurchaseCompleted)
            throw new ConflictException(nameof(ShoppingCart), Id.ToString());

        if (Status == ShoppingCartStatus.SeatsReserved)
            Status = ShoppingCartStatus.PurchaseCompleted;
    }
}



[method: JsonConstructor]
public class SeatShoppingCart(short seatRow, short seatNumber) : Seat(seatRow, seatNumber);


public enum ShoppingCartStatus
{
    InWork = 0,
    SeatsReserved = 1,
    PurchaseCompleted = 2
}

public record SeatRemovedFromShoppingCartDomainEvent(
    Guid MovieSessionId,
    short SeatRow,
    short SeatNumber,
    Guid ShoppingCartId
) : IDomainEvent;