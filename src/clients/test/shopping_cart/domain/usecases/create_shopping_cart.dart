import 'package:dartz/dartz.dart';
import 'package:mocktail/mocktail.dart';
import 'package:movie_theater_tickets/src/shopping_carts/domain/repos/shopping_cart_repo.dart';
import 'package:movie_theater_tickets/src/shopping_carts/domain/usecases/create_shopping_cart.dart';
import 'package:test/test.dart';

class MocShoppingCartRepo extends Mock implements ShoppingCartRepo {}

void main() {
  late CreateShoppingCart usecase;
  late MocShoppingCartRepo repo;
  late  CreateShoppingCartCommand createShoppingCartCommand;

  setUp(() {
    repo = MocShoppingCartRepo();
    usecase = CreateShoppingCart(repo);


  });

  test('should  call the ShoppingCartRepo.createShoppingCartRepo', () async {
    //Arragne
    createShoppingCartCommand = CreateShoppingCartCommand(maxNumberOfSeats: 5);

    //Act

    final result = await usecase(createShoppingCartCommand);
    when(() => repo.createShoppingCart(any())).thenAnswer(
          (_) async => const Right('dcd5d892-4100-400b-b6f3-d4679f5b8db6'),
    );

    //Assert
    expect(result, equals(const Right<dynamic, void>('dcd5d892-4100-400b-b6f3-d4679f5b8db6')));

    verify(() => repo.createShoppingCart(5)).called(1);

    verifyNoMoreInteractions(repo);
  });
}
