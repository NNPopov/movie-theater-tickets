namespace CinemaTicketBooking.Application.ShoppingCarts.Command.CreateCart;

public class CreateShoppingCartCommandValidator: AbstractValidator<CreateShoppingCartCommand>
{
    public CreateShoppingCartCommandValidator()
    {
       
        RuleFor(v => v.MaxNumberOfSeats)
            .NotEmpty()
            .Must(x => x is > 0 and < 5);
    }
}