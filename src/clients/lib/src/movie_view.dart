import 'package:flutter/cupertino.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:movie_theater_tickets/core/extensions/context_extensions.dart';
import '../core/common/views/loading_view.dart';
import '../core/utils/utils.dart';
import 'movie_session_view.dart';
import 'movies/domain/entities/movie.dart';
import 'package:carousel_slider/carousel_slider.dart';

import 'movies/presentation/app/movie_theater_cubit.dart';

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
        if (state is MovieTheaterError) {
          Utils.showSnackBar(context, state.message);
        }
      },
      builder: (context, state) {
        if (state is! MoviesLoaded && state is! MovieTheaterError) {
          return const LoadingView();
        }
        if ((state is MoviesLoaded && state.movies.isEmpty) ||
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

        state as MoviesLoaded;

        final movies = state.movies
          ..sort((a, b) => b.releaseDate.compareTo(a.releaseDate));
        return BuildMovies(movies, context);
      },
    );
  }

  Widget BuildMovies(List<Movie> movies, BuildContext context) {
    return Scaffold(
      backgroundColor: Colors.white,
      appBar: AppBar(
        title: Text('Movies'),
      ),
      body: Column(children: [
        SizedBox(height: 40, width: 100, child: Text('Movies')),
        SizedBox(
            height: 100,
            width: 1000,
            child: CarouselSlider(
              items: movies.map((rowSeats) {
                return SizedBox(
                  height: 100,
                  width: 200,
                  child: Column(children: [
                    Text('Title: ${rowSeats.title}'),
                    Text('Release Date: ${rowSeats.releaseDate}'),
                    TextButton(
                        style: ButtonStyle(
                          padding: MaterialStateProperty.all(
                              const EdgeInsets.symmetric(
                                  vertical: 1, horizontal: 1)),
                          foregroundColor:
                              MaterialStateProperty.all<Color>(Colors.blue),
                        ),
                        onPressed: () {
                          movieSeat(rowSeats);
                        },
                        child: const Text('Select'))
                  ]),
                );
              }).toList(),
              carouselController: buttonCarouselController,
              options: CarouselOptions(
                  height: 300.0,
                  enableInfiniteScroll: true,
                  viewportFraction: 0.4,
                  enlargeCenterPage: false,
                  aspectRatio: 3.0),
            )),
        ElevatedButton(
          onPressed: () => buttonCarouselController.nextPage(
              duration: Duration(milliseconds: 300), curve: Curves.linear),
          child: Text('→'),
        )
      ]),
    );
  }
}
