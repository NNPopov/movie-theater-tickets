namespace CinemaTicketBooking.Application.ShoppingCarts.Command.PurchaseSeats;

public class PurchaseTicketsCommandValidator : AbstractValidator<PurchaseTicketsCommand>
{
    public PurchaseTicketsCommandValidator()
    {
        RuleFor(v => v.ShoppingCartId)
            .NotEmpty()
            .Must(v => v != Guid.Empty);
    }
}