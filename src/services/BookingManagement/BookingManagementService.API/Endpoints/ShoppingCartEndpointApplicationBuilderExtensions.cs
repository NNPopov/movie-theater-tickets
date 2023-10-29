using System.Security.Claims;
using CinemaTicketBooking.Api.Controllers;
using CinemaTicketBooking.Api.Endpoints.Common;
using CinemaTicketBooking.Application.ShoppingCarts.Command.AssingClientCart;
using CinemaTicketBooking.Application.ShoppingCarts.Command.CreateCart;
using CinemaTicketBooking.Application.ShoppingCarts.Command.PurchaseSeats;
using CinemaTicketBooking.Application.ShoppingCarts.Command.ReserveSeats;
using CinemaTicketBooking.Application.ShoppingCarts.Command.SelectSeats;
using CinemaTicketBooking.Application.ShoppingCarts.Command.UnreserveSeats;
using CinemaTicketBooking.Application.ShoppingCarts.Command.UnselectSeats;
using CinemaTicketBooking.Application.ShoppingCarts.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CinemaTicketBooking.Api.Endpoints;

public class ShoppingCartEndpointApplicationBuilderExtensions : IEndpoints
{
    private static readonly string Tag = "ShoppingCart";
    private static readonly string BaseRoute = "api/shoppingcarts";

    public static void DefineEndpoints(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder.MapPost($"{BaseRoute}", async (
                [FromBody] CreateShoppingCartRequest request,
                [FromHeader(Name = "X-Idempotency-Key")] string requestId,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                if (!Guid.TryParse(requestId, out Guid parsedRequestId))
                {
                    throw new Exception($"Incorrect requestId:{ requestId} {nameof(CreateShoppingCartRequest)}");
                }
                
                var command = new CreateShoppingCartCommand(request.MaxNumberOfSeats, parsedRequestId);

                var result = await sender.Send(command, cancellationToken);
                
                return Results.CreatedAtRoute(
                    routeName: "GetShoppingCartById",
                    routeValues: new { shoppingCartId = result.ShoppingCartId.ToString() },
                    value: result);
            })
            .WithName("CreateShoppingCart")
            .WithTags(Tag)
            .Produces<CreateShoppingCartResponse>(201, "application/json")
            .Produces(204);

        endpointRouteBuilder.MapPut($"{BaseRoute}/{{shoppingCartId}}/assignclient", async (
                [FromRoute] Guid shoppingCartId,
                ClaimsPrincipal user ,
                [FromHeader(Name = "X-Idempotency-Key")] string requestId,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                
                var id =  user.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
                
                if (!Guid.TryParse(id, out Guid clientId))
                {
                    throw new Exception($"Incorrect requestId:{ requestId} {nameof(CreateShoppingCartRequest)}");
                }
                
                if (!Guid.TryParse(requestId, out Guid parsedRequestId))
                {
                    throw new Exception($"Incorrect requestId:{ requestId} {nameof(CreateShoppingCartRequest)}");
                }
                
                var command = new AssignClientCartCommand(shoppingCartId, clientId, parsedRequestId);
                
                var result = await sender.Send(command, cancellationToken);
                
                return Results.Ok(result);
            })
            .WithName("AssignUser")
            .RequireAuthorization()
            .WithTags(Tag)
            .Produces<CreateShoppingCartResponse>(201, "application/json")
            .Produces(204);

        endpointRouteBuilder.MapPost($"{BaseRoute}/{{shoppingCartId}}/seats/select", async (
                [FromRoute] Guid shoppingCartId,
                [FromBody] ReserveSeatsRequest reserveSeatsRequest,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var query = new SelectSeatCommand(MovieSessionId: reserveSeatsRequest.ShowtimeId,
                    SeatRow: reserveSeatsRequest.Row,
                    SeatNumber: reserveSeatsRequest.Number,
                    ShoppingCartId: shoppingCartId);
                var result = await sender.Send(query, cancellationToken);

                return result;
            })
            .WithName("SelectSeat")
            .WithTags(Tag)
            .Produces<bool>(201, "application/json")
            .Produces(204);

        endpointRouteBuilder.MapDelete($"{BaseRoute}/{{shoppingCartId}}/seats/unselect", async (
                [FromRoute] Guid shoppingCartId,
                [FromBody] UnselectSeatsRequest reserveSeatsRequest,
                [FromServices] ISender sender,
                CancellationToken cancellationToken) =>
            {
                var query = new UnselectSeatCommand(MovieSessionId: reserveSeatsRequest.ShowtimeId,
                    SeatRow: reserveSeatsRequest.Row,
                    SeatNumber: reserveSeatsRequest.Number,
                    ShoppingCartId: shoppingCartId);
                await sender.Send(query, cancellationToken);
            })
            .WithName("UnselectSeat")
            .WithTags(Tag)
            .Produces<bool>(201, "application/json")
            .Produces(204);

        endpointRouteBuilder.MapPost($"{BaseRoute}/{{shoppingCartId}}/reservations", async (
                [FromRoute] Guid shoppingCartId,
                [FromServices] ISender sender,
                CancellationToken cancellationToken) =>
            {
                var query = new ReserveTicketsCommand(ShoppingCartId: shoppingCartId);
                var result = await sender.Send(query, cancellationToken);

                return result;
            })
            .WithName("ReserveSeats")
            .WithTags(Tag)
            .Produces<bool>(201, "application/json")
            .Produces(204);

        endpointRouteBuilder.MapDelete($"{BaseRoute}/{{shoppingCartId}}/unreserve", async (
                [FromRoute] Guid shoppingCartId,
                [FromHeader(Name = "X-Idempotency-Key")]
                string requestId,
                [FromServices] ISender sender,
                CancellationToken cancellationToken) =>
            {
                if (!Guid.TryParse(requestId, out Guid parsedRequestId))
                {
                    return Results.BadRequest();
                }

                var query = new UnreserveSeatsCommand(ShoppingCartId: shoppingCartId, RequestId: parsedRequestId);
                await sender.Send(query, cancellationToken);

                return Results.Ok();
            })
            .WithName("UnreserveSeats")
            .WithTags(Tag)
            .Produces<bool>(200, "application/json")
            .Produces(204);

        endpointRouteBuilder.MapPost($"{BaseRoute}/{{shoppingCartId}}/purchase", async ([FromRoute] Guid shoppingCartId,
                [FromServices] ISender sender,
                CancellationToken cancellationToken) =>
            {
                var query = new PurchaseTicketsCommand(ShoppingCartId: shoppingCartId);
                var result = await sender.Send(query, cancellationToken);

                return result;
            })
            .WithName("PurchaseSeats")
            .WithTags(Tag)
            .Produces<bool>(201, "application/json")
            .Produces(204);


        endpointRouteBuilder.MapGet($"{BaseRoute}/{{shoppingCartId}}",
                async ([FromRoute] Guid shoppingCartId,
                    [FromServices] IMediator mediator,
                    CancellationToken cancellationToken) =>
                {
                    var command = new GetShoppingCartQuery(ShoppingCartId: shoppingCartId);
                    var cart = await mediator.Send(command, cancellationToken);
                    return cart;
                })
            .WithName("GetShoppingCartById")
            .WithTags(Tag)
            .Produces<ShoppingCartDto>(201, "application/json")
            .Produces(200);
    }
}

public record ReserveSeatsRequest(short Row, short Number, Guid ShowtimeId);

public record UnselectSeatsRequest(short Row, short Number, Guid ShowtimeId);