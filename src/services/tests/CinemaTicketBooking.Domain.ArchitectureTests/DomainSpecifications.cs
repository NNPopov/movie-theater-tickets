using System.Reflection;
using CinemaTicketBooking.Domain.Common;
using CinemaTicketBooking.Domain.Common.Events;
using FluentAssertions;
using NetArchTest.Rules;

namespace CinemaTicketBooking.Domain.ArchitectureTests;

public class DomainSpecifications
{
    private static readonly Assembly DomainAssembly = typeof(IDomainEvent).Assembly;

    [Fact]
    public void DomainEvents_Should_beSealed()
    {
        var result = Types.InAssembly(DomainAssembly)
            .That()
            .ImplementInterface(typeof(IDomainEvent))
            .And()
            .AreNotAbstract()
            .Should()
            .BeSealed()
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void DomainEvents_Should_HaveDomainEventPostfix()
    {
        var result = Types.InAssembly(DomainAssembly)
            .That()
            .ImplementInterface(typeof(IDomainEvent))
            .Should()
            .HaveNameEndingWith("DomainEvent")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void IAggregateRoot_Should_Have_PrivateParameterlessConstructor()
    {
        var entityTypes = Types.InAssembly(DomainAssembly)
            .That()
            .ImplementInterface(typeof(IAggregateRoot))
            .GetTypes()
            .Where(t => t != typeof(AggregateRoot));

        var failingTypes = new List<Type>();
        foreach (var entityType in entityTypes)
        {
            var constructors = entityType.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance);
            if (!constructors.Any(c => c.IsPrivate && c.GetParameters().Length == 0))
            {
                failingTypes.Add(entityType);
            }
        }

        failingTypes.Should().BeEmpty();
    }

    [Fact]
    public void Domain_Should_NotToHaveDependencyOnApplication()
    {
        var result = Types.InAssembly(DomainAssembly)
            .Should()
            .NotHaveDependencyOn("BookingManagementService.Application")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }
}