// Safety-net widget smoke test (Module G) for the Movie-session screen.
//
// Guards the carousel-bearing Movie-session screen across the flutter_bloc
// 8->9 bump: it must still build and surface its loading state while the
// (mocked) GetMovieSessions use-case is in flight. Uses the real
// MovieSessionBloc with a mocktail-mocked use-case — no bloc_test package and
// no new dependencies.

import 'dart:async';

import 'package:dartz/dartz.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:movie_theater_tickets/core/common/views/loading_view.dart';
import 'package:movie_theater_tickets/core/errors/failures.dart';
import 'package:movie_theater_tickets/src/movie_sessions/domain/entities/movie_session.dart';
import 'package:movie_theater_tickets/src/movie_sessions/domain/usecase/get_movie_sessions.dart';
import 'package:movie_theater_tickets/src/movie_sessions/presentation/cubit/movie_session_bloc.dart';
import 'package:movie_theater_tickets/src/movie_sessions/presentation/views/movie_session_view.dart';

class _MockGetMovieSessions extends Mock implements GetMovieSessions {}

void main() {
  late _MockGetMovieSessions getMovieSessions;
  late MovieSessionBloc bloc;
  late Completer<Either<Failure, List<List<List<MovieSession>>>>> pending;

  setUp(() {
    getMovieSessions = _MockGetMovieSessions();
    pending = Completer<Either<Failure, List<List<List<MovieSession>>>>>();
    // Keep the fetch in flight so the screen stays in its loading state.
    when(() => getMovieSessions(any())).thenAnswer((_) => pending.future);
    bloc = MovieSessionBloc(getMovieSessions);
  });

  tearDown(() => bloc.close());

  testWidgets('Movie-session screen builds and shows its loading state', (
    tester,
  ) async {
    await tester.pumpWidget(
      MaterialApp(
        home: BlocProvider<MovieSessionBloc>.value(
          value: bloc,
          child: const MovieSessionsView('movie-1'),
        ),
      ),
    );

    expect(find.byType(LoadingView), findsOneWidget);
    expect(find.byType(CircularProgressIndicator), findsOneWidget);
  });
}
