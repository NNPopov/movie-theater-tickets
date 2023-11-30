import 'package:flutter/cupertino.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import '../../../../core/common/views/loading_view.dart';
import '../../../../core/common/views/no_data_view.dart';
import '../../../../core/utils/utils.dart';
import '../app/movie_cubit.dart';
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
    context.read<MovieCubit>().getMovieById(widget.movieId);
    super.initState();
  }

  @override
  Widget build(BuildContext context) {
    return BlocConsumer<MovieCubit, MovieState>(
      listener: (context, state) {
        if (state.status == MoviesStatus.error) {
          Utils.showSnackBar(context, state.errorMessage!);
        }
      },
      builder: (context, state) {
        if ((state.status == MoviesStatus.fetching ||
            state.status == MoviesStatus.initial)) {
          return const LoadingView();
        }
        if ((state.status == MoviesStatus.completed && state.movie == null)) {
          return const NoDataView();
        }

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
