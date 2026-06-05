import 'package:auto_route/auto_route.dart';
import 'package:flutter/material.dart';
import 'package:movie_theater_tickets/core/res/app_theme.dart';
import 'package:movie_theater_tickets/core/routing/app_router.gr.dart';
import 'package:movie_theater_tickets/src/seats/presentation/cubit/seat_cubit.dart';
import 'package:movie_theater_tickets/src/seats/presentation/cubit/seat_layout_cubit.dart';
import 'package:movie_theater_tickets/src/seats/presentation/widgets/seat_map_view.dart';
import 'package:movie_theater_tickets/src/shopping_carts/presentation/widgens/shopping_cart_widget.dart';
import '../../../../core/res/app_styles.dart';
import '../../../movie_sessions/presentation/widgets/auditorium_detail.dart';
import '../../../cinema_halls/presentation/cubit/cinema_hall_cubit.dart';
import '../../../movie_sessions/domain/entities/movie_session.dart';
import 'package:get_it/get_it.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import '../../../movies/presentation/app/movie_cubit.dart';
import '../../../movies/presentation/widgets/movie_detail_widget.dart';
import 'package:movie_theater_tickets/l10n/gen/app_localizations.dart';

GetIt getIt = GetIt.instance;

/// Route page for the Seats screen.
///
/// Reproduces the per-route [SeatBloc] provider from the legacy `generateRoute`
/// and adds the [SeatLayoutCubit] geometry loader; carries the required typed
/// [movieSession]. The geometry now arrives through the `SeatLayoutSource` port,
/// so the legacy `CinemaHallInfoBloc` provider is no longer mounted here.
@RoutePage(name: 'SeatsRoute')
class SeatsPage extends StatelessWidget {
  const SeatsPage({required this.movieSession, super.key});

  final MovieSession movieSession;

  @override
  Widget build(BuildContext context) {
    return MultiBlocProvider(
      providers: [
        BlocProvider<SeatBloc>(
          create: (_) => SeatBloc(getIt.get(), getIt.get()),
        ),
        BlocProvider<SeatLayoutCubit>(
          create: (_) =>
              SeatLayoutCubit(getIt.get())..load(movieSession.cinemaHallId),
        ),
      ],
      child: SeatsView(movieSession),
    );
  }
}

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
    _controller = ScrollController();
    super.initState();
  }

  late ScrollController _controller;

  @override
  Widget build(BuildContext context) {
    double width = MediaQuery.of(context).size.width;

    return SingleChildScrollView(
      controller: _controller,
      child: Column(
        children: [
          Container(
            padding: const EdgeInsets.only(top: 20.0),
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                if (width > 1200) buildMovieSessionInfo(context),
                if (width > 1200) const SizedBox(width: 15),
                buildSeatMap(context, width),

                if (width > 800) const SizedBox(width: 15),
                if (width > 800) const ShoppingCartWidget(),
              ],
            ),
          ),
        ],
      ),
    );
  }

  /// The coordinate-driven hall body. The renderer fills a bounded box so the
  /// inner `InteractiveViewer` has finite constraints to pan/zoom within.
  Widget buildSeatMap(BuildContext context, double width) {
    final hallWidth = width > 800
        ? (width * 0.5).clamp(320.0, 700.0)
        : (width - 30).clamp(280.0, double.infinity);

    return Container(
      width: hallWidth.toDouble(),
      height: 520,
      padding: const EdgeInsets.all(10),
      decoration: BoxDecoration(
        color: Theme.of(context).widgetColor,
        borderRadius: BorderRadius.circular(AppStyles.defaultRadius),
        border: Border.all(
          color: Theme.of(context).defaultBorderColor,
          width: AppStyles.defaultBorderWidth,
        ),
      ),
      child: SeatMapView(movieSession: widget.movieSession),
    );
  }

  Column buildMovieSessionInfo(BuildContext context) {
    return Column(
      children: [
        Container(
          width: 320,
          alignment: Alignment.topLeft,
          decoration: BoxDecoration(
            color: Theme.of(context).widgetColor,
            borderRadius: BorderRadius.circular(AppStyles.defaultRadius),
            border: Border.all(
              color: Theme.of(context).defaultBorderColor,
              width: AppStyles.defaultBorderWidth,
            ),
          ),
          padding: const EdgeInsets.all(15.0),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              BlocProvider(
                key: const ValueKey('AuditoriumDetailView'),
                create: (_) => CinemaHallCubit(),
                child: AuditoriumDetailView(widget.movieSession.cinemaHallId),
              ),
              Text(
                '${widget.movieSession.sessionDate.year}-${widget.movieSession.sessionDate.month}-${widget.movieSession.sessionDate.day}',
              ),
              Text(
                '${widget.movieSession.sessionDate.hour}:${'${widget.movieSession.sessionDate.minute}0'.substring(0, 2)}',
              ),
              TextButton(
                onPressed: () {
                  movieSeat(widget.movieSession.movieId);
                },
                child: Text(
                  AppLocalizations.of(context)!.select_another_session,
                ),
              ),
            ],
          ),
        ),
        const SizedBox(height: 10),
        BlocProvider(
          key: const ValueKey('MoviesDetailView'),
          create: (_) => MovieCubit(getIt.get()),
          child: MoviesDetailWidget(widget.movieSession.movieId),
        ),
      ],
    );
  }

  Future<void> movieSeat(String movieId) async {
    context.router.push(MovieSessionsRoute(movieId: movieId));
  }

  @override
  void dispose() {
    super.dispose();
  }
}
