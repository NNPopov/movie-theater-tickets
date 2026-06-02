// Outside-in acceptance test for slice 0001_flutter_dart_deps_migration.
//
// This is a *migration* slice, so the gate is not a new network Cubit but the
// highest-risk end-to-end path the dependency bumps touch: a shopping-cart
// round-trip through local storage. Passing this test transitively proves that
// after the migration:
//   - freezed 3 codegen produces a working ShoppingCartDto,
//   - json_serializable toJson/fromJson round-trips intact,
//   - the localstorage major bump still stores and returns the item.
//
// Spec: specs/features/platform/0001_flutter_dart_deps_migration/tests.md
//
// Expected RED at the time of writing: the project does not resolve on the
// installed toolchain (intl 0.18.1 vs SDK-pinned 0.20.2), so `flutter test`
// cannot even compile this file. It turns GREEN only once Module A unblocks
// resolution AND the localstorage round-trip works on the migrated majors.

import 'package:flutter_test/flutter_test.dart';
import 'package:movie_theater_tickets/src/shopping_carts/data/repos/shopping_cart_local_repo_impl.dart';
import 'package:movie_theater_tickets/src/shopping_carts/data/models/seat_dto.dart';
import 'package:movie_theater_tickets/src/shopping_carts/domain/entities/seat.dart';
import 'package:movie_theater_tickets/src/shopping_carts/domain/entities/shopping_cart.dart';
import 'package:movie_theater_tickets/core/errors/failures.dart';

void main() {
  // localstorage needs the Flutter binding initialized to back its store.
  TestWidgetsFlutterBinding.ensureInitialized();

  late ShoppingCartLocalRepoImpl localRepo;

  ShoppingCart buildCart() => ShoppingCart(
    id: '3fa85f64-5717-4562-b3fc-2c963f66afa6',
    maxNumberOfSeats: 4,
    createdAt: DateTime.parse('2023-09-28 19:28:53.299Z'),
    movieSessionId: '3fa85f64-5717-4562-b3fc-2c963f66afa6',
    status: ShoppingCartStatus.InWork,
    isAssigned: false,
    isDirty: false,
    seats: <ShoppingCartSeat>[
      ShoppingCartSeatDto(
        seatRow: 1,
        seatNumber: 1,
        selectionExpirationTime: DateTime.parse('2023-09-28 19:28:53.299Z'),
        price: 15.0,
        isDirty: false,
      ),
    ],
  );

  setUp(() async {
    localRepo = ShoppingCartLocalRepoImpl();
    // Isolate scenarios: start every test from an empty store.
    await localRepo.deleteShoppingCart(ShoppingCart.empty());
  });

  test('cart round-trips through local storage', () async {
    final cart = buildCart();

    final setResult = await localRepo.setShoppingCart(cart);
    expect(setResult.isRight(), isTrue);

    final getResult = await localRepo.getShoppingCart();

    expect(getResult.isRight(), isTrue);
    final stored = getResult.getOrElse(() => ShoppingCart.empty());
    expect(stored.id, cart.id);
    expect(stored.maxNumberOfSeats, cart.maxNumberOfSeats);
    expect(stored.shoppingCartSeat.length, 1);
    expect(stored.shoppingCartSeat.first.seatRow, 1);
    expect(stored.shoppingCartSeat.first.seatNumber, 1);
  });

  test('nothing stored yields a 404 DataFailure', () async {
    final getResult = await localRepo.getShoppingCart();

    expect(getResult.isLeft(), isTrue);
    final failure = getResult.fold((l) => l, (_) => null);
    expect(failure, isA<DataFailure>());
    expect(failure!.statusCode, 404);
    expect(failure.message, 'ShoppingCart not stored');
  });
}
