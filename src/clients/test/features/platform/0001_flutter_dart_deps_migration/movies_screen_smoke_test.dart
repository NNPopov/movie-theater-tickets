// Safety-net widget smoke test (Module G) for the Movies screen.
//
// Guards the carousel-bearing Movies screen across the flutter_bloc 8->9 bump:
// the screen must still build and surface its loading state while the
// (mocked) GetActiveMovies use-case is in flight. Uses the real
// MovieTheaterCubit with a mocktail-mocked use-case — no bloc_test package
// (not a dependency) and no new dependencies.

import 'dart:async';

import 'package:dartz/dartz.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:movie_theater_tickets/core/common/views/loading_view.dart';
import 'package:movie_theater_tickets/core/errors/failures.dart';
import 'package:movie_theater_tickets/src/movie_sessions/domain/entities/active_movie.dart';
import 'package:movie_theater_tickets/src/movie_sessions/presentation/cubit/movie_theater_cubit.dart';
import 'package:movie_theater_tickets/src/movies/domain/usecases/get_movies.dart';
import 'package:movie_theater_tickets/src/movies/presentation/views/movie_view.dart';

class _MockGetActiveMovies extends Mock implements GetActiveMovies {}

void main() {
  late _MockGetActiveMovies getActiveMovies;
  late MovieTheaterCubit cubit;
  late Completer<Either<Failure, List<ActiveMovie>>> pending;

  setUp(() {
    getActiveMovies = _MockGetActiveMovies();
    pending = Completer<Either<Failure, List<ActiveMovie>>>();
    // Keep the fetch in flight so the screen stays in its loading state.
    when(() => getActiveMovies()).thenAnswer((_) => pending.future);
    cubit = MovieTheaterCubit(getActiveMovies);
  });

  tearDown(() => cubit.close());

  testWidgets('Movies screen builds and shows its loading state', (
    tester,
  ) async {
    await tester.pumpWidget(
      MaterialApp(
        home: BlocProvider<MovieTheaterCubit>.value(
          value: cubit,
          child: const MoviesView(),
        ),
      ),
    );

    expect(find.byType(LoadingView), findsOneWidget);
    expect(find.byType(CircularProgressIndicator), findsOneWidget);
  });
}
