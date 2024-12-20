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
    Navigator.pushNamed(context, MovieSessionsView.id, arguments: movie.id);
  }

  late ScrollController _controller;
  double _offset = 0;

  @override
  void initState() {
    _controller = ScrollController();
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
        return buildMovies(movies, context);
      },
    );
  }

  int _itemsCount = 3;
  int _current = 0;

  Widget buildMovies(List<ActiveMovie> movies, BuildContext context) {
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

    return
      Container(
        alignment: Alignment.topLeft,
        child: SingleChildScrollView(
            controller: _controller,
            child: Column(
                mainAxisAlignment: MainAxisAlignment.start,
                crossAxisAlignment: CrossAxisAlignment.center,
                children: [
                  const DashboardWidget(route: MoviesView.id),
                  Container(
                    padding: const EdgeInsets.only(top: 20.0),
                    child: Container(
                      width: width - 10,
                      height: 700,
                      // child: Expanded(
                      child: Align(
                        alignment: Alignment.topCenter,
                        child: CarouselSlider(
                          items: movies.map((rowSeats) {
                            return BlocProvider(
                              create: (_) => MovieCubit(getIt.get()),
                              child: MovieDetailWidget(rowSeats.id),
                            );
                          }).toList(),
                          carouselController: buttonCarouselController,
                          options: CarouselOptions(
                            height: 570.0,
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
                      ),
                      //),
                    ),
                  ),
                  ElevatedButton(
                    onPressed: () => buttonCarouselController.nextPage(
                        duration: const Duration(milliseconds: 300),
                        curve: Curves.linear),
                    child: const Text('→'),
                  )
                ])),
      );



  }
}
