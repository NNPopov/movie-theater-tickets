// Outside-in acceptance test for slice 0003_shopping_cart_not_found_404.
//
// Spec: specs/features/shopping_carts/0003_shopping_cart_not_found_404/tests.md
//
// Wires REAL GetShoppingCartUseCase + ShoppingCartCubit and mocks only the
// system boundaries (ShoppingCartRepo as the network boundary, AuthService,
// EventHub, EventBus, the local repo, and FlutterSecureStorage via its platform
// channel). It proves that a by-id read reporting the cart is gone via
// DataFailure(statusCode: 404) resolves to the clean empty cart AND clears the
// stale cart id — the same observable outcome the old 204 produced.
//
// This is the RED phase: against the current implementation (which only
// recognizes 204) Scenario 1 fails, because a 404 currently yields an error
// state and leaves the stale id in storage. Do NOT edit this test to make it
// pass — change the implementation.

import 'package:dartz/dartz.dart';
import 'package:flutter/services.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';

import 'package:movie_theater_tickets/core/buses/event_bus.dart';
import 'package:movie_theater_tickets/core/errors/failures.dart';
import 'package:movie_theater_tickets/src/auth/domain/abstraction/auth_statuses.dart';
import 'package:movie_theater_tickets/src/auth/domain/services/auth_service.dart';
import 'package:movie_theater_tickets/src/helpers/constants.dart';
import 'package:movie_theater_tickets/src/hub/app_events.dart';
import 'package:movie_theater_tickets/src/hub/domain/event_hub.dart';
import 'package:movie_theater_tickets/src/shopping_carts/domain/entities/shopping_cart.dart';
import 'package:movie_theater_tickets/src/shopping_carts/domain/repos/shopping_cart_local_repo.dart';
import 'package:movie_theater_tickets/src/shopping_carts/domain/repos/shopping_cart_repo.dart';
import 'package:movie_theater_tickets/src/shopping_carts/domain/usecases/create_shopping_cart.dart';
import 'package:movie_theater_tickets/src/shopping_carts/domain/usecases/get_shopping_cart.dart';
import 'package:movie_theater_tickets/src/shopping_carts/domain/usecases/reserve_seats.dart';
import 'package:movie_theater_tickets/src/shopping_carts/domain/usecases/select_seat.dart';
import 'package:movie_theater_tickets/src/shopping_carts/domain/usecases/shopping_cart_subscribe.dart';
import 'package:movie_theater_tickets/src/shopping_carts/domain/usecases/unselect_seat.dart';
import 'package:movie_theater_tickets/src/shopping_carts/presentation/cubit/shopping_cart_cubit.dart';

class _MockShoppingCartRepo extends Mock implements ShoppingCartRepo {}

class _MockShoppingCartLocalRepo extends Mock
    implements ShoppingCartLocalRepo {}

class _MockAuthService extends Mock implements AuthService {}

class _MockEventHub extends Mock implements EventHub {}

class _MockEventBus extends Mock implements EventBus {}

class _MockCreateShoppingCartUseCase extends Mock
    implements CreateShoppingCartUseCase {}

class _MockSelectSeatUseCase extends Mock implements SelectSeatUseCase {}

class _MockUnselectSeatUseCase extends Mock implements UnselectSeatUseCase {}

class _MockShoppingCartUpdateSubscribeUseCase extends Mock
    implements ShoppingCartUpdateSubscribeUseCase {}

class _MockReserveSeatsUseCase extends Mock implements ReserveSeatsUseCase {}

const _staleId = 'stale-cart-1';
const _staleHash = 'stale-hash-1';

