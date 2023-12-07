using AutoMapper;
using CinemaTicketBooking.Api.Endpoints.Common;
using CinemaTicketBooking.Api.Models;
using CinemaTicketBooking.Application.Abstractions;

namespace CinemaTicketBooking.Api.Endpoints;

public class CinemaHallEndpointApplicationBuilderExtensions : IEndpoints
{
    private static readonly string Tag = "CinemaHalls";
    private static readonly string BaseRoute = "api/cinema-halls";

    public static void DefineEndpoints(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder.MapGet($"{BaseRoute}",
                async (ICinemaHallRepository cinemaHallRepository, IMapper mapper,
                    CancellationToken cancellationToken) =>
                {
                    var auditoriums = await cinemaHallRepository.GetAllAsync(
                        cancellationToken);


                    return mapper.Map<ICollection<AuditoriumDto>>(auditoriums);
                })
            .Produces<ICollection<AuditoriumDto>>(200, "application/json")
            .WithName("GetCinemaHalls")
            .WithTags(Tag)
            .Produces(404);


        endpointRouteBuilder.MapGet($"{BaseRoute}/{{cinemaHallId}}",
                async (Guid cinemaHallId,
                    ICinemaHallRepository cinemaHallRepository,
                    IMapper mapper,
                    CancellationToken cancellationToken) =>
                {
                    var auditorium = await cinemaHallRepository.GetAsync(cinemaHallId,
                        cancellationToken);


                    return mapper.Map<AuditoriumDto>(auditorium);
                })
            .Produces<ICollection<AuditoriumDto>>(200, "application/json")
            .WithName("GetCinemaHallById")
            .WithTags(Tag)
            .Produces(404);

        endpointRouteBuilder.MapGet($"{BaseRoute}/{{cinemaHallId}}/seats",
                async (Guid cinemaHallId,
                    ICinemaHallRepository cinemaHallRepository,
                    IMapper mapper,
                    CancellationToken cancellationToken) =>
                {
                    var auditorium = await cinemaHallRepository.GetAsync(cinemaHallId,
                        cancellationToken);

                    return mapper.Map<AuditoriumInfoDto>(auditorium);
                })
            .Produces<ICollection<AuditoriumInfoDto>>(200, "application/json")
            .WithName("GetCinemaHallInfoById")
            .WithTags(Tag)
            .Produces(404);
    }
}