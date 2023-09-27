using CinemaTicketBooking.Application.ShoppingCarts.Command.SelectSeats;

namespace CinemaTicketBooking.Application.ShoppingCarts.Command.UnselectSeats;

public class UnselectSeatCommandValidator: AbstractValidator<SelectSeatCommand>
{
    public UnselectSeatCommandValidator()
    {
        RuleFor(v => v.MovieSessionId)
            .NotEmpty()
            .Must(v => v != Guid.Empty);
        
        RuleFor(v => v.SeatRow)
            .NotEmpty()
            .Must(x => x > 0);
        
        RuleFor(v => v.SeatNumber)
            .NotEmpty()
            .Must(x => x > 0);
        
        RuleFor(v => v.ShoppingCartId)
            .NotEmpty()
            .Must(v => v != Guid.Empty);
    }
}