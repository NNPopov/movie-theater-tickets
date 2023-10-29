import 'package:flutter/widgets.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:movie_theater_tickets/src/movie_sessions/domain/entities/movie_session.dart';
import 'package:movie_theater_tickets/src/shopping_carts/presentation/cubit/shopping_cart_cubit.dart';
import '../../src/auth/presentations/cubit/auth_cubit.dart';
import '../../src/hub/connectivity/connectivity_bloc.dart';
import '../buses/event_bus.dart';
import '../../src/movie_session_view.dart';
import '../../src/movie_sessions/presentation/cubit/movie_session_cubit.dart';
import '../../src/movie_view.dart';
import '../../src/movies/domain/entities/movie.dart';
import '../../src/movies/presentation/app/movie_theater_cubit.dart';
import '../../src/seats/domain/usecases/get_seats_by_movie_session_id.dart';
import '../../src/seats/presentation/cubit/seat_cubit.dart';
import '../../src/seats_view.dart';
import 'package:get_it/get_it.dart';

GetIt getIt = GetIt.instance;

Route<dynamic> generateRoute(RouteSettings settings) {
  if (settings.name == MoviesView.id || settings.name == '/') {
    return _pageBuilder(
      (_) => MultiBlocProvider(
        providers: [
          BlocProvider<AuthCubit>(create: (_) => AuthCubit()),
          BlocProvider(
            create: (_) => MovieTheaterCubit(),
          ),
        ],
        child: const MoviesView(),
      ),
      settings: settings,
    );
  } else if (settings.name == MovieSessionsView.id &&
      settings?.arguments != null) {
    return _pageBuilder(
      (_) => MultiBlocProvider(
        providers: [
          BlocProvider<AuthCubit>(create: (_) => AuthCubit()),
          BlocProvider(create: (_) => MovieSessionCubit()),
          BlocProvider<ConnectivityBloc>(create: (_) => ConnectivityBloc()),
        ],
        child: MovieSessionsView(settings.arguments! as Movie),
      ),
      settings: settings,
    );
  } else if (settings.name == SeatsView.id && settings?.arguments != null) {
    return _pageBuilder(
      (_) => MultiBlocProvider(
        providers: [
          BlocProvider<AuthCubit>(create: (_) => AuthCubit()),
          BlocProvider<ConnectivityBloc>(create: (_) => ConnectivityBloc()),
          BlocProvider<SeatCubit>(
            create: (context) => SeatCubit(
                getMovieSessionById: getIt.get<GetSeatsByMovieSessionId>(),
                eventBus: getIt.get<EventBus>()),
          ),
          BlocProvider<ShoppingCartCubit>(
            create: (context) => ShoppingCartCubit(),
          ),
        ],
        child: SeatsView(settings.arguments! as MovieSession),
      ),
      settings: settings,
    );
  }

  return _pageBuilder(
    (_) => BlocProvider(
      create: (_) => MovieTheaterCubit(),
      child: const MoviesView(),
    ),
    settings: settings,
  );
}

PageRouteBuilder<dynamic> _pageBuilder(
  Widget Function(BuildContext) page, {
  required RouteSettings settings,
}) {
  return PageRouteBuilder(
    settings: settings,
    transitionsBuilder: (_, animation, __, child) => FadeTransition(
      opacity: animation,
      child: child,
    ),
    pageBuilder: (context, __, ___) => page(context),
  );
}
