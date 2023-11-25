import 'dart:async';
import '../../../auth/domain/abstraction/auth_statuses.dart';
import '../../../auth/domain/services/auth_service.dart';
import '../repos/shopping_cart_local_repo.dart';
import '../usecases/assign_client_use_case.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:dartz/dartz.dart';

class ShoppingCartAuthListener {
  ShoppingCartAuthListener(
      this._localRepo, this._assignClientUseCase, this._authService);

  final storage = const FlutterSecureStorage();

  final ShoppingCartLocalRepo _localRepo;

  late final AssignClientUseCase _assignClientUseCase;
  late final StreamSubscription<AuthStatus> _authenticationStatusSubscription;

  final AuthService _authService;

  Future<void> init() async {
    _authenticationStatusSubscription =
        _authService.status.listen((event) async {
      if (event.status == AuthenticationStatus.authorized) {
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
}
