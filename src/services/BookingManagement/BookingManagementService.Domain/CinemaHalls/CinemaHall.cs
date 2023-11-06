using CinemaTicketBooking.Domain.CinemaHalls.Events;
using CinemaTicketBooking.Domain.Common;

namespace CinemaTicketBooking.Domain.CinemaHalls;

public class CinemaHall : AggregateRoot
{
    private readonly IList<SeatEntity> _seats;

    private CinemaHall()
    {
    }

    private CinemaHall(Guid id, string name, string description, IList<SeatEntity> seats) : base(id)
    {
        Description = description;
        Name = name;
        _seats = seats;
    }

    public static CinemaHall Create(string name, string description, IList<(short Row, short SeatNumber)> seats)
    {
        var id = Guid.NewGuid();

        var auditoriumSeats = seats.Select(t => new SeatEntity
        {
            CinemaHallId = id,
            Row = t.Row,
            SeatNumber = t.SeatNumber
        }).ToList();

        var auditorium = new CinemaHall(
            id: id,
            name: name,
            description: description,
            seats: auditoriumSeats);

        auditorium.AddDomainEvent(new AuditoriumCreatedDomainEvent(auditorium));

        return auditorium;
    }

    public string Description { get; private set; }

    public string Name { get; private set; }

    public IReadOnlyCollection<SeatEntity> Seats => _seats.AsReadOnly();
}