using AutoMapper;
using CinemaTicketBooking.Application.Exceptions;
using CinemaTicketBooking.Application.ShoppingCarts.Queries;
using CinemaTicketBooking.Domain.ShoppingCarts;
using CinemaTicketBooking.Domain.ShoppingCarts.Abstractions;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CinemaTicketBooking.Application.UnitTests.ShoppingCarts;

// Carve-out A for slice 0002_content_not_found_404: the get-current-cart query no longer treats
// "no active cart" as a not-found error. It returns null (→ 204 at the endpoint) when the client
// has no active cart, but keeps throwing ContentNotFoundException (→ 404) for the inconsistent
// state where an active-cart id is recorded but its record is missing.
public class GetCurrentShoppingCartQueryHandlerTests
{
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly IActiveShoppingCartRepository _repository = Substitute.For<IActiveShoppingCartRepository>();

    private GetCurrentShoppingCartQueryHandler CreateHandler() => new(_mapper, _repository);

    [Fact]
    public async Task Handle_Should_ReturnNull_When_ClientHasNoActiveCart()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        _repository.GetActiveShoppingCartByClientIdAsync(clientId).Returns(Guid.Empty);
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetCurrentShoppingCartQuery(clientId), CancellationToken.None);

        // Assert
        result.Should().BeNull();
        await _repository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task Handle_Should_ThrowContentNotFoundException_When_ActiveCartRecordedButRecordMissing()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var cartId = Guid.NewGuid();
        _repository.GetActiveShoppingCartByClientIdAsync(clientId).Returns(cartId);
        _repository.GetByIdAsync(cartId).Returns((ShoppingCart)null!);
        var handler = CreateHandler();

        // Act
        var act = async () => await handler.Handle(new GetCurrentShoppingCartQuery(clientId), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ContentNotFoundException>();
    }

    [Fact]
    public async Task Handle_Should_ReturnResponse_When_ActiveCartExists()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var cart = ShoppingCart.Create(5, Substitute.For<IDataHasher>());
        _repository.GetActiveShoppingCartByClientIdAsync(clientId).Returns(cart.Id);
        _repository.GetByIdAsync(cart.Id).Returns(cart);
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetCurrentShoppingCartQuery(clientId), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.ShoppingCartId.Should().Be(cart.Id);
        result.HashId.Should().Be(cart.HashId);
    }
}
