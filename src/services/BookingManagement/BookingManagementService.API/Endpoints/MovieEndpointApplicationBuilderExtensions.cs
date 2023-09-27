﻿using AutoMapper;
using CinemaTicketBooking.Api.Endpoints.Common;
using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Application.Movies.Queries;
using MediatR;

namespace CinemaTicketBooking.Api.Endpoints;

public class MovieEndpointApplicationBuilderExtensions : IEndpoints
{
    private static readonly string Tag = "Movie";
    private static readonly string BaseRoute = "api/movies";

    public static void DefineEndpoints(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder.MapGet($"{BaseRoute}/{{movieId}}",
                async (Guid movieId,
                    ISender sender,
                    CancellationToken cancellationToken) =>
                {
                    var query = new GetMovieQuery(movieId);
                    return await sender.Send(query, cancellationToken);
                })
            .Produces<MovieDto>(200, "application/json")
            .WithName("GetMovieById")
            .WithTags(Tag)
            .Produces(404);

        endpointRouteBuilder.MapGet($"{BaseRoute}",
                async (IMoviesRepository moviesRepository, IMapper mapper,
                    CancellationToken cancellationToken) =>
                {
                    var movie = await moviesRepository.GetAllAsync(
                        cancellationToken);
                    
                    return mapper.Map<ICollection<MovieDto>>(movie);
                })
            .Produces<MovieDto>(200, "application/json")
            .WithName("GetMovies")
            .WithTags(Tag)
            .Produces(404);
    }
}