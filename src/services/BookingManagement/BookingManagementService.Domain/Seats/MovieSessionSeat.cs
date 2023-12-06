using CinemaTicketBooking.Domain.Common;
using CinemaTicketBooking.Domain.Common.Ensure;
using CinemaTicketBooking.Domain.Common.Events;
using CinemaTicketBooking.Domain.Error;
using CinemaTicketBooking.Domain.Seats.Events;
using Newtonsoft.Json;

namespace CinemaTicketBooking.Domain.Seats;

public sealed class MovieSessionSeat : ValueObject, IAggregateRoot
{
    private MovieSessionSeat()
    {
        ShoppingCartHashId = string.Empty;
    }

    [JsonConstructor]
    private MovieSessionSeat(Guid movieSessionId,
        short seatNumber, 
        short seatRow,
        decimal price,
        SeatStatus status,
        Guid shoppingCartId,
        string shoppingCartHashId)
    {
        MovieSessionId = movieSessionId;
        SeatNumber = seatNumber;
        Status = status;
        Price = price;
        SeatRow = seatRow;
        ShoppingCartId = shoppingCartId;
        ShoppingCartHashId = shoppingCartHashId;
    }
    
    public static MovieSessionSeat Create(Guid movieSessionId, 
        short seatNumber,
        short seatRow,
        decimal price)
    {
        Ensure.NotEmpty(movieSessionId, "The movieSessionId is required.", nameof(movieSessionId));
        Ensure.NotEmpty(seatNumber, "The seatNumber is required.", nameof(seatNumber));
        Ensure.NotEmpty(price, "The price is required.", nameof(price));
        Ensure.NotEmpty(seatRow, "The seatRow is required.", nameof(seatRow));

        return new MovieSessionSeat(movieSessionId,
            seatNumber, seatRow,
            price,
            SeatStatus.Available,
            Guid.Empty,
            string.Empty);
    }



    public Guid MovieSessionId { get; }

    public short SeatNumber { get; }

    public short SeatRow { get; }

    public decimal Price { get; private set; }

    public SeatStatus Status { get; private set; }
    
    public Guid ShoppingCartId { get; private set; }
    
    public string ShoppingCartHashId { get; private set; }

    internal Result Select(Guid shoppingCartId, string hashId)
    {
        Ensure.NotEmpty(shoppingCartId, "The shoppingCartId is required.", nameof(shoppingCartId));
        Ensure.NotEmpty(hashId, "The shoppingCartHashId is required.", nameof(hashId));
        
        if (Status != SeatStatus.Available)
        {
            return DomainErrors<MovieSessionSeat>.ConflictException("Status should be Available");
        }
        
        if (shoppingCartId != ShoppingCartId && ShoppingCartId != Guid.Empty)
        {
            return DomainErrors<MovieSessionSeat>.InvalidOperation(
                "The place is already being processed by another shopping cart");
        }
        
        if (ShoppingCartId == Guid.Empty)
        {
            ShoppingCartId = shoppingCartId;
            ShoppingCartHashId = hashId;
        }

        var currentStatus = Status;

        Status = SeatStatus.Selected;
        
        var domainEvent = new MovieSessionSeatStatusUpdatedDomainEvent(this, currentStatus);
        
        AddDomainEvent(domainEvent);

        return Result.Success();
    }

    internal Result Reserve(Guid shoppingCartId)
    {
        Ensure.NotEmpty(shoppingCartId, "The shoppingCartId is required.", nameof(shoppingCartId));

        if (Status != SeatStatus.Selected && Status != SeatStatus.Available)
        {
           return DomainErrors<MovieSessionSeat>.ConflictException("Status should be selected or available.");
        }
        
        var currentStatus = Status;

        Status = SeatStatus.Reserved;
        
        var domainEvent = new MovieSessionSeatStatusUpdatedDomainEvent(this, currentStatus);
        
        AddDomainEvent(domainEvent);
        
        return Result.Success();
    }

    internal Result Sell(Guid shoppingCartId)
    {
        Ensure.NotEmpty(shoppingCartId, "The shoppingCartId is required.", nameof(shoppingCartId));

        if (ShoppingCartId != shoppingCartId)
        {
            return DomainErrors<MovieSessionSeat>.InvalidOperation(
                "The place is already being processed by another shopping cart");
        }
        
        if (Status == SeatStatus.Sold)
        {
            return DomainErrors<MovieSessionSeat>.ConflictException("Status should not be Sold.");
        }
        
        var currentStatus = Status;

        Status = SeatStatus.Sold;
        
        var domainEvent = new MovieSessionSeatStatusUpdatedDomainEvent(this, currentStatus);
        
        AddDomainEvent(domainEvent);
        
        return Result.Success();
    }

    internal Result ReturnToAvailable()
    {
        if (Status == SeatStatus.Sold)
        {
            return DomainErrors<MovieSessionSeat>.ConflictException("Status should not be Sold.");
        }
        
        var currentStatus = Status;

        Status = SeatStatus.Available;
        ShoppingCartId = Guid.Empty;
        ShoppingCartHashId = string.Empty;
        
        var domainEvent = new MovieSessionSeatStatusUpdatedDomainEvent(this, currentStatus);
        
        AddDomainEvent(domainEvent);
        
        return Result.Success();
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return MovieSessionId;

        yield return SeatNumber;

        yield return SeatRow;
    }

    public override string ToString()
    {
        return $"MovieSessionId:{MovieSessionId}, SeatRow:{SeatRow}, SeatNumber:{SeatNumber}, Status:{Status.ToString()}";
    }

    [JsonIgnore] private readonly List<IDomainEvent> _domainEvents = new List<IDomainEvent>();


    public IReadOnlyCollection<IDomainEvent> GetDomainEvents() => _domainEvents.AsReadOnly();

    /// <summary>
    /// Clears all the domain events from the <see cref="AggregateRoot"/>.
    /// </summary>
    public void ClearDomainEvents() => _domainEvents.Clear();
    
    /// <summary>
    /// Adds the specified <see cref="IDomainEvent"/> to the <see cref="AggregateRoot"/>.
    /// </summary>
    /// <param name="domainEvent">The domain event.</param>
    public void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
}