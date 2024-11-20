import 'package:flutter/material.dart';
import 'package:flutter/widgets.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:movie_theater_tickets/src/movie_sessions/domain/entities/movie_session.dart';
import '../../src/about/presentation/views/shopping_cart_view.dart';
import '../../src/cinema_halls/presentation/cubit/movie_cubit.dart';
import '../../src/shopping_carts/presentation/views/shopping_cart_view.dart';
import '../../src/movie_sessions/presentation/views/movie_session_view.dart';
import '../../src/movie_sessions/presentation/cubit/movie_session_bloc.dart';
import '../../src/movies/presentation/views/movie_view.dart';
import '../../src/movie_sessions/presentation/cubit/movie_theater_cubit.dart';
import '../../src/seats/presentation/cubit/seat_cubit.dart';
import '../../src/seats/presentation/views/seats_view.dart';
import 'package:get_it/get_it.dart';

// final log = Logger('ExampleLogger');

GetIt getIt = GetIt.instance;

Route<dynamic> generateRoute(RouteSettings settings) {
  //log.info(settings.name);

  print(settings.name);

  if (settings.name == MoviesView.id || settings.name == '/') {
    return _pageBuilder(
      (_) => MultiBlocProvider(
        providers: [
          BlocProvider(
            create: (_) => MovieTheaterCubit(getIt.get()),
          ),
        ],
        child: const MoviesView(),
      ),
      settings: settings,
    );
  } else if (settings.name == MovieSessionsView.id &&
      settings.arguments != null) {
    return _pageBuilder(
      (_) => MultiBlocProvider(
        providers: [
          BlocProvider(create: (_) => MovieSessionBloc(getIt.get())),
        ],
        child: MovieSessionsView(settings.arguments! as String),
      ),
      settings: settings,
    );
  } else if (settings.name != null &&
      settings.name!.contains(SeatsView.id) &&
      settings.arguments != null) {
    return _pageBuilder(
      (_) => MultiBlocProvider(
        providers: [
          BlocProvider<SeatBloc>(
            create: (context) => SeatBloc(getIt.get(), getIt.get()),
          ),
          BlocProvider<CinemaHallInfoBloc>(
              create: (context) => CinemaHallInfoBloc(getIt.get()))
        ],
        child: SeatsView(settings.arguments! as MovieSession),
      ),
      settings: settings,
    );
  } else if (settings.name == ShoppingCartView.id) {
    return _pageBuilder(
      (_) => const ShoppingCartView(),
      settings: settings,
    );
  } else if (settings.name == AboutUsView.id) {
    return _pageBuilder(
      (_) => const AboutUsView(),
      settings: settings,
    );
  }

  return _pageBuilder(
    (_) => MultiBlocProvider(
      providers: [
        BlocProvider(
          create: (_) => MovieTheaterCubit(getIt.get()),
        ),
      ],
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
    transitionsBuilder: (_, animation, __, child) {

      var begin = Offset(0.0, 1.0);
      var end = Offset.zero;
      var curve = Curves.ease;
      var tween = Tween(begin: begin, end: end).chain(CurveTween(curve: curve));
      var offsetAnimation = animation.drive(tween);

      return SlideTransition(
        position: offsetAnimation,
        child: child,
      );

    // return  FadeTransition(
    //     opacity: animation,
    //     child: child,
    //   );
    },
    pageBuilder: (context, __, ___) =>
  page(context),
  );
}
