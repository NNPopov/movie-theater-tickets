using AutoMapper;
using CinemaTicketBooking.Domain.CinemaHalls;

namespace CinemaTicketBooking.Api.Models;

public class AuditoriumInfoDto
{
    public Guid Id { get; init; }


    public string Description { get; init; }

    public ICollection<ICollection<SeatEntityDto>> Seats { get; init; }

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<CinemaHall, AuditoriumInfoDto>()
                .ForMember(dst => dst.Seats, opt => opt.MapFrom(src => src
                    .Seats
                    .GroupBy(t => t.Row)
                    .OrderBy(f=>f.Key)
                    .Select(t => t.Select(d => new SeatEntityDto { Row = d.Row, SeatNumber = d.SeatNumber })
                        .OrderBy(o => o.SeatNumber)
                        .ToList())
                    .ToList()));
        }
    }
}