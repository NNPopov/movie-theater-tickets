using CinemaTicketBooking.Application.Abstractions.Repositories;
using CinemaTicketBooking.Application.ShoppingCarts.Command.AssingClientCart;
using CinemaTicketBooking.Domain.Error;
using CinemaTicketBooking.Domain.ShoppingCarts;
using CinemaTicketBooking.Domain.ShoppingCarts.Abstractions;
using FluentAssertions;
using NSubstitute;
using Xunit;
using ILogger = Serilog.ILogger;

namespace CinemaTicketBooking.Application.UnitTests.ShoppingCarts;

// Post-green unit facts for slice 0003_assign_client_cart_result_http: the converted
// AssignClientCart use-case returns NotFoundError / ConflictError as a Result (no longer throws),
// propagates a failing domain Result through its now-live IsFailure branch, and on success records
// the signed-in client id (the bug fix) as the cart owner.
public class AssignClientCartCommandHandlerTests
{
    private readonly IActiveShoppingCartRepository _repository = Substitute.For<IActiveShoppingCartRepository>();

    private readonly IShoppingCartLifecycleManager _lifecycleManager =
        Substitute.For<IShoppingCartLifecycleManager>();

    private readonly ILogger _logger = Substitute.For<ILogger>();

    private readonly IDataHasher _dataHasher = Substitute.For<IDataHasher>();

    private AssignClientCartCommandHandler CreateHandler() => new(_repository, _lifecycleManager, _logger);

    [Fact]
    public async Task Handle_Should_ReturnNotFoundError_When_CartDoesNotExist()
    {
        // Arrange
        var command = new AssignClientCartCommand(Guid.NewGuid(), Guid.NewGuid());
        _repository.GetByIdAsync(command.ShoppingCartId).Returns((ShoppingCart)null!);
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<NotFoundError>();
    }

    [Fact]
    public async Task Handle_Should_ReturnConflictError_When_ClientAlreadyOwnsADifferentActiveCart()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var cart = ShoppingCart.Create(5, _dataHasher);
        var otherActiveCart = ShoppingCart.Create(5, _dataHasher);

        var command = new AssignClientCartCommand(cart.Id, clientId);
        _repository.GetByIdAsync(cart.Id).Returns(cart);
        _repository.GetActiveShoppingCartByClientIdAsync(clientId).Returns(otherActiveCart.Id);
        _repository.GetByIdAsync(otherActiveCart.Id).Returns(otherActiveCart);
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ConflictError>();
        await _repository.DidNotReceive().SaveAsync(Arg.Any<ShoppingCart>());
    }

    [Fact]
    public async Task Handle_Should_PropagateDomainConflictError_When_CartAlreadyHasAnOwner()
    {
        // Arrange — the cart already belongs to another client; the signed-in client owns no cart yet,
        // so the conflict can only come from ShoppingCart.AssignClientId returning a failing Result.
        var clientId = Guid.NewGuid();
        var cart = ShoppingCart.Create(5, _dataHasher);
        cart.AssignClientId(Guid.NewGuid());

        var command = new AssignClientCartCommand(cart.Id, clientId);
        _repository.GetByIdAsync(cart.Id).Returns(cart);
        _repository.GetActiveShoppingCartByClientIdAsync(clientId).Returns(Guid.Empty);
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ConflictError>();
        result.Error.Code.Should().Be("ShoppingCart.ConflictException");
        await _repository.DidNotReceive().SaveAsync(Arg.Any<ShoppingCart>());
    }

    [Fact]
    public async Task Handle_Should_AssignTheSignedInClientAsOwner_When_Successful()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var cart = ShoppingCart.Create(5, _dataHasher);

        var command = new AssignClientCartCommand(cart.Id, clientId);
        _repository.GetByIdAsync(cart.Id).Returns(cart);
        _repository.GetActiveShoppingCartByClientIdAsync(clientId).Returns(Guid.Empty);
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // Bug fix: the owner is the signed-in client id, not the cart id.
        cart.ClientId.Should().Be(clientId);
        cart.ClientId.Should().NotBe(cart.Id);
        await _repository.Received(1).SetClientActiveShoppingCartAsync(clientId, cart.Id);
    }
}
