import 'dart:async' as domain;
import 'package:bloc/bloc.dart';
import '../../domain/abstruction/auth_event_bus.dart';
import '../../domain/services/auth_service.dart';
import 'package:get_it/get_it.dart';

GetIt getIt = GetIt.instance;

class AuthCubit extends Cubit<AuthStatus> {
  AuthCubit({AuthService? authService})
      : _authService = authService ?? getIt.get<AuthService>(),
        super(UnauthorizedAuthStatus());

  late AuthService _authService;

  domain.Future<void> logInt() async {
    var statusResult = await _authService.logIn();

    statusResult.fold((l) => null, (status) => emit(AuthorizedAuthStatus()));
  }

  domain.Future<void> getAuthStatus() async {
    var statusResult = await _authService.getCurrentStatus();

    statusResult.fold((l) => null, (status) => emit(status));
  }
}
