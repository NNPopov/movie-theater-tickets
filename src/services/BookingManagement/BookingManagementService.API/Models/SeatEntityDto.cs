using AutoMapper;
using CinemaTicketBooking.Domain.CinemaHalls;

namespace CinemaTicketBooking.Api.Models;

public class SeatEntityDto
{
    public short Row { get; set; }
    public short SeatNumber { get; set; }

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<SeatEntity, SeatEntityDto>();
        }
    }
}