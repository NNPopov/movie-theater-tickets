namespace CinemaTicketBooking.Application.Common;

public abstract record IdempotentRequest(Guid RequestId);