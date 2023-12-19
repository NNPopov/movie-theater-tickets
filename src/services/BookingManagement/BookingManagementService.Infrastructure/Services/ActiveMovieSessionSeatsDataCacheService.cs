using CinemaTicketBooking.Application.Abstractions.Services;
using CinemaTicketBooking.Application.MovieSessionSeats;
using CinemaTicketBooking.Application.MovieSessionSeats.Queries;
using CinemaTicketBooking.Domain.Common.Ensure;
using MediatR;
using Serilog;

namespace CinemaTicketBooking.Infrastructure.Services;

public class ActiveMovieSessionSeatsDataCacheService : IMovieSessionSeatsDataCacheService
{
    private readonly ICacheService _cacheService;
    private readonly IMediator _mediator;
    private readonly ILogger _logger;

    public ActiveMovieSessionSeatsDataCacheService(ICacheService cacheService, IMediator mediator, ILogger logger)
    {
        _cacheService = cacheService;
        _mediator = mediator;
        _logger = logger;
    }

    private const string MovieSessionSeatsKeyPrefix = "MovieSessionSeatsCache";

    public async Task AddOrUpdateMovieSessionSeatsCache(ActiveMovieSessionSeatsDTO data)
    {
        var movieSessionSeatsCacheLifetime =
            data.MovieSessionExpirationTime.Subtract(TimeProvider.System.GetUtcNow().DateTime);

        var movieSessionSeatsKey = MovieSessionSeatsKey(data.MovieSessionId);

        await _cacheService.Set(movieSessionSeatsKey, data, movieSessionSeatsCacheLifetime);
        
        _logger.Debug("MovieSessionSeatsCache has been updated for movieSessionId:{@MovieSessionId}",
            data.MovieSessionId);
    }
    
    public async Task<ActiveMovieSessionSeatsDTO?> GetMovieSessionSeatsData(Guid movieSessionId)
    {
        Ensure.NotEmpty(movieSessionId, "The MovieSessionId is required.", nameof(movieSessionId));
        
        var movieSessionSeatsKey = MovieSessionSeatsKey(movieSessionId);

        var movieSessionSeats = await _cacheService.TryGet<ActiveMovieSessionSeatsDTO>(movieSessionSeatsKey);

        return movieSessionSeats;
    }

    public async Task<ActiveMovieSessionSeatsDTO?> GetActualMovieSessionSeatsData(Guid movieSessionId)
    {
        ActiveMovieSessionSeatsDTO? movieSessionSeats = await _mediator.Send(new GetActiveMovieSessionSeatsQuery(movieSessionId));

        if (movieSessionSeats is null)
        {
            _logger.Error("Movie session seats not found:{@MovieSessionId}", movieSessionId);
            return default;
        }
        return movieSessionSeats;
    }
    

    private static string MovieSessionSeatsKey(Guid movieSessionId)
    {
        return $"{MovieSessionSeatsKeyPrefix}:{movieSessionId}";
    }
}