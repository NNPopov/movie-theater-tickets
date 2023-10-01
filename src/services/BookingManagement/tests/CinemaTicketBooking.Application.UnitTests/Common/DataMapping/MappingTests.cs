using System.Reflection;
using System.Runtime.Serialization;
using CinemaTicketBooking.Application.Movies.Queries;
using AutoMapper;
using CinemaTicketBooking.Application;
using CinemaTicketBooking.Application.MovieSessions.DTOs;
using CinemaTicketBooking.Application.MovieSessions.Queries;
using CinemaTicketBooking.Domain.Movies;
using CinemaTicketBooking.Domain.MovieSessions;
using Xunit;

namespace CinemaTicketBooking.Application.UnitTests.Common.DataMapping;

public class MappingTests
{
    private readonly IConfigurationProvider _configuration;
    private readonly IMapper _mapper;

    public MappingTests()
    {
        _configuration = new MapperConfiguration(config =>
            config.AddMaps(Assembly.GetAssembly(typeof(ConfigureServices))));

        _mapper = _configuration.CreateMapper();
    }

    [Fact]
    public void ShouldHaveValidConfiguration()
    {
        _configuration.AssertConfigurationIsValid();
    }

    [Theory]
    [InlineData(typeof(Movie), typeof(MovieDto))]
    [InlineData(typeof(MovieSession), typeof(MovieSessionsDto))]
    
    public void ShouldMappingFromSourceToDestination(Type source, Type destination)
    {
        var instance = GetInstanceOf(source);

        _mapper.Map(instance, source, destination);
    }

    private object GetInstanceOf(Type type)
    {
        if (type.GetConstructor(Type.EmptyTypes) != null)
            return Activator.CreateInstance(type)!;

#pragma warning disable SYSLIB0050
        return FormatterServices.GetUninitializedObject(type);
#pragma warning restore SYSLIB0050
    }
}