namespace CinemaTicketBooking.Application.ShoppingCarts.Command.ReserveSeats;

public class ReserveTicketsCommandValidator : AbstractValidator<ReserveTicketsCommand>
{
    public ReserveTicketsCommandValidator()
    {
        RuleFor(v => v.ShoppingCartId)
            .NotEmpty()
            .Must(v => v != Guid.Empty);
    }
}