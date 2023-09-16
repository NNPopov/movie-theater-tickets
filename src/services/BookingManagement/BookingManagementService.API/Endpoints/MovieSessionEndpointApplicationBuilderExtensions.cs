using AutoMapper;
using CinemaTicketBooking.Api.Endpoints.Common;
using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Application.MovieSessions.Commands.CreateShowtime;
using CinemaTicketBooking.Application.MovieSessions.Queries;
using CinemaTicketBooking.Application.ShoppingCarts.Command.SelectSeats;
using CinemaTicketBooking.Domain.MovieSessions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CinemaTicketBooking.Api.Endpoints;

public class MovieSessionEndpointApplicationBuilderExtensions : IEndpoints
{
    private static readonly string Tag = "MovieSessions";
    private static readonly string BaseRoute = "api/moviesessions";
    


    public static void DefineEndpoints(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder.MapGet($"{BaseRoute}/{{movieSessionId}}/seats",
                async (Guid movieSessionId, ISender sender, CancellationToken cancellationToken) =>
                {
                    var query = new GetMovieSessionSeatsQuery(movieSessionId);
                    return await sender.Send(query, cancellationToken);
                })
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
                    routeValues: new { id = result.ToString()  },
                    value: result);
            })
            .WithName("CreateMovieSessions")
            .WithTags(Tag)
            .Produces<Guid>(201, "application/json")
            .Produces(204);


        endpointRouteBuilder.MapGet($"{BaseRoute}/{{movieSessionId}}", async ([FromRoute] Guid movieSessionId,
                    [FromServices] IMovieSessionsRepository showtimesRepository,
                    CancellationToken cancellationToken) =>
                {
                    var showtimes = await showtimesRepository.GetAllAsync(
                        t => t.Id == movieSessionId,
                        cancellationToken);

                    return showtimes;
                }
            )
            .WithName("GetMovieSessionsById")
            .WithTags(Tag)
            .Produces<ReserveResponse>(200, "application/json")
            .Produces(204);
        
        
        endpointRouteBuilder.MapGet($"{BaseRoute}", async (
                    [FromServices] IMovieSessionsRepository showtimesRepository,
                    [FromServices] IMapper mapper,
                    CancellationToken cancellationToken) =>
                {
                    var showtimes = await showtimesRepository.GetAllAsync(
                        null,
                        cancellationToken);

                   var response = mapper.Map<IList<MovieSessionDTO>>(showtimes);

                    return response;
                }
            )
            .WithName("GetMovieSessions")
            .WithTags(Tag)
            .Produces<ReserveResponse>(200, "application/json")
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

public class MovieSessionDTO 
{
    public Guid Id { get; set; }
    public Guid MovieId { get; set; }
    public DateTime SessionDate { get; set; }
    public Guid AuditoriumId { get; set; }
    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<MovieSession, MovieSessionDTO>()
                .ForMember(dst=>dst.Id, opt=>opt.MapFrom(src=>src.Id));
        }
    }
}