import 'dart:async';
import 'package:bloc/bloc.dart';
import '../../domain/abstraction/auth_statuses.dart';
import '../../domain/services/auth_service.dart';
import 'auth_event.dart';

class AuthBloc extends Bloc<AuthEvent, AuthStatus> {
  AuthBloc(this._authService)
      : super(AuthStatus(status: AuthenticationStatus.unauthorized)) {
    init();
    on<LogInEvent>(_onAuthenticationLogInRequested);
    on<LogOutEvent>(_onAuthenticationLogoutRequested);

    _getAuthStatus();
  }

  late final StreamSubscription<AuthStatus> _authenticationStatusSubscription;

  final AuthService _authService;

  Future<void> init() async {
    _authenticationStatusSubscription =
        _authService.status.listen((status) async {
      emit(status);
    });

    print('AuthCubit initialized');
  }

  Future<void> _getAuthStatus() async {
    var statusResult = await _authService.getCurrentStatus();

    statusResult.fold((l) => null, (status) => {}
        //emit(status)
        );
  }

  @override
  Future<void> close() {
    _authenticationStatusSubscription.cancel();
    return super.close();
  }

  Future<void> _onAuthenticationLogInRequested(
      LogInEvent event, Emitter<AuthStatus> emit) async {
    emit(AuthStatus(status: AuthenticationStatus.inProgress));

    var result = await _authService.logIn();
    result.fold(
        (l) => emit(AuthStatus(status: AuthenticationStatus.unauthorized)),
        (r) => emit(r));
  }

  Future<void> _onAuthenticationLogoutRequested(
      LogOutEvent event, Emitter<AuthStatus> emit) async {
    emit(AuthStatus(status: AuthenticationStatus.inProgress));

    var result = await _authService.logOut();
    result.fold(
        (l) => emit(AuthStatus(status: AuthenticationStatus.unauthorized)),
        (r) => emit(r));
  }
}
