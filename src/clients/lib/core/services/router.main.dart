import 'package:flutter/widgets.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import '../../src/movie_session_view.dart';
import '../../src/movie_sessions/presentation/cubit/movie_session_cubit.dart';
import '../../src/movie_view.dart';
import '../../src/movies/domain/entities/movie.dart';
import '../../src/movies/presentation/app/movie_theater_cubit.dart';
import '../common/views/page_under_construction.dart';
import 'package:get_it/get_it.dart';

GetIt getIt = GetIt.instance;

Route<dynamic> generateRoute(RouteSettings settings) {
  switch (settings.name) {
    case '/':
    case MoviesView.id:
      return _pageBuilder(
            (_) => BlocProvider(
          create: (_) => getIt<MovieTheaterCubit>(),
          child: const MoviesView(),
        ),
        settings: settings,
      );
    case MovieSessionsView.id:
      return _pageBuilder(
            (_) => BlocProvider(
          create: (_) => getIt<MovieSessionCubit>(),
          child: MovieSessionsView(settings.arguments! as Movie),
        ),
        settings: settings,
      );
    default:
      return _pageBuilder(
            (_) => const PageUnderConstruction(),
        settings: settings,
      );
  }
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