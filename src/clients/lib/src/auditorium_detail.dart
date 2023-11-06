import 'package:flutter/cupertino.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:movie_theater_tickets/core/extensions/context_extensions.dart';
import '../core/common/views/loading_view.dart';
import '../core/utils/utils.dart';
import 'cinema_halls/presentation/cubit/cinema_hall_cubit.dart';
import 'movies/presentation/app/movie_theater_cubit.dart';
import 'package:carousel_slider/carousel_slider.dart';

class AuditoriumDetailView extends StatefulWidget {
  const AuditoriumDetailView(this.auditoriumId, {super.key});

  final String auditoriumId;
  static const id = 'cinema-hall';

  @override
  State<StatefulWidget> createState() => _auditoriumDetailView();
}

class _auditoriumDetailView extends State<AuditoriumDetailView> {

  @override
  void initState() {
    context.read<CinemaHallCubit>().getAuditorium(widget.auditoriumId);
    super.initState();
  }

  @override
  Widget build(BuildContext context) {
    return BlocConsumer<CinemaHallCubit, CinemaHallState>(
      listener: (context, state) {
        if (state is CinemaHallError) {
          Utils.showSnackBar(context, state.message);
        }
      },
      builder: (context, state) {
        if (state is! CinemaHallLoaded && state is! CinemaHallError) {
          return const LoadingView();
        }
        if ((state is CinemaHallLoaded && state.auditorium == null) ||
            state is MovieTheaterError) {
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

        state as CinemaHallLoaded;

        final cinemaHall = state.auditorium;
        return Column(children: [
          // Text("ID :${movie.id}"),
          Text("Auditorium name :${cinemaHall.description}")
        ]);
      },
    );
  }
}
