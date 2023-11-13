import 'package:flutter/cupertino.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:movie_theater_tickets/core/extensions/context_extensions.dart';
import '../../../../core/common/views/loading_view.dart';
import '../../../../core/utils/utils.dart';
import '../app/movie_theater_cubit.dart';
import '../../../globalisations_flutter/widgets/globalisation_widget.dart';
import 'package:flutter_gen/gen_l10n/app_localizations.dart';

class MoviesDetailView extends StatefulWidget {
  const MoviesDetailView(this.movieId, {super.key});

  final String movieId;
  static const id = 'movie';

  @override
  State<StatefulWidget> createState() => _MoviesDetailViewView();
}

class _MoviesDetailViewView extends State<MoviesDetailView> {
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

        state as MovieLoaded;

        final movie = state.movie;
        return Column(children: [
          Text(movie.title),
          Text('${AppLocalizations.of(context)!.release_date} :${movie.releaseDate.year}-${movie.releaseDate.month}-${movie.releaseDate.day}')
        ]);
      },
    );
  }
}
