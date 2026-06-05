// Safety-net smoke test (Module G) for the auth flow.
//
// Guards the auth orchestration (AuthBloc -> AuthService) across the
// flutter_bloc 8->9 bump and the flutter_web_auth_2 / jwt_decoder /
// secure-storage adapter bumps: the bloc must start unauthorized and reach the
// authorized state when the (mocked) service authenticates. The concrete
// flutter_web_auth_2 / secure-storage path below AuthService is exercised by
// the manual web smoke run (PRD M10); here we mock at the AuthService seam so
// the test is deterministic and needs no browser. mocktail only — no new deps.

import 'dart:async';

import 'package:dartz/dartz.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:movie_theater_tickets/src/auth/domain/abstraction/auth_statuses.dart';
import 'package:movie_theater_tickets/src/auth/domain/services/auth_service.dart';
import 'package:movie_theater_tickets/src/auth/presentations/bloc/auth_cubit.dart';
import 'package:movie_theater_tickets/src/auth/presentations/bloc/auth_event.dart';

class _MockAuthService extends Mock implements AuthService {}

void main() {
  late _MockAuthService authService;
  late StreamController<AuthStatus> statusController;

  setUp(() {
    authService = _MockAuthService();
    statusController = StreamController<AuthStatus>.broadcast();
    when(() => authService.status).thenAnswer((_) => statusController.stream);
    when(() => authService.getCurrentStatus()).thenAnswer(
      (_) async => Right(AuthStatus(status: AuthenticationStatus.unauthorized)),
    );
  });

  tearDown(() => statusController.close());

  test('AuthBloc starts unauthorized', () {
    final bloc = AuthBloc(authService);
    addTearDown(bloc.close);

    expect(bloc.state.status, AuthenticationStatus.unauthorized);
  });

  test('AuthBloc reaches authorized after a successful login', () async {
    when(() => authService.logIn()).thenAnswer(
      (_) async => Right(AuthStatus(status: AuthenticationStatus.authorized)),
    );

    final bloc = AuthBloc(authService);
    addTearDown(bloc.close);

    final expectation = expectLater(
      bloc.stream,
      emitsInOrder([
        isA<AuthStatus>().having(
          (s) => s.status,
          'status',
          AuthenticationStatus.inProgress,
        ),
        isA<AuthStatus>().having(
          (s) => s.status,
          'status',
          AuthenticationStatus.authorized,
        ),
      ]),
    );

    bloc.add(LogInEvent());

    await expectation;
  });
}