const _secureStorageChannel =
    MethodChannel('plugins.it_nomads.com/flutter_secure_storage');

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  // In-memory backing store for the mocked flutter_secure_storage channel.
  late Map<String, String> secureStore;

  late _MockShoppingCartRepo repo;
  late _MockShoppingCartLocalRepo localRepo;
  late _MockAuthService authService;
  late _MockEventHub eventHub;
  late _MockEventBus eventBus;
  late _MockCreateShoppingCartUseCase createUseCase;
  late _MockSelectSeatUseCase selectUseCase;
  late _MockUnselectSeatUseCase unselectUseCase;
  late _MockShoppingCartUpdateSubscribeUseCase subscribeUseCase;
  late _MockReserveSeatsUseCase reserveUseCase;

  late GetShoppingCartUseCase getShoppingCartUseCase; // REAL — under test.

  void installSecureStorageMock() {
    TestDefaultBinaryMessengerBinding.instance.defaultBinaryMessenger
        .setMockMethodCallHandler(_secureStorageChannel, (call) async {
      final args = (call.arguments as Map?)?.cast<String, dynamic>() ?? {};
      switch (call.method) {
        case 'read':
          return secureStore[args['key'] as String];
        case 'write':
          secureStore[args['key'] as String] = args['value'] as String;
          return null;
        case 'delete':
          secureStore.remove(args['key'] as String);
          return null;
        case 'readAll':
          return Map<String, String>.from(secureStore);
        case 'deleteAll':
          secureStore.clear();
          return null;
        case 'containsKey':
          return secureStore.containsKey(args['key'] as String);
        default:
          return null;
      }
    });
  }

  ShoppingCartCubit buildCubit() => ShoppingCartCubit(
        createUseCase,
        selectUseCase,
        unselectUseCase,
        getShoppingCartUseCase,
        subscribeUseCase,
        reserveUseCase,
        eventBus,
      );

  setUp(() {
    secureStore = <String, String>{};
    installSecureStorageMock();

    repo = _MockShoppingCartRepo();
    localRepo = _MockShoppingCartLocalRepo();
    authService = _MockAuthService();
    eventHub = _MockEventHub();
    eventBus = _MockEventBus();
    createUseCase = _MockCreateShoppingCartUseCase();
    selectUseCase = _MockSelectSeatUseCase();
    unselectUseCase = _MockUnselectSeatUseCase();
    subscribeUseCase = _MockShoppingCartUpdateSubscribeUseCase();
    reserveUseCase = _MockReserveSeatsUseCase();

    getShoppingCartUseCase = GetShoppingCartUseCase(
      repo,
      localRepo,
      authService,
      eventHub,
      eventBus,
    );

    // Unauthenticated → use-case takes the stored-id path and reads the cart by id.
    when(() => authService.getCurrentStatus())
        .thenAnswer((_) async => const Left<Failure, AuthStatus>(
              NotFoundFailure(),
            ));
    when(() => eventHub.shoppingCartUpdateSubscribe(_staleId))
        .thenAnswer((_) async {});
    when(() => eventBus.stream).thenAnswer((_) => const Stream<dynamic>.empty());
  });

  tearDown(() {
    TestDefaultBinaryMessengerBinding.instance.defaultBinaryMessenger
        .setMockMethodCallHandler(_secureStorageChannel, null);
  });

  // Drives the cubit through one full load and asserts the externally observable
  // outcome: the emitted state sequence, the stale id being cleared, and the
  // hash-id-updated event being published.
  Future<void> runLoadAndExpectEmptyCart(ShoppingCartCubit cubit) async {
    // Let the constructor-triggered auto-load settle so the explicit act below
    // produces a deterministic state sequence.
    await pumpEventQueue();

    // Re-establish the precondition: a stale cart id is present in storage.
    secureStore[Constants.SHOPPING_CARD_ID] = _staleId;
    secureStore[Constants.SHOPPING_CARD_HASH_ID] = _staleHash;
    clearInteractions(repo);
    clearInteractions(eventBus);

    final expectation = expectLater(
      cubit.stream,
      emitsInOrder(<Matcher>[
        isA<ShoppingCartState>().having(
          (s) => s.status,
          'status',
          ShoppingCartStateStatus.creating,
        ),
        isA<ShoppingCartState>().having(
          (s) => s.status,
          'status',
          ShoppingCartStateStatus.initial,
        ),
      ]),
    );

    await cubit.getShoppingCart();
    await expectation;

    // The stale id (and hash id) must be cleared from secure storage.
    expect(secureStore.containsKey(Constants.SHOPPING_CARD_ID), isFalse);
    expect(secureStore.containsKey(Constants.SHOPPING_CARD_HASH_ID), isFalse);

    // The cart-by-id read happened at the network boundary.
    verify(() => repo.getShoppingCart(_staleId)).called(1);

    // The hash-id-updated event was published as part of the cleanup.
    final sent = verify(() => eventBus.send(captureAny())).captured;
    expect(sent.whereType<ShoppingCartHashIdUpdated>(), isNotEmpty);
  }

  test(
    'missing cart by id (404) emits empty cart and clears the stale id',
    () async {
      when(() => repo.getShoppingCart(_staleId)).thenAnswer(
        (_) async => const Left<Failure, ShoppingCart>(
          DataFailure(message: "shoppingCartId doesn't exist", statusCode: 404),
        ),
      );

      final cubit = buildCubit();
      addTearDown(cubit.close);

      await runLoadAndExpectEmptyCart(cubit);
    },
  );

  test(
    'regression: missing cart by id (204) still emits empty cart and clears the id',
    () async {
      when(() => repo.getShoppingCart(_staleId)).thenAnswer(
        (_) async => const Left<Failure, ShoppingCart>(
          DataFailure(message: "shoppingCartId doesn't exist", statusCode: 204),
        ),
      );

      final cubit = buildCubit();
      addTearDown(cubit.close);

      await runLoadAndExpectEmptyCart(cubit);
    },
  );
}
