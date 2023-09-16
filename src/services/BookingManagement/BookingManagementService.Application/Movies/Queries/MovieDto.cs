using CinemaTicketBooking.Domain.Movies;

namespace CinemaTicketBooking.Application.Movies.Queries;

public class MovieDto
{
    public Guid Id { get; init; }
    public string Title { get; init; }
    public string ImdbId { get; init; }
    public string Stars { get; init; }
    public DateTime ReleaseDate { get; init; }

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<Movie, MovieDto>()
                .ForMember(dst=>dst.Id, opt=>opt.MapFrom(src=>src.Id));
        }
    }
}