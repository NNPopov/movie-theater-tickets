import 'package:flutter/cupertino.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import '../../../../core/common/views/loading_view.dart';
import '../../../../core/common/views/no_data_view.dart';
import '../../../../core/utils/utils.dart';
import '../app/movie_theater_cubit.dart';
import 'package:flutter_gen/gen_l10n/app_localizations.dart';

class MoviesDetailWidget extends StatefulWidget {
  const MoviesDetailWidget(this.movieId, {super.key});

  final String movieId;
  static const id = 'movie';

  @override
  State<StatefulWidget> createState() => _MoviesDetailViewView();
}

class _MoviesDetailViewView extends State<MoviesDetailWidget> {
  @override
  void initState() {
    context.read<MovieTheaterCubit>().getMovieById(widget.movieId);
    super.initState();
  }

  @override
  Widget build(BuildContext context) {
    return BlocConsumer<MovieTheaterCubit, MovieTheaterState>(
      listener: (context, state) {
        if (state is MovieTheaterError) {
          Utils.showSnackBar(context, state.message);
        }
      },
      builder: (context, state) {
        if (state is! MovieLoaded && state is! MovieTheaterError) {
          return const LoadingView();
        }
        if ((state is MovieLoaded && state.movie == null) ||
            state is MovieTheaterError) {
          return NoDataView();
        }

        state as MovieLoaded;

        final movie = state.movie;
        return Column(children: [
          Text(movie.title),
          Text(
              '${AppLocalizations.of(context)!.release_date} :${movie.releaseDate.year}-${movie.releaseDate.month}-${movie.releaseDate.day}')
        ]);
      },
    );
  }
}
