using CinemaTicketBooking.Domain.Common.Events;
using CinemaTicketBooking.Domain.Constants;

namespace CinemaTicketBooking.Application.Common.Events;

public class BaseApplicationEvent<T>:  INotification
where T:IDomainEvent
{

    public BaseApplicationEvent(T appEvent)
    {
        Event = appEvent;
    }

    

    public T Event { get; set; }
}