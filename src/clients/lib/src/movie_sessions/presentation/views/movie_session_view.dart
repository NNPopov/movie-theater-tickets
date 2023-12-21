import 'package:flutter/material.dart';
import 'package:carousel_slider/carousel_slider.dart';
import 'package:movie_theater_tickets/core/res/app_theme.dart';
import 'package:movie_theater_tickets/src/seats/presentation/views/seats_view.dart';
import '../../../../core/common/views/loading_view.dart';
import '../../../../core/common/views/no_data_view.dart';
import '../../../../core/res/app_styles.dart';
import '../../../../core/utils/utils.dart';
import '../widgets/auditorium_detail.dart';
import '../../../cinema_halls/presentation/cubit/cinema_hall_cubit.dart';
import '../../../dashboards/presentation/dashboard_widget.dart';
import '../../../movies/presentation/app/movie_cubit.dart';
import '../../../movies/presentation/widgets/movie_detail_widget.dart';
import '../../domain/entities/movie_session.dart';
import '../cubit/movie_session_bloc.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:get_it/get_it.dart';

import 'package:flutter_gen/gen_l10n/app_localizations.dart';

GetIt getIt = GetIt.instance;

class MovieSessionsView extends StatefulWidget {
  const MovieSessionsView(this.movieId, {super.key});

  final String movieId;
  static const id = '/movie-sessions';

  @override
  State<StatefulWidget> createState() => _MovieSessionsView();
}

class _MovieSessionsView extends State<MovieSessionsView> {
  CarouselController buttonCarouselController = CarouselController();

  void getMovieSessions() {
    context
        .read<MovieSessionBloc>()
        .add(MovieSessionEvent(movieId: widget.movieId));
  }

  late ScrollController _controller;

  @override
  void initState() {
    _controller = ScrollController();
    super.initState();
    getMovieSessions();
  }

  @override
  Widget build(BuildContext context) {
    return BlocConsumer<MovieSessionBloc, MovieSessionState>(
      listener: (context, state) {
        if (state.status == MovieSessionStateStatus.error) {
          Utils.showSnackBar(context, state.errorMessage!);
        }
      },
      builder: (context, state) {
        if (state.status == MovieSessionStateStatus.fetching ||
            state.status == MovieSessionStateStatus.initial) {
          return const LoadingView();
        }
        if ((state.status == MovieSessionStateStatus.loaded &&
            state.movieSession.isEmpty)) {
          return const NoDataView();
        }

        final movieSessions = state.movieSession;

        return BuildMovieSessions(movieSessions, context);
      },
    );
  }

  Widget BuildMovieSessions(
      List<List<List<MovieSession>>> movieSessionResult, BuildContext context) {
    double width = MediaQuery.of(context).size.width;

    if (width > 800) {
      return SingleChildScrollView(
        controller: _controller,
        child: Column(
          children: [
            const DashboardWidget(route: MovieSessionsView.id),
            Container(
              padding: const EdgeInsets.only(top: 20.0),
              child: Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  buildMovieDetail(),
                  const SizedBox(width: 15),
                  buildMovieSessions(width, movieSessionResult),
                ],
              ),
            ),
          ],
        ),
      );
    } else {
      return SingleChildScrollView(
        controller: _controller,
        child: Column(
          children: [
            const DashboardWidget(route: MovieSessionsView.id),
            Container(
              padding: const EdgeInsets.only(top: 20.0),
              child: Column(
                children: [
                  buildMovieDetail(),
                  buildMovieSessions(width + 700, movieSessionResult),
                ],
              ),
            ),
          ],
        ),
      );
    }
  }

  Column buildMovieSessions(
      double width, List<List<List<MovieSession>>> movieSessionResult) {
    width = width > 1250 ? 1250 : width;

    return Column(children: [
      Container(
          alignment: Alignment.bottomLeft,
          decoration: BoxDecoration(
            color: Theme.of(context).widgetColor,
            borderRadius: BorderRadius.circular(AppStyles.defaultRadius),
            border: Border.all(
              color: Theme.of(
                  context).defaultBorderColor,
              width: AppStyles.defaultBorderWidth,
            ),
          ),
          padding: EdgeInsets.all(20),
          width: width - 500,
          child: Container(
            alignment: Alignment.topLeft,
            child: ListView.builder(
                shrinkWrap: true,
                scrollDirection: Axis.vertical,
                itemCount: movieSessionResult.length,
                itemBuilder: (context, index) {
                  var rowSeats = movieSessionResult[index];
                  var day = rowSeats[0][0];

                  return SizedBox(
                    width: width - 600,
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

                              return SizedBox(
                                width: width - 700,
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
                                      color: Theme.of(
                                          context).defaultBorderColor,
                                    ),
                                    Wrap(
                                      spacing: 8.0,
                                      runSpacing: 4.0,
                                      children: rows.map((movieSession) {
                                        return SizedBox(
                                            height: 110,
                                            width: 200,
                                            child: Column(
                                              children: [
                                                Text(
                                                    '${movieSession.sessionDate.hour}:${'${movieSession.sessionDate.minute}0'.substring(0, 2)}'),
                                                TextButton(
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
                }),
          )),
      ElevatedButton(
        onPressed: () => buttonCarouselController.nextPage(
            duration: const Duration(milliseconds: 300), curve: Curves.linear),
        child: const Text('→'),
      )
    ]);
  }

  BlocProvider<MovieCubit> buildMovieDetail() {
    return BlocProvider(
        key: const ValueKey('MoviesDetailView'),
        create: (_) => MovieCubit(getIt.get()),
        child: MoviesDetailWidget(widget.movieId));
  }

  @override
  void dispose() {
    super.dispose();
  }

  void pressMovieSession(MovieSession movieSession) {
    Navigator.pushNamed(context, SeatsView.id, arguments: movieSession);
  }
}
