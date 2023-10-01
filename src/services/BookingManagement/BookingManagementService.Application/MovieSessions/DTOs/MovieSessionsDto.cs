using CinemaTicketBooking.Domain.MovieSessions;

namespace CinemaTicketBooking.Application.MovieSessions.DTOs;

public class MovieSessionsDto
{
    public Guid Id { get; init; }
    public Guid MovieId { get; set; }
    public DateTime SessionDate { get; init; }
    public Guid AuditoriumId { get; init; }

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<MovieSession, MovieSessionsDto>();
        }
    }
}