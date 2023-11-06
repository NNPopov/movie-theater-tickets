using CinemaTicketBooking.Domain.Common;
using CinemaTicketBooking.Domain.Common.Ensure;
using CinemaTicketBooking.Domain.Common.Events;
using CinemaTicketBooking.Domain.Exceptions;
using CinemaTicketBooking.Domain.Seats.Events;
using Newtonsoft.Json;

namespace CinemaTicketBooking.Domain.Seats;

public class MovieSessionSeat : ValueObject, IAggregateRoot
{
    private MovieSessionSeat()
    {
        HashId = string.Empty;
    }

    [JsonConstructor]
    private MovieSessionSeat(Guid movieSessionId,
        short seatNumber, 
        short seatRow,
        decimal price,
        SeatStatus status,
        Guid shoppingCartId,
        string hashId)
    {
        MovieSessionId = movieSessionId;
        SeatNumber = seatNumber;
        Status = status;
        Price = price;
        SeatRow = seatRow;
        ShoppingCartId = shoppingCartId;
        HashId = hashId;
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



    public Guid MovieSessionId { get; private set; }

    public short SeatNumber { get; private set; }

    public short SeatRow { get; private set; }

    public decimal Price { get; private set; }

    public SeatStatus Status { get; private set; }
    
    public Guid ShoppingCartId { get; private set; }
    
    public string HashId { get; private set; }

    public void Select(Guid shoppingCartId, string hashId)
    {
        Ensure.NotEmpty(shoppingCartId, "The shoppingCartId is required.", nameof(shoppingCartId));
        Ensure.NotEmpty(hashId, "The hashId is required.", nameof(hashId));
        
        if (Status != SeatStatus.Available)
        {
            throw new ConflictException(nameof(MovieSessionSeat), this.ToString());
        }
        
        if (shoppingCartId != ShoppingCartId && ShoppingCartId != Guid.Empty)
        {
            throw new InvalidOperationException("The seat is already selected by another shopping cart.");
        }
        
        if (ShoppingCartId == Guid.Empty)
        {
            ShoppingCartId = shoppingCartId;
            HashId = hashId;
        }
        

        var currentStatus = Status;

        Status = SeatStatus.Selected;
        
        var domainEvent = new MovieSessionSeatStatusUpdatedEvent(this, currentStatus);
        
        AddDomainEvent(domainEvent);
        
    }



    public void Reserve(Guid shoppingCartId)
    {
        Ensure.NotEmpty(shoppingCartId, "The shoppingCartId is required.", nameof(shoppingCartId));

        if (Status != SeatStatus.Selected && Status != SeatStatus.Available) // || ShoppingCartId != shoppingCartId)
        {
            throw new ConflictException(nameof(MovieSessionSeat), this.ToString());
        }
        
        var currentStatus = Status;

        Status = SeatStatus.Reserved;
        
        var domainEvent = new MovieSessionSeatStatusUpdatedEvent(this, currentStatus);
        
        AddDomainEvent(domainEvent);
    }

    public void Sel(Guid shoppingCartId)
    {
        Ensure.NotEmpty(shoppingCartId, "The shoppingCartId is required.", nameof(shoppingCartId));

        if (ShoppingCartId != shoppingCartId)
        {
            throw new InvalidOperationException("The seat is already selected by another shopping cart.");
        }
        
        if (Status == SeatStatus.Sold)
        {
            throw new ConflictException(nameof(MovieSessionSeat), this.ToString());
        }
        
        var currentStatus = Status;

        Status = SeatStatus.Sold;
        
        var domainEvent = new MovieSessionSeatStatusUpdatedEvent(this, currentStatus);
        
        AddDomainEvent(domainEvent);
    }

    public void ReturnToAvailable()
    {
        if (Status == SeatStatus.Sold)
        {
            throw new ConflictException(nameof(MovieSessionSeat), this.ToString());
        }
        
        var currentStatus = Status;

        Status = SeatStatus.Available;
        ShoppingCartId = Guid.Empty;
        HashId = string.Empty;
        
        var domainEvent = new MovieSessionSeatStatusUpdatedEvent(this, currentStatus);
        
        AddDomainEvent(domainEvent);
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

    [JsonIgnore]
    protected readonly List<IDomainEvent> _domainEvents = new List<IDomainEvent>();


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