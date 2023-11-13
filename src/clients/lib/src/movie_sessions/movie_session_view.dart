import 'package:flutter/material.dart';
import 'package:carousel_slider/carousel_slider.dart';
import 'package:movie_theater_tickets/core/extensions/context_extensions.dart';
import 'package:movie_theater_tickets/src/seats_view.dart';
import '../../core/common/views/loading_view.dart';
import '../../core/utils/utils.dart';
import '../auditorium_detail.dart';
import '../auth/presentations/widgets/auth_widget.dart';
import '../cinema_halls/presentation/cubit/cinema_hall_cubit.dart';
import '../globalisations_flutter/widgets/globalisation_widget.dart';
import '../home/presentation/widgets/home_app_bar.dart';
import '../movies/presentation/views/movie_detail.dart';
import '../shopping_carts/presentation/widgens/shopping_cart_icon_widget.dart';
import 'domain/entities/movie_session.dart';
import 'presentation/cubit/movie_session_cubit.dart';
import '../movies/domain/entities/movie.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:get_it/get_it.dart';
import 'package:collection/collection.dart';

import '../movies/presentation/app/movie_theater_cubit.dart';
import 'package:flutter_gen/gen_l10n/app_localizations.dart';

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
    final movieSessionResult = groupBy(
            movieSessions,
            (movieSession) =>
                '${movieSession.sessionDate.year}${movieSession.sessionDate.month}${movieSession.sessionDate.day}')
        .values
        .map((seatsByDate) => groupBy(
                seatsByDate.toList(), (seatByDate) => seatByDate.cinemaHallId)
            .values
            .map((seatsBycinemaHallId) => seatsBycinemaHallId.toList()
              ..sort((a, b) => a.sessionDate.compareTo(b.sessionDate)))
            .toList()
          ..sort((a, b) => -a[0].cinemaHallId.compareTo(b[0].cinemaHallId)))
        .toList() ..sort((a, b) => a[0][0].sessionDate.compareTo(b[0][0].sessionDate));

    return Scaffold(
      backgroundColor: Colors.white,
      appBar:  const HomeAppBar(),
      body: Row(
        children: [
          BlocProvider(
              key: const ValueKey('MoviesDetailView'),
              create: (_) => MovieTheaterCubit(),
              child: MoviesDetailView(widget.movie.id)),
          Column(children: [
            SizedBox(
                height: 40,
                width: 100,
                child: Text(AppLocalizations.of(context)!.movies)),
            SizedBox(
             //   height: 1950,
                width: 1100,
                child: ListView.builder(
                    shrinkWrap: true,
                    scrollDirection: Axis.vertical,
                    itemCount: movieSessionResult.length,
                    itemBuilder: (context, index) {
                      var rowSeats = movieSessionResult[index];
                      var day = rowSeats[0][0];

                      return Container(
                        width: 900,
                        //height: 200,
                        child: Column(
                          children: [
                            Text(
                                '${day.sessionDate.year}-${day.sessionDate.month}-${day.sessionDate.day}'),
                            ListView.builder(
                                shrinkWrap: true,
                                scrollDirection: Axis.vertical,
                                itemCount: rowSeats.length,
                                itemBuilder: (context, index2) {
                                  var rows = rowSeats[index2];

                                  return Container(
                                    width: 800,
                                   // height: 240,
                                    child: Column(
                                      children: [
                                        BlocProvider(
                                            key: const ValueKey(
                                                'AuditoriumDetailView'),
                                            create: (_) => CinemaHallCubit(),
                                            child: AuditoriumDetailView(
                                                rows[0].cinemaHallId)),
                                        Divider(
                                          color: Colors.blue,
                                        ),
                                        Wrap(
                                          spacing: 8.0,
                                          // горизонтальный отступ между элементами
                                          runSpacing: 4.0,
                                          // вертикальный отступ между строками
                                          children: rows.map((movieSession) {
                                            return SizedBox(
                                                height: 110,
                                                width: 200,
                                                child: Column(
                                                  children: [
                                                    Text(
                                                        '${movieSession.sessionDate.hour}:${'${movieSession.sessionDate.minute}0'.substring(0, 2)}'),
                                                    TextButton(
                                                        style: ButtonStyle(
                                                          padding: MaterialStateProperty
                                                              .all(const EdgeInsets
                                                                  .symmetric(
                                                                  vertical: 1,
                                                                  horizontal:
                                                                      1)),
                                                          foregroundColor:
                                                              MaterialStateProperty
                                                                  .all<Color>(
                                                                      Colors
                                                                          .blue),
                                                        ),
                                                        onPressed: () {
                                                          pressMovieSession(
                                                              movieSession);
                                                        },
                                                        child: Text(
                                                            AppLocalizations.of(
                                                                    context)!
                                                                .select))
                                                  ],
                                                ));
                                          }).toList(),
                                        ),
                                      ],
                                    ),
                                  );
                                }),
                          ],
                        ),
                      );
                    })),
            ElevatedButton(
              onPressed: () => buttonCarouselController.nextPage(
                  duration: Duration(milliseconds: 300), curve: Curves.linear),
              child: Text('→'),
            )
          ]),
        ],
      ),
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
