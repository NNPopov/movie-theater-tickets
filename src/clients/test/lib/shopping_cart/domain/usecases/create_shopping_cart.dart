import 'package:dartz/dartz.dart';
import 'package:mocktail/mocktail.dart';
import 'package:movie_theater_tickets/src/hub/domain/event_hub.dart';
import 'package:movie_theater_tickets/src/shopping_carts/domain/entities/create_shopping_cart_response.dart';
import 'package:movie_theater_tickets/src/shopping_carts/domain/repos/shopping_cart_repo.dart';
import 'package:movie_theater_tickets/src/shopping_carts/domain/usecases/create_shopping_cart.dart';
import 'package:test/test.dart';

class MocShoppingCartRepo extends Mock implements ShoppingCartRepo {}
class MockEventHub extends Mock implements EventHub {}

void main() {
  late CreateShoppingCartUseCase usecase;
  late MocShoppingCartRepo repo;
  late  CreateShoppingCartCommand createShoppingCartCommand;
  late MockEventHub hub;

  setUp(() {
    repo = MocShoppingCartRepo();
    hub = MockEventHub();
    usecase = CreateShoppingCartUseCase(repo:repo,eventHub: hub);


  });

  test('should  call the ShoppingCartRepo.createShoppingCartRepo', () async {
    //Arragne
    createShoppingCartCommand = const CreateShoppingCartCommand(maxNumberOfSeats: 5);

    //Act

    final result = await usecase(createShoppingCartCommand);


    when(() => repo.createShoppingCart(any())).thenAnswer(
          (t) async =>

          const Right(CreateShoppingCartResponse('dcd5d892-4100-400b-b6f3-d4679f5b8db6','f2a8d6cb31e38a1e5d7fc42e55daf53a')),
    );

    //Assert
    expect(result, equals(const Right<dynamic, void>('dcd5d892-4100-400b-b6f3-d4679f5b8db6')));

    verify(() => repo.createShoppingCart(5)).called(1);

    verifyNoMoreInteractions(repo);
  });
}
