import 'dart:async';

import 'package:get_it/get_it.dart';
import '../../../../core/buses/event_bus.dart';
import '../../../../core/errors/failures.dart';
import '../../../../core/utils/typedefs.dart';
import '../../../auth/domain/abstraction/auth_event_bus.dart';
import '../../../auth/domain/services/auth_service.dart';
import '../../../hub/app_events.dart';
import '../entities/create_shopping_cart_response.dart';
import '../entities/shopping_cart.dart';
import '../repos/shopping_cart_local_repo.dart';
import '../repos/shopping_cart_repo.dart';
import '../usecases/assign_client_use_case.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:dartz/dartz.dart';

GetIt getIt = GetIt.instance;

class ShoppingCartService {
  ShoppingCartService(this._repo,
      {ShoppingCartLocalRepo? localRepo,
      EventBus? eventBus,
      AuthService? authService,
      AuthEventBus? authEventBus})
      : _eventBus = eventBus ?? getIt.get<EventBus>(),
        _authService = authService ?? getIt.get<AuthService>(),
        _authEventBus = authEventBus ?? getIt<AuthEventBus>(),
        _localRepo = localRepo ?? getIt<ShoppingCartLocalRepo>();

  final ShoppingCartRepo _repo;

  late final StreamSubscription _appEventSubscription;
  final storage = const FlutterSecureStorage();

  late final ShoppingCartLocalRepo _localRepo;
  late final AuthEventBus _authEventBus;
  late final AuthService _authService;
  late final EventBus _eventBus;

  late final AssignClientUseCase _assignClientUseCase;

  Future<void> init() async {
    _appEventSubscription = _authEventBus.stream.listen((event) async {
      if (event is AuthorizedAuthStatus) {
        var shoppingCartResult = await _localRepo.getShoppingCart();

        shoppingCartResult.fold((l) => null, (r) async {
          var assignClientResult = await _assignClientUseCase(r.id!);

          assignClientResult.fold((l) => null, (r) async {
            return const Right(null);
          });
        });
      }
    });
  }

  ResultFuture<ShoppingCart> getShoppingCartById(String shoppingCartId) async {
    return await _repo.getShoppingCart(shoppingCartId);
  }

  ResultFuture<CreateShoppingCartResponse>
      getShoppingCartIfExistsForNotAnonymousUser() async {
    var userStatus = await _authService.getCurrentStatus();

    return userStatus.fold((l) => Left(l), (r) async {
      if (r is AuthorizedAuthStatus) {
        return await _repo.getCurrentUserShoppingCart();
      }
      return const Left(NotAuthorisedException());
    });
  }

  ResultFuture<void> assignClient(String shoppingCartId) async {
    var result = await _repo.assignClient(shoppingCartId);


    return result;
  }

  ResultFuture<CreateShoppingCartResponse> createShoppingCartForAnonymousUser(
      int maxNumberOfSeats) async {
    if (maxNumberOfSeats > 4 || maxNumberOfSeats < 1) {
      return const Left(
          ValidationFailure(message: 'Number of places should be from 1 to 4'));
    }

    return await _repo.createShoppingCart(maxNumberOfSeats);
  }
}
