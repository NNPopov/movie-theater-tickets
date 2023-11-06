using CinemaTicketBooking.Application.ShoppingCarts.Command.CreateCart;

namespace CinemaTicketBooking.Application.ShoppingCarts.Command.AssingClientCart;

public class AssignClientCartCommandValidator: AbstractValidator<AssignClientCartCommand>
{
    public AssignClientCartCommandValidator()
    {
        RuleFor(v => v.ClientId)
            .NotEmpty();
        
        RuleFor(v => v.ShoppingCartId)
            .NotEmpty();
    }
}