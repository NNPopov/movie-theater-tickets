namespace CinemaTicketBooking.Application.MovieSessions.Commands.CreateShowtime;

internal sealed class CreateMovieSessionCommandValidator: AbstractValidator<CreateMovieSessionCommand>
{
    public CreateMovieSessionCommandValidator()
    {
        RuleFor(v => v.AuditoriumId)
            .NotEmpty();
        
        RuleFor(v => v.MovieId)
            .NotEmpty();
        
        RuleFor(v => v.SessionDate)
            .NotEmpty();
    }
}