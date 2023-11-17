using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using CinemaTicketBooking.Domain.Common;
using CinemaTicketBooking.Domain.Common.Ensure;
using CinemaTicketBooking.Domain.Common.Events;
using CinemaTicketBooking.Domain.Error;
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

    public Guid ClientId { get; private set; }

    public string HashId { get; private set; }

    public ShoppingCartStatus Status { get; private set; }

    public IReadOnlyList<SeatShoppingCart> Seats => _seats.AsReadOnly();

    [JsonConstructor]
    public ShoppingCart(Guid id,
        Guid movieSessionId,
        DateTime createdCard,
        short maxNumberOfSeats,
        SeatShoppingCart[]? seats,
        ShoppingCartStatus status,
        Guid clientId,
        string hashId) : base(id: id)
    {
        MovieSessionId = movieSessionId;

        CreatedCard = createdCard;
        MaxNumberOfSeats = maxNumberOfSeats;
        Status = status;
        _seats = seats == null ? default : seats.ToList();
        ClientId = clientId;
        HashId = hashId;
    }
    
    static string ComputeMD5(string s)
    {
        StringBuilder sb = new StringBuilder();
        
        using (MD5 md5 = MD5.Create())
        {
            byte[] hashValue = md5.ComputeHash(Encoding.UTF8.GetBytes(s));
            
            foreach (byte b in hashValue)
            {
                sb.Append($"{b:X2}");
            }
        }

        return sb.ToString();
    }

    public Result AssignClientId(Guid clientId)
    {
        Ensure.NotEmpty(clientId, "The clientId is required.", nameof(clientId));

        if (Status == ShoppingCartStatus.PurchaseCompleted)
            throw new ConflictException(nameof(ShoppingCart), Id.ToString());

        if (ClientId != Guid.Empty)
            throw new ConflictException(nameof(ShoppingCart), Id.ToString());

        ClientId = clientId;

        return Result.Success();
    }

    private ShoppingCart(Guid id, short maxNumberOfSeats) : base(id: id)
    {
        Ensure.NotEmpty(maxNumberOfSeats, "The maxNumberOfSeats is required.", nameof(maxNumberOfSeats));
        Ensure.NotEmpty(id, "The id is required.", nameof(id));

        HashId = ComputeMD5(id.ToString());
        CreatedCard = TimeProvider.System.GetUtcNow().DateTime;
        MaxNumberOfSeats = maxNumberOfSeats;
        Status = ShoppingCartStatus.InWork;
        ClientId = Guid.Empty;
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


    public void AddSeats(SeatShoppingCart seat, Guid movieSessionId)
    {
        Ensure.NotEmpty(MovieSessionId, "The MovieSessionId is required.", nameof(MovieSessionId));
        Ensure.NotEmpty(movieSessionId, "The movieSessionId is required.", nameof(movieSessionId));
        
        if (Status != ShoppingCartStatus.InWork)
            throw new ConflictException(nameof(ShoppingCart), Id.ToString());
        
        if (MovieSessionId != movieSessionId)
            throw new DomainValidationException($"The Seat does not belong to the cinema hall being processed.");

        if (_seats.Count() >= MaxNumberOfSeats)
        {
            throw new DomainValidationException($"Number of seats cannot be greater than {MaxNumberOfSeats}.");
        }
        
        if (_seats.Any(t => t.SeatRow == seat.SeatRow && t.SeatNumber == seat.SeatNumber))
        {
            throw new DomainValidationException($"Seat has already been added to cart movieSessionId:{movieSessionId}, SeatRow:{seat.SeatRow}, SeatNumber:{seat.SeatNumber}.");
        }

        _seats.Add(seat);
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

        Ensure.NotEmpty(ClientId, "The ClientId is required.", nameof(ClientId));

        if (Status == ShoppingCartStatus.SeatsReserved)
            Status = ShoppingCartStatus.PurchaseCompleted;
    }
}

[method: JsonConstructor]
public class SeatShoppingCart(short seatRow, short seatNumber) : Seat(seatRow, seatNumber);

public sealed record SeatRemovedFromShoppingCartDomainEvent(
    Guid MovieSessionId,
    short SeatRow,
    short SeatNumber,
    Guid ShoppingCartId
) : IDomainEvent;