import 'package:flutter/material.dart';
import 'package:movie_theater_tickets/src/seats/presentation/widgets/seats_movie_session_widget.dart';
import 'package:movie_theater_tickets/src/shopping_carts/presentation/widgens/shopping_cart_widget.dart';
import '../../../auditorium_detail.dart';
import '../../../cinema_halls/presentation/cubit/cinema_hall_cubit.dart';
import '../../../dashboards/presentation/dashboard_widget.dart';
import '../../../movie_sessions/domain/entities/movie_session.dart';
import 'package:get_it/get_it.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import '../../../movies/presentation/app/movie_cubit.dart';
import '../../../movies/presentation/widgets/movie_detail_widget.dart';

GetIt getIt = GetIt.instance;

class SeatsView extends StatefulWidget {
  const SeatsView(this.movieSession, {super.key});

  final MovieSession movieSession;
  static const id = '/seats';

  @override
  State<StatefulWidget> createState() => _SeatsView();
}

class _SeatsView extends State<SeatsView> {
  @override
  void initState() {
    super.initState();
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        const DashboardWidget(route: SeatsView.id),
        LayoutBuilder(builder: (context, constraint) {
          return SingleChildScrollView(
            scrollDirection: Axis.horizontal,
            child: ConstrainedBox(
              constraints: BoxConstraints(
                  maxWidth:
                      constraint.maxWidth > 880 ? constraint.maxWidth : 880,
                  minWidth: 870),
              child: IntrinsicHeight(
                child: SizedBox(
                  height: 700,
                  child: Row(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const SizedBox(
                        width: 40,
                      ),
                      Column(
                        children: [
                          Container(
                              height: 110,
                              width: 320,
                              alignment: Alignment.topLeft,
                              decoration: BoxDecoration(
                                borderRadius: BorderRadius.circular(10),
                                border: Border.all(
                                  color: Colors.blue,
                                  width: 2,
                                ),
                              ),
                              margin: const EdgeInsets.all(5.0),
                              padding: const EdgeInsets.all(20.0),
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  BlocProvider(
                                      key: const ValueKey(
                                          'AuditoriumDetailView'),
                                      create: (_) => CinemaHallCubit(),
                                      child: AuditoriumDetailView(
                                          widget.movieSession.cinemaHallId)),
                                  Text(
                                      '${widget.movieSession.sessionDate.year}-${widget.movieSession.sessionDate.month}-${widget.movieSession.sessionDate.day}'),
                                  Text(
                                      '${widget.movieSession.sessionDate.hour}:${'${widget.movieSession.sessionDate.minute}0'.substring(0, 2)}')
                                ],
                              )),
                          BlocProvider(
                              key: const ValueKey('MoviesDetailView'),
                              create: (_) => MovieCubit(getIt.get()),
                              child: MoviesDetailWidget(
                                  widget.movieSession.movieId)),
                        ],
                      ),
                      const SizedBox(
                        width: 40,
                      ),
                      Expanded(
                        child: Align(
                          alignment: Alignment.topCenter,
                          child: SeatsMovieSessionWidget(
                              movieSession: widget.movieSession,
                              getCinemaHallInfo: getIt.get()),
                        ),
                      ),
                      const ShoppingCartWidget()
                    ],
                  ),
                ),
              ),
            ),
          );
        }),
      ],
    );
  }

  @override
  void dispose() {
    super.dispose();
  }
}
