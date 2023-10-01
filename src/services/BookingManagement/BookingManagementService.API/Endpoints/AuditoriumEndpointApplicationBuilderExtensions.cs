using AutoMapper;
using CinemaTicketBooking.Api.Endpoints.Common;
using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Domain.CinemaHalls;

namespace CinemaTicketBooking.Api.Endpoints;

public class AuditoriumEndpointApplicationBuilderExtensions : IEndpoints
{
    private static readonly string Tag = "CinemaHalls";
    private static readonly string BaseRoute = "api/cinema-halls";

    public static void DefineEndpoints(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder.MapGet($"{BaseRoute}",
                async (IAuditoriumsRepository auditoriumsRepository, IMapper mapper,
                    CancellationToken cancellationToken) =>
                {
                    var auditoriums = await auditoriumsRepository.GetAllAsync(
                        cancellationToken);

                    
                    return mapper.Map<ICollection<AuditoriumDTO>>(auditoriums);
                })
            .Produces<ICollection<AuditoriumDTO>>(200, "application/json")
            .WithName("GetAuditoriums")
            .WithTags(Tag)
            .Produces(404);
    }
}

public class AuditoriumDTO 
{
    public Guid Id { get; set; }


    public string Description { get; set; }
    
    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<CinemaHall, AuditoriumDTO>();
        }
    }
}

public class SeatEntityDTO 
{
    public short Row { get; set; }
    public short SeatNumber { get; set; }
    
    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<SeatEntity, SeatEntityDTO>();
        }
    }
}