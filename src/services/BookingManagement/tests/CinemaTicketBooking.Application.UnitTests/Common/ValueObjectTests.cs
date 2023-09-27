using CinemaTicketBooking.Domain.Common;
using FluentAssertions;
using Xunit;

namespace CinemaTicketBooking.Application.UnitTests.Common;

public class DerivedObject : ValueObject
{
    public int Value1 { get; set; }
    public int Value2 { get; set; }
    
    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value1;
        yield return Value2;
    }
}

public class ValueObjectTests
{
    [Fact]
    public void TestGetHashCode()
    {
        var derivedObject = new DerivedObject { Value1 = 1, Value2 = 2 };
        var anotherDerivedObject = new DerivedObject { Value1 = 1, Value2 = 2 };
        
        
        (derivedObject.GetHashCode().Equals(anotherDerivedObject.GetHashCode())).Should().BeTrue();;
    }

    [Theory]
    [InlineData(1, 2, 1, 2)]
    [InlineData(3, 4, 3, 4)]
    public void TestEquals(int a, int b, int x, int y)
    {
        var derivedObjectOne = new DerivedObject { Value1 = a, Value2 = b };
        var derivedObjectTwo = new DerivedObject { Value1 = x, Value2 = y };
        derivedObjectOne.Equals(derivedObjectTwo).Should().BeTrue();
    }

    [Theory]
    [InlineData(1, 2, 1, 2)]
    [InlineData(3, 4, 3, 4)]
    public void TestOperators(int a, int b, int x, int y)
    {
        var derivedObjectOne = new DerivedObject { Value1 = a, Value2 = b };
        var derivedObjectTwo = new DerivedObject { Value1 = x, Value2 = y };
        (derivedObjectOne == derivedObjectTwo).Should().BeTrue();
        (derivedObjectOne != derivedObjectTwo).Should().BeFalse();
    }
}