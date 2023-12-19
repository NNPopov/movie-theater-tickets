using AutoMapper;
using CinemaTicketBooking.Api.Endpoints.Common;
using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Application.MovieSessions.Commands.CreateShowtime;
using CinemaTicketBooking.Application.MovieSessions.DTOs;
using CinemaTicketBooking.Application.MovieSessions.Queries;
using CinemaTicketBooking.Application.MovieSessionSeats;
using CinemaTicketBooking.Application.ShoppingCarts.Command.SelectSeats;
using CinemaTicketBooking.Domain.MovieSessions;
using CinemaTicketBooking.Domain.MovieSessions.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CinemaTicketBooking.Api.Endpoints;

public class MovieSessionEndpointApplicationBuilderExtensions : IEndpoints
{
    private static readonly string Tag = "MovieSessions";
    private static readonly string BaseRoute = "api/moviesessions";


    public static void DefineEndpoints(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder.MapGet($"{BaseRoute}/activemovies",
                async (ISender sender, CancellationToken cancellationToken) =>
                {
                    var query = new GetActiveMoviesQuery();
                    return await sender.Send(query, cancellationToken);
                })
            .WithName("GetActiveMovies")
            .WithTags(Tag)
            .Produces<IReadOnlyCollection<ActiveMovieDto>>(200, "application/json")
            .Produces(204);


        endpointRouteBuilder.MapGet($"{BaseRoute}/{{movieSessionId}}/seats",
                async (Guid movieSessionId, IMovieSessionSeatsDataCacheService movieSessionSeatsDataCacheService,
                    CancellationToken cancellationToken) => await movieSessionSeatsDataCacheService.GetMovieSessionSeatsData(movieSessionId))
            .WithName("GetSeats")
            .WithTags(Tag)
            .Produces<IReadOnlyCollection<MovieSessionSeatDto>>(200, "application/json")
            .Produces(204);


        endpointRouteBuilder.MapPost($"{BaseRoute}", async ([FromBody] CreateMovieSessionCommand request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(request, cancellationToken);

                return Results.CreatedAtRoute(
                    routeName: "GetShowtimeById",
                    routeValues: new { id = result.ToString() },
                    value: result);
            })
            .WithName("CreateMovieSessions")
            .WithTags(Tag)
            .Produces<Guid>(201, "application/json")
            .Produces(204);


        endpointRouteBuilder.MapGet($"{BaseRoute}/{{movieSessionId}}", async ([FromRoute] Guid movieSessionId,
                    ISender sender,
                    CancellationToken cancellationToken) =>
                {
                    var query = new GetMovieSessionByIdQuery(movieSessionId);

                    return await sender.Send(query, cancellationToken);
                }
            )
            .WithName("GetMovieSessionsById")
            .WithTags(Tag)
            .Produces<MovieSessionsDto>(200, "application/json")
            .Produces(204);


        endpointRouteBuilder.MapGet($"{BaseRoute}", async (
                    [FromServices] IMovieSessionsRepository showtimesRepository,
                    [FromServices] IMapper mapper,
                    CancellationToken cancellationToken) =>
                {
                    var showtimes = await showtimesRepository.GetAllAsync(
                        null,
                        cancellationToken);

                    var response = mapper.Map<IReadOnlyCollection<MovieSessionsDto>>(showtimes);

                    return response;
                }
            )
            .WithName("GetMovieSessions")
            .WithTags(Tag)
            .Produces<IReadOnlyCollection<MovieSessionsDto>>(200, "application/json")
            .Produces(204);


        endpointRouteBuilder.MapGet("api/movies/{movieId}/moviesessions", async (Guid movieId,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var query = new GetMovieSessionsQuery(movieId);
                return await sender.Send(query, cancellationToken);
            })
            .WithName("GetActiveMovieSessionsByMovieId")
            .WithTags(Tag)
            .Produces<IReadOnlyCollection<MovieSessionsDto>>(200, "application/json")
            .Produces(204);
    }
}