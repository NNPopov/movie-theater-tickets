// Outside-in acceptance test for slice 0002_navigation_auto_route_shell_migration.
//
// This is a *migration* slice whose public surface is NAVIGATION BEHAVIOR, not a
// Cubit method. So — exactly like the precedent migration test
// 0001_flutter_dart_deps_migration/flutter_dart_deps_migration_outside_in_test.dart
// (which gates at the storage level, not a Cubit) — this test gates at the
// router + widget level: it pumps the real `AppRouter` shell and drives navigation
// through the tester, asserting externally observable outcomes (which screen is
// shown, whether a duplicate appears, whether the stack grows, whether 404 shows).
// It never asserts auto_route internals.
//
// Spec: specs/features/platform/0002_navigation_auto_route_shell_migration/tests.md
//
// Expected RED at the time of writing: the slice is not implemented. `auto_route`
// is not yet a dependency and none of `AppRouter`, `HomeShell`, `NotFoundView`, or
// the generated `*Route` classes exist, so this file does not compile. It turns
// GREEN only once the shell migration is implemented per plan.md.
//
// Boundary note: the only system boundary mocked is the Movies screen's
// `GetActiveMovies` use-case (registered in get_it so the route page builds without
// the network). The persistent app bar pulls in several cross-cutting cubits; those
// are provided as inert, side-effect-free stubs so the shell renders deterministically.
// Navigation is observable regardless of each screen's data state.

import 'dart:async';

import 'package:auto_route/auto_route.dart';
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
import 'package:movie_theater_tickets/src/about/presentation/views/shopping_cart_view.dart';
import 'package:movie_theater_tickets/src/auth/domain/abstraction/auth_statuses.dart';
import 'package:movie_theater_tickets/src/auth/presentations/bloc/auth_cubit.dart';
import 'package:movie_theater_tickets/src/auth/presentations/bloc/auth_event.dart';
import 'package:movie_theater_tickets/src/dashboards/presentation/dashboard_widget.dart';
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

// Inert, side-effect-free stubs for the cross-cutting cubits the app bar reads.
// They expose only the initial state the real cubit starts with; every other
// member is routed through noSuchMethod (never exercised by these scenarios).
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
    // System boundary: Movies route page resolves GetActiveMovies from get_it.
    // Return an empty list so the screen settles without touching the network.
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

  // Tap a menu item by its visible label, scoped to the persistent menu so the
  // tap never matches a same-named label elsewhere on the screen.
  Future<void> tapMenuItem(WidgetTester tester, String label) async {
    final finder = find.descendant(
      of: find.byType(DashboardWidget),
      matching: find.text(label),
    );
    expect(finder, findsOneWidget);
    await tester.tap(finder);
    await tester.pumpAndSettle();
  }

  Future<void> pumpApp(WidgetTester tester) async {
    // Give the persistent app bar + menu a wide surface so the row layout fits.
    tester.view.physicalSize = const Size(1400, 900);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    await tester.pumpWidget(buildApp());
    await tester.pumpAndSettle();
  }

  testWidgets(
    'tapping the already-active menu item does not duplicate the screen',
    (tester) async {
      await pumpApp(tester);

      // App starts on Movies (initial child of the shell).
      expect(find.byType(MoviesView), findsOneWidget);

      // Tap the menu item for the screen we are already on.
      await tapMenuItem(tester, 'Movies');

      // No second instance was pushed on top of itself.
      expect(find.byType(MoviesView), findsOneWidget);
    },
  );

  testWidgets('bouncing Movies <-> About does not grow the stack without bound', (
    tester,
  ) async {
    await pumpApp(tester);

    for (var i = 0; i < 5; i++) {
      await tapMenuItem(tester, 'About Us');
      await tapMenuItem(tester, 'Movies');
    }

    // End on About to inspect the steady state.
    await tapMenuItem(tester, 'About Us');

    // Switching is navigate/replace, not push: at no point do duplicate screen
    // instances accumulate. Exactly the current screen is present, and the one
    // we navigated away from is gone.
    expect(find.byType(AboutUsView), findsOneWidget);
    expect(find.byType(MoviesView), findsNothing);

    // The shell's child stack is bounded: a single back action is enough to
    // leave the current child (it did not pile up one entry per tap).
    expect(appRouter.canPop(), isFalse);
  });

  testWidgets('an unknown route renders the 404 page, not Movies', (
    tester,
  ) async {
    await pumpApp(tester);

    // NOTE: not awaited — auto_route's pushNamed returns the route's pop
    // completer (it resolves only when the pushed route is popped), so awaiting
    // it would suspend the test body forever. Driving the frames via
    // pumpAndSettle is what makes the navigation observable.
    unawaited(appRouter.pushNamed('/totally-unknown-route'));
    await tester.pumpAndSettle();

    expect(find.byType(NotFoundView), findsOneWidget);
    expect(find.byType(MoviesView), findsNothing);
  });
}
