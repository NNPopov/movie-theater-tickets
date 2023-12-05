using System.Security.Claims;
using CinemaTicketBooking.Api.Controllers;
using CinemaTicketBooking.Api.Endpoints.Common;
using CinemaTicketBooking.Application.Exceptions;
using CinemaTicketBooking.Application.ShoppingCarts.Command.AssingClientCart;
using CinemaTicketBooking.Application.ShoppingCarts.Command.CreateCart;
using CinemaTicketBooking.Application.ShoppingCarts.Command.PurchaseSeats;
using CinemaTicketBooking.Application.ShoppingCarts.Command.ReserveSeats;
using CinemaTicketBooking.Application.ShoppingCarts.Command.SelectSeats;
using CinemaTicketBooking.Application.ShoppingCarts.Command.UnreserveSeats;
using CinemaTicketBooking.Application.ShoppingCarts.Command.UnselectSeats;
using CinemaTicketBooking.Application.ShoppingCarts.Queries;
using CinemaTicketBooking.Domain.Error;
using CinemaTicketBooking.Domain.Exceptions;
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
                [FromHeader(Name = "X-Idempotency-Key")]
                string requestId,
                ClaimsPrincipal user,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                if (!Guid.TryParse(requestId, out Guid parsedRequestId))
                {
                    throw new Exception($"Incorrect requestId:{requestId} {nameof(CreateShoppingCartRequest)}");
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
        
        endpointRouteBuilder.MapGet($"{BaseRoute}/current", async (
                ClaimsPrincipal user,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var clientId = GetClientId(user);

                var command = new GetCurrentShoppingCartQuery(clientId);

                return await sender.Send(command, cancellationToken);
            })
            .WithName("current")
            .RequireAuthorization()
            .WithTags(Tag)
            .Produces(201)
            .Produces(204)
            .Produces(409);

        endpointRouteBuilder.MapPut($"{BaseRoute}/{{ShoppingCartId}}/assignclient", async (
                [FromRoute] Guid shoppingCartId,
                ClaimsPrincipal user,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var clientId = GetClientId(user);

                var assignClientCartCommand =
                    new AssignClientCartCommand(shoppingCartId, clientId);

                var result =  await sender.Send(assignClientCartCommand, cancellationToken);
                
                return result.Match(
                    () => Results.Ok(),
                    failure =>
                    {
                        if (failure is ConflictError)
                            throw new ConflictException(failure.Code, failure.Description);
                        if (failure is NotFountError)
                            throw new ContentNotFoundException(failure.Code, failure.Description);
                        
                        throw new Exception(failure.Description);
                    });
            })
            .WithName("AssignUser")
            .RequireAuthorization()
            .WithTags(Tag)
            .Produces(201)
            .Produces(204)
            .Produces(409);

        endpointRouteBuilder.MapPost($"{BaseRoute}/{{ShoppingCartId}}/seats/select", async (
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

                return result.Match(
                    () => Results.Ok(),
                    failure => Results.BadRequest(failure.Description));
            })
            .WithName("SelectSeat")
            .WithTags(Tag)
            .Produces(201)
            .Produces(204);

        endpointRouteBuilder.MapDelete($"{BaseRoute}/{{ShoppingCartId}}/seats/unselect", async (
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

        endpointRouteBuilder.MapPost($"{BaseRoute}/{{ShoppingCartId}}/reservations", async (
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

        endpointRouteBuilder.MapDelete($"{BaseRoute}/{{ShoppingCartId}}/unreserve", async (
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

        endpointRouteBuilder.MapPost($"{BaseRoute}/{{ShoppingCartId}}/purchase", async ([FromRoute] Guid shoppingCartId,
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


        endpointRouteBuilder.MapGet($"{BaseRoute}/{{ShoppingCartId}}",
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
            .Produces<ShoppingCartDto>(200, "application/json")
            .Produces(204);
    }

    private static Guid GetClientId(ClaimsPrincipal user)
    {
        var id = user.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")
            ?.Value;

        if (!Guid.TryParse(id, out Guid clientId))
        {
            throw new Exception($"Incorrect clientId:{clientId} {nameof(CreateShoppingCartRequest)}");
        }

        return clientId;
    }
}

public record ReserveSeatsRequest(short Row, short Number, Guid ShowtimeId);

public record UnselectSeatsRequest(short Row, short Number, Guid ShowtimeId);