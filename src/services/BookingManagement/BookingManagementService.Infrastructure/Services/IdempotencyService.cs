using CinemaTicketBooking.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CinemaTicketBooking.Infrastructure.Services;

public class IdempotencyService : IIdempotencyService
{
    private readonly CinemaContext _cinemaContext;

    public IdempotencyService(CinemaContext cinemaContext)
    {
        _cinemaContext = cinemaContext;
    }

    public async Task<bool> RequestExistsAsync(Guid requestId)
    {
        return await _cinemaContext.Set<IdempotentRequest>().AnyAsync(t => t.Id == requestId);
    }

    public async Task CreateRequestAsync(Guid requestId, string name)
    {
        var idempotentRequest = new IdempotentRequest
        {
            Id = requestId,
            Name = name,
            CreatedOnUtc = TimeProvider.System.GetUtcNow().DateTime
        };

        await _cinemaContext.Set<IdempotentRequest>().AddAsync(idempotentRequest);
        await _cinemaContext.SaveChangesAsync();
    }
}

public sealed class IdempotentRequest
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateTime CreatedOnUtc { get; set; }
}