using CinemaTicketBooking.Domain.Common;
using FluentAssertions;
using Xunit;

namespace CinemaTicketBooking.Application.UnitTests.Common;

public class EntityTests
{   
    private class TestEntity : Entity<int>
    {
        public TestEntity(int id) : base(id) { }
    }
 
    [Fact]
    public void EqualsWithSameIdTest()
    {
        var t1 = new TestEntity(1);
        var t2 = new TestEntity(1);


        t1.Equals(t2).Should().BeTrue();
    }
    
    [Fact]
    public void EqualsWithDifferentIdTest()
    {
        var t1 = new TestEntity(1);
        var t2 = new TestEntity(2);
        t1.Equals(t2).Should().BeFalse();
    }

    [Fact]
    public void GetHashCodeTest()
    {
        var t1 = new TestEntity(1);
        var t1Hash = t1.GetHashCode();

        var t2 = new TestEntity(1);
        var t2Hash = t2.GetHashCode();
        
        t1Hash.Equals(t2Hash).Should().BeTrue();
    }
    
    [Fact]
    public void EqualityOperatorTest()
    {
        var t1 = new TestEntity(1);
        var t2 = new TestEntity(1);
        
        (t1 == t2).Should().BeTrue();
    }

    [Fact]
    public void InequalityOperatorTest()
    {
        var t1 = new TestEntity(1);
        var t2 = new TestEntity(2);
        
        (t1 != t2).Should().BeTrue();
    }
}