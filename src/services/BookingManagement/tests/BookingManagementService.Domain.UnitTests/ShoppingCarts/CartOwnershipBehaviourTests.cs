using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Application.Common.Behaviours;
using CinemaTicketBooking.Application.Exceptions;
using CinemaTicketBooking.Domain.ShoppingCarts;
using CinemaTicketBooking.Domain.ShoppingCarts.Abstractions;
using FluentAssertions;
using MediatR;
using NSubstitute;
using Xunit;

namespace CinemaTicketBooking.Application.UnitTests.ShoppingCarts;

// RED acceptance gate for slice 0009_cart_ownership_authorization (ADR-003 slice 1).
//
// The central object-level authorization mechanism is a MediatR pipeline behaviour,
// CartOwnershipBehaviour<TRequest, TResponse> constrained `where TRequest : ICartScopedRequest`,
// that loads the cart by request.ShoppingCartId (via IActiveShoppingCartRepository) and applies
// the ADR-003 two-mode rule using the caller identity from ICurrentUser:
//   - cart not found            => pass through (next invoked; handler keeps its existing 404);
//   - cart anonymous (ClientId == Guid.Empty) => pass through (guest capability preserved);
//   - cart assigned + caller authenticated AND ClientId == cart.ClientId => pass through (owner);
//   - cart assigned + (unauthenticated OR ClientId != cart.ClientId) => throw ForbiddenAccessException
//     (mapped centrally to 403), and next is NOT invoked.
//
// Expected constructor: CartOwnershipBehaviour(IActiveShoppingCartRepository carts, ICurrentUser currentUser).
//
// This gate is RED until ICartScopedRequest, ICurrentUser, and CartOwnershipBehaviour exist:
// today none of these types compile, so the whole class is a build-failure red. Once the
// behaviour and its two abstractions land per plan.md section 5, all five scenarios pass.
public class CartOwnershipBehaviourTests
{
    private readonly IActiveShoppingCartRepository _cartRepository =
        Substitute.For<IActiveShoppingCartRepository>();

    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private readonly IDataHasher _dataHasher = Substitute.For<IDataHasher>();

    private CartOwnershipBehaviour<TestCartScopedRequest, bool> CreateBehaviour() =>
        new(_cartRepository, _currentUser);

    [Fact]
    public async Task Handle_Should_PassThrough_When_CartIsAnonymous()
    {
        // Arrange — an anonymous cart (ClientId == Guid.Empty) is a pure capability; any caller passes.
        var cart = AnonymousCart();
        _cartRepository.GetByIdAsync(cart.Id).Returns(cart);
        var request = new TestCartScopedRequest(cart.Id);
        var nextCalled = false;
        RequestHandlerDelegate<bool> next = () =>
        {
            nextCalled = true;
            return Task.FromResult(true);
        };

        // Act
        var result = await CreateBehaviour().Handle(request, next, CancellationToken.None);

        // Assert
        nextCalled.Should().BeTrue();
        result.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Should_PassThrough_When_CartIsAssigned_And_CallerIsTheOwner()
    {
        // Arrange
        var owner = Guid.NewGuid();
        var cart = AssignedCart(owner);
        _cartRepository.GetByIdAsync(cart.Id).Returns(cart);
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.ClientId.Returns(owner);
        var request = new TestCartScopedRequest(cart.Id);
        var nextCalled = false;
        RequestHandlerDelegate<bool> next = () =>
        {
            nextCalled = true;
            return Task.FromResult(true);
        };

        // Act
        var result = await CreateBehaviour().Handle(request, next, CancellationToken.None);

        // Assert
        nextCalled.Should().BeTrue();
        result.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Should_Throw_And_NotInvokeNext_When_CartIsAssigned_And_CallerIsDifferent()
    {
        // Arrange — authenticated, but a different client than the cart owner (the stranger / IDOR case).
        var cart = AssignedCart(Guid.NewGuid());
        _cartRepository.GetByIdAsync(cart.Id).Returns(cart);
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.ClientId.Returns(Guid.NewGuid());
        var request = new TestCartScopedRequest(cart.Id);
        var nextCalled = false;
        RequestHandlerDelegate<bool> next = () =>
        {
            nextCalled = true;
            return Task.FromResult(true);
        };

        // Act
        Func<Task> act = () => CreateBehaviour().Handle(request, next, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ForbiddenAccessException>();
        nextCalled.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_Should_Throw_And_NotInvokeNext_When_CartIsAssigned_And_CallerIsUnauthenticated()
    {
        // Arrange — an owned cart cannot be operated anonymously.
        var cart = AssignedCart(Guid.NewGuid());
        _cartRepository.GetByIdAsync(cart.Id).Returns(cart);
        _currentUser.IsAuthenticated.Returns(false);
        _currentUser.ClientId.Returns(Guid.Empty);
        var request = new TestCartScopedRequest(cart.Id);
        var nextCalled = false;
        RequestHandlerDelegate<bool> next = () =>
        {
            nextCalled = true;
            return Task.FromResult(true);
        };

        // Act
        Func<Task> act = () => CreateBehaviour().Handle(request, next, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ForbiddenAccessException>();
        nextCalled.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_Should_PassThrough_When_CartDoesNotExist()
    {
        // Arrange — a missing cart passes through so the handler owns the existing 404; existence is
        // not leaked as a 403.
        _cartRepository.GetByIdAsync(Arg.Any<Guid>()).Returns((ShoppingCart)null!);
        var request = new TestCartScopedRequest(Guid.NewGuid());
        var nextCalled = false;
        RequestHandlerDelegate<bool> next = () =>
        {
            nextCalled = true;
            return Task.FromResult(true);
        };

        // Act
        var result = await CreateBehaviour().Handle(request, next, CancellationToken.None);

        // Assert
        nextCalled.Should().BeTrue();
        result.Should().BeTrue();
    }

    private ShoppingCart AnonymousCart() => ShoppingCart.Create(5, _dataHasher);

    private ShoppingCart AssignedCart(Guid owner)
    {
        var cart = ShoppingCart.Create(5, _dataHasher);
        cart.AssignClientId(owner);
        return cart;
    }

    // A minimal request that opts into the cart-ownership check, used to drive the behaviour
    // without coupling the gate to PurchaseTicketsCommand / GetShoppingCartQuery.
    private sealed record TestCartScopedRequest(Guid ShoppingCartId) : ICartScopedRequest;
}
