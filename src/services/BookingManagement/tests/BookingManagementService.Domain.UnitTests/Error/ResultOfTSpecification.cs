using CinemaTicketBooking.Domain.Error;
using FluentAssertions;
using Xunit;

namespace CinemaTicketBooking.Application.UnitTests.Error;

// Acceptance gate for slice 0001_error_model_result_infrastructure.
// This slice has no HTTP entry point and no use-case, so the gate is this in-process
// specification of the public Result<T> contract (see tests.md) rather than a
// WebApplicationFactory outside-in test. It is RED until Result<T> and the generic
// Match overload exist in CinemaTicketBooking.Domain.Error.
public class ResultOfTSpecification
{
    [Fact]
    public void Success_Should_CarryTheValue()
    {
        // Arrange
        const string value = "payload";

        // Act
        var result = Result<string>.Success(value);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Error.Should().Be(CinemaTicketBooking.Domain.Error.Error.None);
        result.Value.Should().Be(value);
    }

    [Fact]
    public void Failure_Should_ExposeTheError()
    {
        // Arrange
        var error = new CinemaTicketBooking.Domain.Error.Error("Test.Code", "test description");

        // Act
        var result = Result<string>.Failure(error);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(error);
    }

    [Fact]
    public void Value_Should_Throw_When_ResultIsFailure()
    {
        // Arrange
        var result = Result<string>.Failure(new CinemaTicketBooking.Domain.Error.Error("Test.Code"));

        // Act
        var act = () => _ = result.Value;

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ImplicitConversion_FromValue_Should_YieldSuccess()
    {
        // Arrange
        const string value = "payload";

        // Act
        Result<string> result = value;

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(value);
    }

    [Fact]
    public void ImplicitConversion_FromError_Should_YieldFailure()
    {
        // Arrange
        var error = new CinemaTicketBooking.Domain.Error.Error("Test.Code", "test description");

        // Act
        Result<string> result = error;

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
    }

    [Fact]
    public void Match_Should_RunSuccessBranch_With_TheCarriedValue()
    {
        // Arrange
        var result = Result<string>.Success("payload");

        // Act
        var output = result.Match(
            value => $"ok:{value}",
            error => $"err:{error.Code}");

        // Assert
        output.Should().Be("ok:payload");
    }

    [Fact]
    public void Match_Should_RunFailureBranch_With_TheError()
    {
        // Arrange
        var result = Result<string>.Failure(new CinemaTicketBooking.Domain.Error.Error("Test.Code", "d"));

        // Act
        var output = result.Match(
            value => $"ok:{value}",
            error => $"err:{error.Code}");

        // Assert
        output.Should().Be("err:Test.Code");
    }
}
