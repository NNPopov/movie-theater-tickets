using CinemaTicketBooking.Application.ShoppingCarts.Command.SelectSeats;
using FluentAssertions;
using Xunit;

namespace CinemaTicketBooking.Application.UnitTests.Showtimes;

public class ReserveSeatsCommandValidatorSpecification
{
    [Theory]
    [InlineData("09F67315-012D-4B17-B6F5-C49BE21BBE6B", 1,2, "08F67315-012D-4B17-B6F5-C49BE21BBE6B" )]
    public async Task Should_Reserve_Seats_When_Parameters_Are_Correct(Guid showtimeId, short seats, short row, Guid userId)
    {
        var createRecipeCommand = new SelectSeatCommand(showtimeId,row, seats,userId);

        var validator = new SelectSeatCommandValidator();
        var validationResult = await validator.ValidateAsync(createRecipeCommand);
        validationResult.Errors.Should().BeEmpty();
    }
    
    [Theory]
    [InlineData("00000000-0000-0000-0000-000000000000",  1,3, "08F67315-012D-4B17-B6F5-C49BE21BBE6B"  )]
    [InlineData("09F67315-012D-4B17-B6F5-C49BE21BBE6B", default,default, "08F67315-012D-4B17-B6F5-C49BE21BBE6B"  )]
    [InlineData("09F67315-012D-4B17-B6F5-C49BE21BBE6B", 1,3, "00000000-0000-0000-0000-000000000000" )]
    public async Task Should_Not_Reserve_Seats_When_Parameters_Are_Correct(Guid showtimeId, short seats, short row, Guid userId)
    {
        var createRecipeCommand = new SelectSeatCommand(showtimeId, row, seats, userId);

        var validator = new SelectSeatCommandValidator();
        var validationResult = await validator.ValidateAsync(createRecipeCommand);
        validationResult.Errors.Should().NotBeEmpty();
    }
    
    
}