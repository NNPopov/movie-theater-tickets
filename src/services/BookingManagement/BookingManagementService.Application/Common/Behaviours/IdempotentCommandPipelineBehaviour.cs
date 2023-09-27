using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Application.Exceptions;

namespace CinemaTicketBooking.Application.Common.Behaviours;

public class IdempotentCommandPipelineBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IdempotentRequest
{
    private readonly IIdempotencyService _idempotencyService;

    public IdempotentCommandPipelineBehaviour(IIdempotencyService idempotencyService)
    {
        _idempotencyService = idempotencyService;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (await _idempotencyService.RequestExistsAsync(request.RequestId))
        {
            throw new DuplicateRequestException(request.GetType().Name, request.RequestId.ToString() );
        }

        await _idempotencyService.CreateRequestAsync(request.RequestId, typeof(TRequest).Name);

        return await next();
    }
}