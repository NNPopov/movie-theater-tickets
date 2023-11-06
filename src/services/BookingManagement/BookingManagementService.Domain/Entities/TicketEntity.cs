using CinemaTicketBooking.Domain.CinemaHalls;
using CinemaTicketBooking.Domain.MovieSessions;

namespace CinemaTicketBooking.Domain.Entities
{
    public class TicketEntity
    {
        public TicketEntity()
        {
            CreatedTime =  TimeProvider.System.GetUtcNow().DateTime;
            Paid = false;
        }

        public Guid Id { get; set; }
        public Guid MovieSessionId { get; set; }
        public ICollection<ShowTimeSeatEntity> Seats { get; set; }
        public DateTime CreatedTime { get; set; }
        public bool Paid { get; set; }
        public MovieSession MovieSession { get; set; }
    }
}
