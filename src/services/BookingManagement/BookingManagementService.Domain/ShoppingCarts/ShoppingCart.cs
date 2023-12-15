using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using CinemaTicketBooking.Domain.Common;
using CinemaTicketBooking.Domain.Common.Ensure;
using CinemaTicketBooking.Domain.Error;
using CinemaTicketBooking.Domain.Exceptions;
using CinemaTicketBooking.Domain.MovieSessions;
using CinemaTicketBooking.Domain.Shared;
using CinemaTicketBooking.Domain.ShoppingCarts.Abstractions;
using CinemaTicketBooking.Domain.ShoppingCarts.Events;
using Newtonsoft.Json;

namespace CinemaTicketBooking.Domain.ShoppingCarts;

public class ShoppingCart : AggregateRoot
{
    private List<SeatShoppingCart> _seats = new();

    public short MaxNumberOfSeats { get; }

    public DateTime CreatedAt { get; private set; }

    public Guid MovieSessionId { get; private set; }

    public Guid ClientId { get; private set; }

    public string HashId { get; private set; }

    public ShoppingCartStatus Status { get; private set; }

    public PriceCalculationResult PriceCalculationResult { get; private set; }

    public IReadOnlyList<SeatShoppingCart> Seats => _seats.AsReadOnly();

    [JsonConstructor]
    private ShoppingCart(Guid id,
        Guid movieSessionId,
        DateTime createdAt,
        short maxNumberOfSeats,
        SeatShoppingCart[]? seats,
        ShoppingCartStatus status,
        Guid clientId,
        string hashId,
        PriceCalculationResult priceCalculationResult) : base(id: id)
    {
        MovieSessionId = movieSessionId;

        CreatedAt = createdAt;
        MaxNumberOfSeats = maxNumberOfSeats;
        Status = status;
        _seats = seats == null ? default : seats.ToList();
        ClientId = clientId;
        HashId = hashId;
        PriceCalculationResult = priceCalculationResult;
    }



    public Result AssignClientId(Guid clientId)
    {
        Ensure.NotEmpty(clientId, "The clientId is required.", nameof(clientId));

        EnsurePurchaseIsNotCompleted();

        if (ClientId != Guid.Empty)
            throw new ConflictException(nameof(ShoppingCart), Id.ToString());

        ClientId = clientId;

        _domainEvents.Add(new ShoppingCartAssignedToClientDomainEvent(Id));

        return Result.Success();
    }

    private ShoppingCart(Guid id, short maxNumberOfSeats, IDataHasher dataHasher) : base(id: id)
    {
        Ensure.NotEmpty(maxNumberOfSeats, "The maxNumberOfSeats is required.", nameof(maxNumberOfSeats));
        Ensure.NotEmpty(id, "The id is required.", nameof(id));

        HashId = dataHasher.ComputeHash(id.ToString());
        CreatedAt = TimeProvider.System.GetUtcNow().DateTime;
        MaxNumberOfSeats = maxNumberOfSeats;
        Status = ShoppingCartStatus.InWork;
        ClientId = Guid.Empty;

        _domainEvents.Add(new ShoppingCartCreatedDomainEvent(id));
    }

    public void SetShowTime(Guid showTimeId)
    {
        Ensure.NotEmpty(showTimeId, "The movieSessionId is required.", nameof(showTimeId));

        EnsurePurchaseIsNotCompleted();

        if (MovieSessionId != showTimeId)
        {
            MovieSessionId = showTimeId;

            ClearSeats();
        }
    }

    public static ShoppingCart Create(short maxNumberOfSeats, IDataHasher dataHasher)
    {
        return new(Guid.NewGuid(), maxNumberOfSeats, dataHasher);
    }


