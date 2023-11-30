import 'package:dartz/dartz.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:movie_theater_tickets/src/auth/domain/services/auth_service.dart';
import 'package:movie_theater_tickets/src/hub/domain/event_hub.dart';
import 'package:movie_theater_tickets/src/shopping_carts/domain/entities/create_shopping_cart_response.dart';
import 'package:movie_theater_tickets/src/shopping_carts/domain/services/shopping_cart_service.dart';
import 'package:movie_theater_tickets/src/shopping_carts/domain/usecases/create_shopping_cart.dart';

class MocShoppingCartService extends Mock implements ShoppingCartAuthListener {}

class MockEventHub extends Mock implements EventHub {}

class MockAuthService extends Mock implements AuthService {}

void main() {
  late CreateShoppingCartUseCase usecase;
  late MocShoppingCartService repo;
  late CreateShoppingCartCommand createShoppingCartCommand;
  late MockEventHub hub;
  late MockAuthService authService;

  setUp(() {
    repo = MocShoppingCartService();
    hub = MockEventHub();
    authService=MockAuthService();
    usecase = CreateShoppingCartUseCase( repo,  hub, authService);
  });

  group('CreateShoppingCart', () {
    test('should  call the ShoppingCartRepo.createShoppingCartRepo', () async {
      //Arragne
      createShoppingCartCommand =
          const CreateShoppingCartCommand(maxNumberOfSeats: 5);

      //Act

      final result = await usecase(createShoppingCartCommand);

      when(() => repo.createShoppingCartForAnonymousUser(any())).thenAnswer(
        (t) async => const Right(CreateShoppingCartResponse(
            'dcd5d892-4100-400b-b6f3-d4679f5b8db6',
            'f2a8d6cb31e38a1e5d7fc42e55daf53a')),
      );

      //Assert
      expect(
          result,
          equals(const Right<dynamic, void>(
              'dcd5d892-4100-400b-b6f3-d4679f5b8db6')));

      verify(() => repo.createShoppingCartForAnonymousUser(5)).called(1);

      verifyNoMoreInteractions(repo);
    });
  });
}
