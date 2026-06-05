// dart format width=80
// GENERATED CODE - DO NOT MODIFY BY HAND

// **************************************************************************
// AutoRouterGenerator
// **************************************************************************

// ignore_for_file: type=lint
// coverage:ignore-file

// ignore_for_file: no_leading_underscores_for_library_prefixes
import 'package:auto_route/auto_route.dart' as _i8;
import 'package:flutter/material.dart' as _i9;
import 'package:movie_theater_tickets/src/about/presentation/views/shopping_cart_view.dart'
    as _i1;
import 'package:movie_theater_tickets/src/home/presentation/home_shell.dart'
    as _i2;
import 'package:movie_theater_tickets/src/home/presentation/views/not_found_view.dart'
    as _i5;
import 'package:movie_theater_tickets/src/movie_sessions/domain/entities/movie_session.dart'
    as _i10;
import 'package:movie_theater_tickets/src/movie_sessions/presentation/views/movie_session_view.dart'
    as _i3;
import 'package:movie_theater_tickets/src/movies/presentation/views/movie_view.dart'
    as _i4;
import 'package:movie_theater_tickets/src/seats/presentation/views/seats_view.dart'
    as _i6;
import 'package:movie_theater_tickets/src/shopping_carts/presentation/views/shopping_cart_view.dart'
    as _i7;

/// generated route for
/// [_i1.AboutPage]
class AboutRoute extends _i8.PageRouteInfo<void> {
  const AboutRoute({List<_i8.PageRouteInfo>? children})
    : super(AboutRoute.name, initialChildren: children);

  static const String name = 'AboutRoute';

  static _i8.PageInfo page = _i8.PageInfo(
    name,
    builder: (data) {
      return const _i1.AboutPage();
    },
  );
}

/// generated route for
/// [_i2.HomeShell]
class HomeRoute extends _i8.PageRouteInfo<void> {
  const HomeRoute({List<_i8.PageRouteInfo>? children})
    : super(HomeRoute.name, initialChildren: children);

  static const String name = 'HomeRoute';

  static _i8.PageInfo page = _i8.PageInfo(
    name,
    builder: (data) {
      return const _i2.HomeShell();
    },
  );
}

/// generated route for
/// [_i3.MovieSessionsPage]
class MovieSessionsRoute extends _i8.PageRouteInfo<MovieSessionsRouteArgs> {
  MovieSessionsRoute({
    required String movieId,
    _i9.Key? key,
    List<_i8.PageRouteInfo>? children,
  }) : super(
         MovieSessionsRoute.name,
         args: MovieSessionsRouteArgs(movieId: movieId, key: key),
         initialChildren: children,
       );

  static const String name = 'MovieSessionsRoute';

  static _i8.PageInfo page = _i8.PageInfo(
    name,
    builder: (data) {
      final args = data.argsAs<MovieSessionsRouteArgs>();
      return _i3.MovieSessionsPage(movieId: args.movieId, key: args.key);
    },
  );
}

class MovieSessionsRouteArgs {
  const MovieSessionsRouteArgs({required this.movieId, this.key});

  final String movieId;

  final _i9.Key? key;

  @override
  String toString() {
    return 'MovieSessionsRouteArgs{movieId: $movieId, key: $key}';
  }

  @override
  bool operator ==(Object other) {
    if (identical(this, other)) return true;
    if (other is! MovieSessionsRouteArgs) return false;
    return movieId == other.movieId && key == other.key;
  }

  @override
  int get hashCode => movieId.hashCode ^ key.hashCode;
}

/// generated route for
/// [_i4.MoviesPage]
class MoviesRoute extends _i8.PageRouteInfo<void> {
  const MoviesRoute({List<_i8.PageRouteInfo>? children})
    : super(MoviesRoute.name, initialChildren: children);

  static const String name = 'MoviesRoute';

  static _i8.PageInfo page = _i8.PageInfo(
    name,
    builder: (data) {
      return const _i4.MoviesPage();
    },
  );
}

/// generated route for
/// [_i5.NotFoundView]
class NotFoundRoute extends _i8.PageRouteInfo<void> {
  const NotFoundRoute({List<_i8.PageRouteInfo>? children})
    : super(NotFoundRoute.name, initialChildren: children);

  static const String name = 'NotFoundRoute';

  static _i8.PageInfo page = _i8.PageInfo(
    name,
    builder: (data) {
      return const _i5.NotFoundView();
    },
  );
}

/// generated route for
/// [_i6.SeatsPage]
class SeatsRoute extends _i8.PageRouteInfo<SeatsRouteArgs> {
  SeatsRoute({
    required _i10.MovieSession movieSession,
    _i9.Key? key,
    List<_i8.PageRouteInfo>? children,
  }) : super(
         SeatsRoute.name,
         args: SeatsRouteArgs(movieSession: movieSession, key: key),
         initialChildren: children,
       );

  static const String name = 'SeatsRoute';

  static _i8.PageInfo page = _i8.PageInfo(
    name,
    builder: (data) {
      final args = data.argsAs<SeatsRouteArgs>();
      return _i6.SeatsPage(movieSession: args.movieSession, key: args.key);
    },
  );
}

class SeatsRouteArgs {
  const SeatsRouteArgs({required this.movieSession, this.key});

  final _i10.MovieSession movieSession;

  final _i9.Key? key;

  @override
  String toString() {
    return 'SeatsRouteArgs{movieSession: $movieSession, key: $key}';
  }

  @override
  bool operator ==(Object other) {
    if (identical(this, other)) return true;
    if (other is! SeatsRouteArgs) return false;
    return movieSession == other.movieSession && key == other.key;
  }

  @override
  int get hashCode => movieSession.hashCode ^ key.hashCode;
}

/// generated route for
/// [_i7.ShoppingCartPage]
class ShoppingCartRoute extends _i8.PageRouteInfo<void> {
  const ShoppingCartRoute({List<_i8.PageRouteInfo>? children})
    : super(ShoppingCartRoute.name, initialChildren: children);

  static const String name = 'ShoppingCartRoute';

  static _i8.PageInfo page = _i8.PageInfo(
    name,
    builder: (data) {
      return const _i7.ShoppingCartPage();
    },
  );
}