    public void AddSeats(SeatShoppingCart seat, Guid movieSessionId)
    {
        Ensure.NotEmpty(MovieSessionId, "The MovieSessionId is required.", nameof(MovieSessionId));
        Ensure.NotEmpty(movieSessionId, "The movieSessionId is required.", nameof(movieSessionId));

        if (Status != ShoppingCartStatus.InWork)
        {
            throw new ConflictException(nameof(ShoppingCart), Id.ToString());
        }

        if (MovieSessionId != movieSessionId)
        {
            throw new DomainValidationException($"The Seat does not belong to the cinema hall being processed.");
        }

        if (_seats.Count() >= MaxNumberOfSeats)
        {
            throw new DomainValidationException($"Number of seats cannot be greater than {MaxNumberOfSeats}.");
        }

        if (_seats.Any(t => t.SeatRow == seat.SeatRow && t.SeatNumber == seat.SeatNumber))
        {
            throw new DomainValidationException(
                $"Seat has already been added to cart movieSessionId:{movieSessionId}, SeatRow:{seat.SeatRow}, SeatNumber:{seat.SeatNumber}.");
        }

        _domainEvents.Add(new SeatAddedToShoppingCartDomainEvent(MovieSessionId,
            seat.SeatRow, seat.SeatNumber, Id));

        _seats.Add(seat);
    }

    public bool TryRemoveSeats(short seatRow, short seatNumber)
    {
        Ensure.NotEmpty(MovieSessionId, "The MovieSessionId is required.", nameof(MovieSessionId));

        EnsurePurchaseIsNotCompleted();

        var seat = _seats.FirstOrDefault(t => t.SeatRow == seatRow && t.SeatNumber == seatNumber);

        if (seat is null)
            return false;

        _seats.Remove(seat);

        _domainEvents.Add(new SeatRemovedFromShoppingCartDomainEvent(MovieSessionId,
            seatRow, seatNumber, Id));

        return true;
    }

    public void ClearCart()
    {
        EnsurePurchaseIsNotCompleted();

        Status = ShoppingCartStatus.InWork;
        MovieSessionId = Guid.Empty;

        foreach (var seat in _seats)
        {
            _domainEvents.Add(new SeatRemovedFromShoppingCartDomainEvent(MovieSessionId,
                seat.SeatRow, seat.SeatNumber, Id));
        }

        ClearSeats();
        
        _domainEvents.Add(new ShoppingCartCleanedDomainEvent(Id));
    }

    public void SeatsReserve()
    {
        EnsurePurchaseIsNotCompleted();

        if (Status == ShoppingCartStatus.InWork)
            Status = ShoppingCartStatus.SeatsReserved;
        
        _domainEvents.Add(new ShoppingCartReservedDomainEvent(Id));
    }

    public void PurchaseComplete()
    {
        EnsurePurchaseIsNotCompleted();

        Ensure.NotEmpty(ClientId, "The ClientId is required.", nameof(ClientId));

        if (Status == ShoppingCartStatus.SeatsReserved)
            Status = ShoppingCartStatus.PurchaseCompleted;
        
        _domainEvents.Add(new ShoppingCartPurchaseDomainEvent(Id));
    }

    public PriceCalculationResult CalculateCartAmount(IPriceService priceService)
    {
        if (MovieSessionId == Guid.Empty)
        {
            return priceService.GetCartAmount(_seats);
        }

        EnsurePurchaseIsNotCompleted();

        PriceCalculationResult = priceService.GetCartAmount(_seats);

        return PriceCalculationResult;
    }


    public void Delete()
    {
        Status = ShoppingCartStatus.Deleted;

        _domainEvents.Add(new ShoppingCartDeletedDomainEvent(this));
    }
    
    private void ClearSeats()
    {
        _seats = new List<SeatShoppingCart>();
    }
    
    
    private void EnsurePurchaseIsNotCompleted()
    {
        if (Status == ShoppingCartStatus.PurchaseCompleted)
        {
            throw new ConflictException(nameof(ShoppingCart), Id.ToString());
        }
    }
}

[method: JsonConstructor]
public class SeatShoppingCart(short seatRow, short seatNumber, decimal price, DateTime? selectionExpirationTime = null)
    : Seat(seatRow, seatNumber)
{
    public decimal Price { get; private set; } = price;
    public DateTime? SelectionExpirationTime { get; private set; } = selectionExpirationTime;
};
