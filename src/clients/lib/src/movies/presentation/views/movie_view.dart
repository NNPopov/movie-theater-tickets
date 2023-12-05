import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import '../../../../core/common/views/loading_view.dart';
import '../../../../core/common/views/no_data_view.dart';
import '../../../../core/utils/utils.dart';
import '../../../dashboards/presentation/dashboard_widget.dart';
import '../../../movie_sessions/domain/entities/active_movie.dart';
import '../../../movie_sessions/presentation/views/movie_session_view.dart';
import '../../domain/entities/movie.dart';
import 'package:carousel_slider/carousel_slider.dart';
import '../../../movie_sessions/presentation/cubit/movie_theater_cubit.dart';
import 'package:flutter_gen/gen_l10n/app_localizations.dart';

import '../app/movie_cubit.dart';
import '../widgets/movie_full_detail_widget.dart';

class MoviesView extends StatefulWidget {
  const MoviesView({super.key});

  static const id = 'movies';

  @override
  State<StatefulWidget> createState() => _MoviesView();
}

class _MoviesView extends State<MoviesView> {
  CarouselController buttonCarouselController = CarouselController();

  Future<void> movieSeat(Movie movie) async {
    Navigator.pushNamed(context, MovieSessionsView.id, arguments: movie);
  }

  @override
  void initState() {
    context.read<MovieTheaterCubit>().getMovies();
    super.initState();
  }

  @override
  Widget build(BuildContext context) {
    return BlocConsumer<MovieTheaterCubit, MovieTheaterState>(
      listener: (context, state) {
        if (state.status == MoviesStatus.error) {
          Utils.showSnackBar(context, state.errorMessage ?? '');
        }
      },
      builder: (context, state) {
        if (state.status == MoviesStatus.fetching ||
            state.status == MoviesStatus.initial) {
          return const LoadingView();
        }
        if ((state.status == MoviesStatus.completed && state.movies.isEmpty)) {
          return const NoDataView();
        }

        final movies = state.movies;
        return BuildMovies(movies, context);
      },
    );
  }

  int _itemsCount = 3;
  int _current = 0;

  Widget BuildMovies(List<ActiveMovie> movies, BuildContext context) {
    final Locale locale = Localizations.localeOf(context);
    double width = MediaQuery.of(context).size.width;
    if (width > 1700) {
      _itemsCount = 5;
    } else if (width > 1350) {
      _itemsCount = 4;
    } else if (width > 1000) {
      _itemsCount = 3;
    } else if (width > 650) {
      _itemsCount = 2;
    } else {
      _itemsCount = 1;
    }

    return Column(
        mainAxisAlignment: MainAxisAlignment.start,
        crossAxisAlignment: CrossAxisAlignment.center,
        children: [
          const DashboardWidget(route: MoviesView.id),
          SizedBox(
              height: 40,
              width: 100,
              child: Text(AppLocalizations.of(context)!.movies)),
          Expanded(
              child: Align(
            alignment: Alignment.topCenter,
            child: CarouselSlider(
              items: movies.map((rowSeats) {

                return   BlocProvider(
                  create: (_) => MovieCubit(getIt.get()),
                  child: MovieDetailWidget(rowSeats.id),
                );


                // BlocProvider<MovieTheaterCubit>
                // return MovieDetailWidget(rowSeats.id);
              }).toList(),
              carouselController: buttonCarouselController,
              options: CarouselOptions(
                height: 670.0,
                enableInfiniteScroll: true,
                viewportFraction: 1.0 / _itemsCount,
                enlargeCenterPage: false,
                aspectRatio: 3.0,
                enlargeStrategy: CenterPageEnlargeStrategy.height,
                onPageChanged: (index, reason) {
                  setState(() {
                    _current = index;
                  });
                },
              ),
            ),
          )),
          ElevatedButton(
            onPressed: () => buttonCarouselController.nextPage(
                duration: const Duration(milliseconds: 300), curve: Curves.linear),
            child: const Text('→'),
          )
        ]);
  }

  Widget buildBovieContainer(Movie rowSeats, BuildContext context) {
       return Container(
      width: 320,
      height: 650,
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
                child: Text(rowSeats.title,
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
            Text(
                '${AppLocalizations.of(context)!.stars}: ${rowSeats.stars}'),
            Text(
                '${AppLocalizations.of(context)!.release_date}: ${rowSeats.releaseDate.year}-${rowSeats.releaseDate.month}-${rowSeats.releaseDate.day} '),
            Text('imdbId: ${rowSeats.imdbId}'),
            const Expanded(
              child: SizedBox(),
            ),
            Align(
              alignment: Alignment.center,
              child: TextButton(
                  style: ButtonStyle(
                    padding: MaterialStateProperty.all(
                        const EdgeInsets.symmetric(
                            vertical: 1, horizontal: 1)),
                    foregroundColor:
                        MaterialStateProperty.all<Color>(
                            Colors.blue),
                  ),
                  onPressed: () {
                    movieSeat(rowSeats);
                  },
                  child:
                      Text(AppLocalizations.of(context)!.select)),
            )
          ]),
    );
  }
}
