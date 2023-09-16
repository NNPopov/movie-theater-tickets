namespace CinemaTicketBooking.Application.ShoppingCarts.Command.SelectSeats;

public class SelectSeatCommandValidator: AbstractValidator<SelectSeatCommand>
{
    public SelectSeatCommandValidator()
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