using CinemaTicketBooking.Domain.Common;

namespace CinemaTicketBooking.Domain.CinemaHalls;
    public class SeatEntity: ValueObject
    {
        public short Row { get; set; }
        public short SeatNumber { get; set; }
        public Guid CinemaHallId { get; set; }
        
        //public CinemaHall CinemaHall { get; set; }
        public override IEnumerable<object> GetEqualityComponents()
        {
            yield return CinemaHallId;
            yield return Row;
            yield return SeatNumber;
        }
    }

public class ShowTimeSeatEntity: ValueObject
{
    public short Row { get; set; }
    public short SeatNumber { get; set; }
    public Guid CinemaHallId { get; set; }
    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return CinemaHallId;
        yield return Row;
        yield return SeatNumber;
    }
}

