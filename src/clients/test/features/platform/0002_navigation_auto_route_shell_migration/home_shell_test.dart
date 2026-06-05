// Widget test for the persistent HomeShell (slice 0002).
//
// Verifies the shell builds with the top app bar and the three-item menu, that
// the nested AutoRouter renders the initial (Movies) child, and that the active
// menu item (Movies) is highlighted (bold) on first paint.
//
// Boundary note: the Movies route page resolves GetActiveMovies from get_it;
// it is registered as a mocktail fake returning an empty list so the screen
// settles without the network. The cross-cutting cubits the app bar reads are
// provided as inert stubs, exactly as in the outside-in acceptance test.

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
import 'package:movie_theater_tickets/src/dashboards/presentation/dashboard_widget.dart';
import 'package:movie_theater_tickets/src/globalisations_flutter/cubit/globalisation_cubit.dart';
import 'package:movie_theater_tickets/src/home/presentation/widgets/home_app_bar.dart';
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

  Future<void> pumpApp(WidgetTester tester) async {
    tester.view.physicalSize = const Size(1400, 900);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);
    await tester.pumpWidget(buildApp());
    await tester.pumpAndSettle();
  }

  testWidgets('shell builds with app bar, menu and the initial Movies child', (
    tester,
  ) async {
    await pumpApp(tester);

    // The persistent chrome is present.
    expect(find.byType(HomeAppBar), findsOneWidget);
    expect(find.byType(DashboardWidget), findsOneWidget);

    // The nested AutoRouter renders the initial child (Movies).
    expect(find.byType(MoviesView), findsOneWidget);
  });

  testWidgets('the active menu item (Movies) is highlighted on first paint', (
    tester,
  ) async {
    await pumpApp(tester);

    Text menuLabel(String label) => tester.widget<Text>(
      find.descendant(
        of: find.byType(DashboardWidget),
        matching: find.text(label),
      ),
    );

    // Movies is the active child → bold; the others are not.
    expect(menuLabel('Movies').style?.fontWeight, FontWeight.bold);
    expect(menuLabel('About Us').style?.fontWeight, FontWeight.normal);
    expect(menuLabel('Shopping Cart').style?.fontWeight, FontWeight.normal);
  });
}
