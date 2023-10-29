import 'package:flutter/material.dart';
import 'package:carousel_slider/carousel_slider.dart';
import 'package:movie_theater_tickets/core/extensions/context_extensions.dart';
import 'package:movie_theater_tickets/src/seats_view.dart';
import '../core/common/views/loading_view.dart';
import '../core/utils/utils.dart';
import 'auditorium_detail.dart';
import 'auth/presentations/widgets/auth_widget.dart';
import 'cinema_halls/presentation/cubit/cinema_hall_cubit.dart';
import 'movie_detail.dart';
import 'movie_sessions/domain/entities/movie_session.dart';
import 'movie_sessions/presentation/cubit/movie_session_cubit.dart';
import 'movies/domain/entities/movie.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:get_it/get_it.dart';

import 'movies/presentation/app/movie_theater_cubit.dart';

GetIt getIt = GetIt.instance;

class MovieSessionsView extends StatefulWidget {
  const MovieSessionsView(this.movie, {super.key});

  final Movie movie;
  static const id = '/movie-sessions';

  @override
  State<StatefulWidget> createState() => _MovieSessionsView();
}

class _MovieSessionsView extends State<MovieSessionsView> {
  CarouselController buttonCarouselController = CarouselController();

  void getMovieSessions() {
    context.read<MovieSessionCubit>().getMovieSessions(widget.movie.id);
  }

  @override
  void initState() {
    //completeExam = widget.exam;
    super.initState();
    getMovieSessions();
  }

  @override
  Widget build(BuildContext context) {
    return BlocConsumer<MovieSessionCubit, MovieSessionState>(
      listener: (context, state) {
        if (state is MovieSessionError) {
          Utils.showSnackBar(context, state.message);
        }
      },
      builder: (context, state) {
        if (state is! MovieSessionsLoaded && state is! MovieSessionError) {
          return const LoadingView();
        }
        if ((state is MovieSessionsLoaded && state.movieSession.isEmpty) ||
            state is MovieSessionError) {
          return Center(
            child: Text(
              'No courses found\nPlease contact '
              'admin or if you are admin, add courses',
              textAlign: TextAlign.center,
              style: context.theme.textTheme.headlineMedium?.copyWith(
                fontWeight: FontWeight.w600,
                color: Colors.grey.withOpacity(0.5),
              ),
            ),
          );
        }

        state as MovieSessionsLoaded;

        final movieSessions = state.movieSession
          ..sort((a, b) => b.sessionDate.compareTo(a.sessionDate));

        return BuildMovieSessions(movieSessions, context);
      },
    );
  }

  Widget BuildMovieSessions(
      List<MovieSession> movieSessions, BuildContext context) {
    return Scaffold(
      backgroundColor: Colors.white,
      appBar: AppBar(
        title: const Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Expanded(
              child: Text("Movie session"),
            ),
            AuthWidget(),
          ],
        ),
      ),
      body: Column(children: [
        SizedBox(height: 40, width: 100, child: Text('Movies')),
        SizedBox(
            height: 250,
            width: 1000,
            child: CarouselSlider(
              items: movieSessions.map((movieSession) {
                return SizedBox(
                    height: 250,
                    width: 450,
                    child: Column(
                      children: [
                        BlocProvider(
                            key: const ValueKey('MoviesDetailView'),
                            create: (_) => MovieTheaterCubit(),
                            child: MoviesDetailView(movieSession.movieId)),
                        BlocProvider(
                            key: const ValueKey('AuditoriumDetailView'),
                            create: (_) => CinemaHallCubit(),
                            child: AuditoriumDetailView(
                                movieSession.auditoriumId)),
                        // Text('auditoriumId: ${movieSession.auditoriumId}'),
                        Text('Session Date: ${movieSession.sessionDate}'),
                        TextButton(
                            style: ButtonStyle(
                              padding: MaterialStateProperty.all(
                                  const EdgeInsets.symmetric(
                                      vertical: 1, horizontal: 1)),
                              foregroundColor:
                                  MaterialStateProperty.all<Color>(Colors.blue),
                            ),
                            onPressed: () {
                               pressMovieSession(movieSession);
                            },
                            child: const Text('Select'))
                      ],
                    ));
              }).toList(),
              carouselController: buttonCarouselController,
              options: CarouselOptions(
                  height: 300.0,
                  enableInfiniteScroll: true,
                  viewportFraction: 0.4,
                  enlargeCenterPage: false,
                  aspectRatio: 3.0),
            )),
        ElevatedButton(
          onPressed: () => buttonCarouselController.nextPage(
              duration: Duration(milliseconds: 300), curve: Curves.linear),
          child: Text('→'),
        )
      ]),
    );
  }

  @override
  void dispose() {
    super.dispose();
  }

  void pressMovieSession(MovieSession movieSession) {

    Navigator.pushNamed(context, SeatsView.id, arguments: movieSession);


  }
}
