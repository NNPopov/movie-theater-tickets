namespace CinemaTicketBooking.Api.Controllers;


public record ShoppingCartResponse(Guid shoppingCartId);

public record CreateShoppingCartRequest(short MaxNumberOfSeats);