import 'package:flutter/material.dart';
import 'package:movie_theater_tickets/core/res/app_theme.dart';
import 'package:movie_theater_tickets/src/seats/presentation/widgets/seat_widget.dart';
import '../../../../core/common/views/loading_view.dart';
import '../../../../core/res/app_styles.dart';
import '../../../cinema_halls/domain/entity/cinema_seat.dart';
import '../../../cinema_halls/presentation/cubit/movie_cubit.dart';
import '../../../movie_sessions/domain/entities/movie_session.dart';
import '../../../shopping_carts/presentation/cubit/shopping_cart_cubit.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import '../../domain/entities/seat.dart';
import '../../domain/usecases/get_cinema_hall_info.dart';
import '../cubit/seat_cubit.dart';
import 'package:movie_theater_tickets/l10n/gen/app_localizations.dart';
import 'package:get_it/get_it.dart';

final getIt = GetIt.instance;

class SeatsMovieSessionWidget extends StatefulWidget {
  const SeatsMovieSessionWidget({super.key, required this.movieSession});
  //  , required this.getCinemaHallInfo});

  final MovieSession movieSession;
  // final GetCinemaHallInfo getCinemaHallInfo;

  @override
  State<SeatsMovieSessionWidget> createState() => _SeatsMovieSessionWidget();
}

