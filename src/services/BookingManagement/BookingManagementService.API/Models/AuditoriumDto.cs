using AutoMapper;
using CinemaTicketBooking.Domain.CinemaHalls;

namespace CinemaTicketBooking.Api.Models;

public class AuditoriumDto
{
    public Guid Id { get; init; }


    public string Description { get; init; }

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<CinemaHall, AuditoriumDto>();
        }
    }
}