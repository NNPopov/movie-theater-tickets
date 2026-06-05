// Widget test for the 404 NotFoundView (slice 0002).
//
// Verifies the catch-all page renders the localized title/message and a
// "back home" action that returns the user to the Movies screen. Driven
// through the real AppRouter so the "back home" navigation effect is observable
// (the page reads context.router), mirroring the outside-in test harness.

import 'dart:async';

import 'package:dartz/dartz.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:get_it/get_it.dart';
import 'package:mocktail/mocktail.dart';

import 'package:movie_theater_tickets/core/errors/failures.dart';
import 'package:movie_theater_tickets/core/routing/app_router.dart';
import 'package:movie_theater_tickets/l10n/gen/app_localizations.dart';
import 'package:movie_theater_tickets/src/auth/domain/abstraction/auth_statuses.dart';
import 'package:movie_theater_tickets/src/auth/presentations/bloc/auth_cubit.dart';
import 'package:movie_theater_tickets/src/auth/presentations/bloc/auth_event.dart';
import 'package:movie_theater_tickets/src/globalisations_flutter/cubit/globalisation_cubit.dart';
import 'package:movie_theater_tickets/src/home/presentation/views/not_found_view.dart';
import 'package:movie_theater_tickets/src/hub/presentation/cubit/connectivity_bloc.dart';
import 'package:movie_theater_tickets/src/movie_sessions/domain/entities/active_movie.dart';
import 'package:movie_theater_tickets/src/movies/domain/usecases/get_movies.dart';
import 'package:movie_theater_tickets/src/movies/presentation/views/movie_view.dart';
import 'package:movie_theater_tickets/src/server_state/domain/entities/server_state.dart';
import 'package:movie_theater_tickets/src/server_state/presentation/cubit/server_state_cubit.dart';
import 'package:movie_theater_tickets/src/shopping_carts/presentation/cubit/shopping_cart_cubit.dart';
import 'package:movie_theater_tickets/src/theme_flutter/cubit/theme_cubit.dart';

class _MockGetActiveMovies extends Mock implements GetActiveMovies {}

class _StubAuthBloc extends Bloc<AuthEvent, AuthStatus> implements AuthBloc {
  _StubAuthBloc()
    : super(AuthStatus(status: AuthenticationStatus.unauthorized));
  @override
  dynamic noSuchMethod(Invocation invocation) => super.noSuchMethod(invocation);
}

class _StubServerStateCubit extends Cubit<ServerState>
    implements ServerStateCubit {
  _StubServerStateCubit() : super(ServerState.initState());
  @override
  dynamic noSuchMethod(Invocation invocation) => super.noSuchMethod(invocation);
}

class _StubConnectivityBloc extends Cubit<ConnectivityState>
    implements ConnectivityBloc {
  _StubConnectivityBloc() : super(DisconnectedState());
  @override
  dynamic noSuchMethod(Invocation invocation) => super.noSuchMethod(invocation);
}

class _StubShoppingCartCubit extends Cubit<ShoppingCartState>
    implements ShoppingCartCubit {
  _StubShoppingCartCubit() : super(ShoppingCartState.initState());
  @override
  dynamic noSuchMethod(Invocation invocation) => super.noSuchMethod(invocation);
}

void main() {
  final getIt = GetIt.instance;
  late _MockGetActiveMovies getActiveMovies;
  late AppRouter appRouter;

  setUp(() async {
    await getIt.reset();
    getActiveMovies = _MockGetActiveMovies();
    when(
      () => getActiveMovies(),
    ).thenAnswer((_) async => Right<Failure, List<ActiveMovie>>(const []));
    getIt.registerFactory<GetActiveMovies>(() => getActiveMovies);
    appRouter = AppRouter();
  });

  tearDown(() async {
    await getIt.reset();
  });

  Widget buildApp() {
    return MultiBlocProvider(
      providers: [
        BlocProvider<ThemeCubit>(create: (_) => ThemeCubit()),
        BlocProvider<GlobalisationCubit>(create: (_) => GlobalisationCubit()),
        BlocProvider<AuthBloc>(create: (_) => _StubAuthBloc()),
        BlocProvider<ServerStateCubit>(create: (_) => _StubServerStateCubit()),
        BlocProvider<ConnectivityBloc>(create: (_) => _StubConnectivityBloc()),
        BlocProvider<ShoppingCartCubit>(
          create: (_) => _StubShoppingCartCubit(),
        ),
      ],
      child: MaterialApp.router(
        localizationsDelegates: const [
          AppLocalizations.delegate,
          GlobalMaterialLocalizations.delegate,
          GlobalWidgetsLocalizations.delegate,
          GlobalCupertinoLocalizations.delegate,
        ],
        supportedLocales: AppLocalizations.supportedLocales,
        routerConfig: appRouter.config(),
      ),
    );
  }

  Future<void> pumpAt404(WidgetTester tester) async {
    tester.view.physicalSize = const Size(1400, 900);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);
    await tester.pumpWidget(buildApp());
    await tester.pumpAndSettle();
    // Not awaited: auto_route's pushNamed returns the route's pop completer
    // (resolves only on pop), so awaiting it would hang the test.
    unawaited(appRouter.pushPath('/totally-unknown-route'));
    await tester.pumpAndSettle();
  }

  testWidgets('renders the localized title, message and back-home action', (
    tester,
  ) async {
    await pumpAt404(tester);

    expect(find.byType(NotFoundView), findsOneWidget);
    expect(find.text('Page Not Found'), findsOneWidget);
    expect(
      find.text('The page you are looking for does not exist.'),
      findsOneWidget,
    );
    expect(find.widgetWithText(ElevatedButton, 'Back to Home'), findsOneWidget);
  });

  testWidgets('tapping back-home returns to the Movies screen', (tester) async {
    await pumpAt404(tester);

    await tester.tap(find.widgetWithText(ElevatedButton, 'Back to Home'));
    await tester.pumpAndSettle();

    expect(find.byType(MoviesView), findsOneWidget);
    expect(find.byType(NotFoundView), findsNothing);
  });
}