class _SeatsMovieSessionWidget extends State<SeatsMovieSessionWidget> {
  @override
  void initState() {
    super.initState();
    print('movieSessionid ${widget.movieSession.id}');
    context.read<CinemaHallInfoBloc>().add(
      CinemaHallInfoEvent(cinemaHallId: widget.movieSession.cinemaHallId),
    );

    context.read<SeatBloc>().add(
      SeatEvent(movieSessionId: widget.movieSession.id),
    );
  }

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(
      builder: (context, constraints) {
        return Row(
          children: [
            BlocBuilder<CinemaHallInfoBloc, CinemaHallInfoState>(
              builder: (context, snapshot) {
                if (snapshot.status != CinemaHallInfoStatus.completed) {
                  return const LoadingView();
                }
                return buildSeats(
                  snapshot.movie.cinemaSeat,
                  context,
                  constraints.maxWidth,
                );
              },
            ),
          ],
        );
      },
    );
  }

  Widget buildSeats(
    List<List<CinemaSeat>> seats,
    BuildContext context,
    double maxWidth,
  ) {
    var seatsWidth = seats[0].length * 19.0;
    var seatsHeight = seats.length * 22.0;

    // Natural width of the hall body (60px row label + seats + trailing slack),
    // capped to the available viewport so a hall wider than the screen scrolls
    // sideways instead of overflowing (the one intentional behaviour change,
    // F12 / N11). The incoming layout constraint is authoritative when bounded;
    // inside seats_view.dart the widget is laid out unbounded (it sits in a
    // centred Row), so we fall back to the screen width from MediaQuery.
    var contentWidth = seatsWidth + 90;
    var viewport = MediaQuery.of(context).size.width;
    var available = maxWidth.isFinite && maxWidth < viewport
        ? maxWidth
        : viewport;
    var maxBody = available - 20;
    var bodyWidth = contentWidth < maxBody ? contentWidth : maxBody;

    return BlocSelector<ShoppingCartCubit, ShoppingCartState, String>(
      selector: (ShoppingCartState cart) {
        return cart.hashId;
      },
      builder: (BuildContext context, String hashId) {
        return Container(
          height: seatsHeight + 110,
          width: bodyWidth + 20,

          padding: const EdgeInsets.all(10),
          alignment: Alignment.topCenter,
          decoration: BoxDecoration(
            color: Theme.of(context).widgetColor,
            borderRadius: BorderRadius.circular(AppStyles.defaultRadius),
            border: Border.all(
              color: Theme.of(context).defaultBorderColor,
              width: AppStyles.defaultBorderWidth,
            ),
          ),
          // Bounded horizontal scroll: the body keeps its natural width and
          // scrolls within [bodyWidth] when the hall is wider than the viewport.
          // Vertical scrolling already lives one level up in seats_view.dart.
          child: SingleChildScrollView(
            scrollDirection: Axis.horizontal,
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                Row(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    const SizedBox(height: 40, width: 60),
                    Container(
                      height: 40,
                      width: seatsWidth,
                      alignment: Alignment.center,
                      child: Text(AppLocalizations.of(context)!.screen),
                    ),
                  ],
                ),
                for (final rowSeats in seats)
                  buildSeatBox(seatsWidth, context, rowSeats, hashId),
              ],
            ),
          ),
        );
      },
    );
  }

  SizedBox buildSeatBox(
    double seatsWidth,
    BuildContext context,
    List<CinemaSeat> rowSeats,
    String hashId,
  ) {
    return SizedBox(
      height: 22,
      width: seatsWidth + 90,
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          SizedBox(
            height: 19,
            width: 60,
            child: Text(
              '${AppLocalizations.of(context)!.row}: ${rowSeats[0].row}',
            ),
          ),
          for (final seatPlace in rowSeats)
            buildSeatCell(seatPlace, context, hashId),
        ],
      ),
    );
  }

  Widget buildSeatCell(
    CinemaSeat seatPlace,
    BuildContext context,
    String hashId,
  ) {
    return BlocSelector<SeatBloc, SeatState, Seat?>(
      selector: (SeatState state) {
        if (state.status != SeatStateStatus.loaded) {
          return null;
        }
        if (state.seats.isEmpty) {
          return null;
        }
        // O(1) lookup; a miss yields null → emptySeat, matching the legacy
        // firstWhere-catch path.
        return state.byId[(seatPlace.row, seatPlace.seatNumber)];
      },
      builder: (BuildContext context, Seat? state) {
        if (state == null) {
          return emptySeat(context);
        }
        return buildSeat(state, context, hashId);
      },
    );
  }

  Widget emptySeat(BuildContext context) {
    return const SeatWidget(text: '', backgroundColor: Colors.black12);
  }

  Widget buildSeat(Seat seat, BuildContext context, String hashId) {
    if (seat.blocked &&
        seat.hashId == hashId &&
        seat.seatStatus == SeatStatus.selected) {
      return SeatWidget(
        text: seat.seatNumber.toString(),
        backgroundColor: Colors.greenAccent,
        onPressed: () async {
          await onSeatUnselectPress(seat);
        },
      );
    }
    if (seat.blocked &&
        seat.hashId == hashId &&
        seat.seatStatus != SeatStatus.selected) {
      return SeatWidget(
        text: seat.seatNumber.toString(),
        backgroundColor: Colors.green,
        onPressed: () async {
          await onSeatUnselectPress(seat);
        },
      );
    } else if (seat.blocked && seat.hashId != hashId) {
      return SeatWidget(
        text: seat.seatNumber.toString(),
        backgroundColor: Colors.blue,
        onPressed: () async {
          await onSeatUnselectPress(seat);
        },
      );
    } else {
      return SeatWidget(
        text: seat.seatNumber.toString(),
        backgroundColor: Colors.grey,
        onPressed: () async {
          await onSelectSeatPress(seat);
        },
      );
    }
  }

  Future<void> onSelectSeatPress(Seat seat) async {
    if (context.read<ShoppingCartCubit>().state.status !=
        ShoppingCartStateStatus.initial) {
      await context.read<ShoppingCartCubit>().seatSelect(
        row: seat.row,
        seatNumber: seat.seatNumber,
        movieSessionId: widget.movieSession.id,
      );
    }
  }

  Future<void> onSeatUnselectPress(Seat seat) async {
    if (context.read<ShoppingCartCubit>().state.status !=
        ShoppingCartStateStatus.initial) {
      await context.read<ShoppingCartCubit>().unSeatSelect(
        row: seat.row,
        seatNumber: seat.seatNumber,
        movieSessionId: widget.movieSession.id,
      );
    }
  }

  @override
  void dispose() {
    super.dispose();
  }
}
