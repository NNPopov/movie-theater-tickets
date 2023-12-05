import 'package:flutter/cupertino.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import '../../../../core/common/views/loading_view.dart';
import '../../../../core/common/views/no_data_view.dart';
import '../../../../core/utils/utils.dart';
import '../app/movie_cubit.dart';
import '../../../movie_sessions/presentation/cubit/movie_theater_cubit.dart';
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
        return Container(
          width: 320,
          height: 550,
          alignment: Alignment.bottomLeft,
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
              mainAxisAlignment: MainAxisAlignment.start,
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Align(
                    child: Text(movie.title,
                        style: const TextStyle(
                            fontWeight: FontWeight.bold,
                            fontSize: 16,
                            color: Colors.grey))),
                const SizedBox(
                  height: 10,
                ),
                Container(
                  height: 290,
                  width: 290,
                  decoration: BoxDecoration(
                    borderRadius: BorderRadius.circular(5.0),
                    color: Colors.white,
                    image: const DecorationImage(
                        fit: BoxFit.fill,
                        image: NetworkImage(
                          'https://picsum.photos/250?image=9',
                        )),
                  ),
                ),
                Text('${AppLocalizations.of(context)!.stars}: ${movie.stars}'),
                Text(
                    '${AppLocalizations.of(context)!.release_date}: ${movie.releaseDate.year}-${movie.releaseDate.month}-${movie.releaseDate.day} '),
                Text('imdbId: ${movie.imdbId}'),
                const Expanded(
                  child: SizedBox(),
                ),
              ]),
        );
      },
    );
  }
}
