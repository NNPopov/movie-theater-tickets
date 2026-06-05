# Entry points: Minimal API

How HTTP endpoints are written. Read this when adding or changing an endpoint.

## The `IEndpoints` convention

Endpoints are grouped per resource in a class implementing `IEndpoints` (see
`API/Endpoints/Common/IEndpoints.cs` and `EndpointExtensions.cs`). The host
discovers these and calls `DefineEndpoints` at startup.

```csharp
// API/Endpoints/ShoppingCartEndpointApplicationBuilderExtensions.cs
public class ShoppingCartEndpointApplicationBuilderExtensions : IEndpoints
{
    private static readonly string Tag = "ShoppingCart";
    private static readonly string BaseRoute = "api/shoppingcarts";

    public static void DefineEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost($"{BaseRoute}/{{ShoppingCartId}}/seats/select", async (
                [FromRoute] Guid shoppingCartId,
                [FromBody] ReserveSeatsRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var command = new SelectSeatCommand(
                    MovieSessionId: request.ShowtimeId,
                    SeatRow: request.Row,
                    SeatNumber: request.Number,
                    ShoppingCartId: shoppingCartId);

                var result = await sender.Send(command, cancellationToken);

                return result.Match(
                    () => Results.Ok(),
                    failure => Results.BadRequest(failure.Description));
            })
            .WithName("SelectSeat")
            .WithTags(Tag)
            .Produces(200)
            .Produces(204);
    }
}
```

## Rules for an endpoint delegate

- **It contains no business logic.** It (1) binds the request, (2) builds the
  command/query, (3) `await sender.Send(...)`, (4) shapes the HTTP result.
- **Inject via parameters**, not a constructor: `ISender`/`IMediator`,
  `ClaimsPrincipal`, `CancellationToken`, and `[FromBody]`/`[FromRoute]`/
  `[FromQuery]`/`[FromHeader]` bound values.
- **Always thread `CancellationToken`** into `Send`.
- **Request models are records** defined near the endpoint (or in `API/Models`),
  distinct from the MediatR command. The endpoint maps request → command. Never bind
  a request body straight onto a domain type.
- **Do not catch exceptions.** Domain/application exceptions propagate to
  `CustomExceptionHandler`. The endpoint only deals with the success path (and, for
  `Result`-returning handlers, the failure branch of `.Match`).
- **Result handlers:** call `.Match(onSuccess, onFailure)`. For aggregates that
  bridge `Result`→exception, the failure branch may re-throw a typed exception so the
  central handler renders it — keep this consistent with sibling endpoints (see
  `error_handling.md`).
- **Metadata:** `.WithName(...)` (used for `CreatedAtRoute` links), `.WithTags(Tag)`,
  and `.Produces<TResponse>(status, "application/json")` / `.Produces(status)` for
  every status the endpoint can return. `.RequireAuthorization()` for protected
  endpoints.
- **Identity:** read claims from `ClaimsPrincipal` (see the `GetClientId` helper
  pattern), not from headers, for the authenticated user.
- **Idempotency:** for create/mutating endpoints that use it, accept
  `[FromHeader(Name = "X-Idempotency-Key")] string requestId`, parse it to a `Guid`,
  and pass it into the command; the `IdempotentCommandPipelineBehaviour` enforces
  replay handling (`DuplicateRequestException` → 200).

## Status codes

The endpoint chooses success codes (`Results.Ok`, `Results.CreatedAtRoute`,
`Results.NoContent`). **Failure codes come from `CustomExceptionHandler`**, driven by
the exception type the handler raised — do not encode failure statuses in the
endpoint except via the `Result` failure branch. To add a new failure status, add a
mapping in `CustomExceptionHandler` (see `error_handling.md`).

## Registering a new endpoint group

Add a new `IEndpoints` class for the resource; the discovery mechanism wires it in.
Adding a class is a normal feature change. Changing the discovery/registration
mechanism itself (`EndpointExtensions`) is a stable-infrastructure change — see
`stable_vs_feature.md`.
